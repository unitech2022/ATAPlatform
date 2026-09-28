# ATA Platform — Backend

ASP.NET Core 10 Minimal API (modular monolith) + EF Core (Pomelo MySQL) for the ATA ride-hailing platform.
Step 1 covers identity (OTP login, JWT + refresh tokens), passenger profile, driver onboarding, catalog, wallet
(sandbox top-ups with a double-entry ledger), notifications, files and the admin review console.
F8 adds the trip lifecycle (`Modules/Trips`): estimates, requests, driver offers via a background matcher, the
en-route → arrived → PIN → in-trip → completed state machine, fare settlement through the ledger, the admin trips
console + live map data, and a SignalR hub for real-time updates.

## Layout

```
backend/
  ATAPlatform.sln
  docker-compose.yml            # MySQL 8 (db ata / user ata / password ata, port 3306) + Redis 7
  src/ATA.Domain                # entities, enums, business rules (no EF dependency)
  src/ATA.Infrastructure        # AtaDbContext, configurations, migrations, seed, storage, SMS, security
  src/ATA.Api                   # Program.cs + Modules/<Name>/{Endpoints,Services,Contracts} + Common/
  tests/ATA.Tests               # xUnit: unit tests + integration tests over SQLite in-memory
```

## Run locally

```bash
cd backend
docker compose up -d                      # MySQL 8 + Redis 7
dotnet tool restore                       # installs the pinned dotnet-ef (local tool manifest)
dotnet ef database update -p src/ATA.Infrastructure -s src/ATA.Infrastructure
dotnet run --project src/ATA.Api          # http://localhost:5080
```

In `Development` the API also applies pending migrations and seeds data automatically at startup.
Open `http://localhost:5080/docs` (Scalar UI) or `http://localhost:5080/openapi/v1.json`.

Tests (no MySQL needed, SQLite in-memory):

```bash
dotnet test
```

### Configuration keys (`appsettings*.json` or environment variables with `__`)

| Key | Notes |
|---|---|
| `ConnectionStrings:Default` | `Server=localhost;Port=3306;Database=ata;User=ata;Password=ata;` |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` | Key must be ≥ 32 chars. Access token 60 min, refresh 30 days |
| `Otp:DevMode` | `true` in Development: no SMS is sent and `devCode` is returned by `/auth/otp/request` |
| `Storage:Root` | Local folder for uploaded documents (`./storage`) |
| `Cors:Origins` | Allowed origins for the website and dashboard |
| `Payments:SandboxEnabled` | `true` in Development: `POST /wallet/topups` with `method: "sandbox"` credits the wallet |
| `RateLimiting:OtpPerIpPerHour` | IP-level limit on the OTP endpoints (default 10) |
| `Admin:Username`, `Admin:Password` | Seeded development admin (override in production) |
| `Trips:FreeWaitingMinutes` | Free waiting at the pickup before waiting time is billed (default 3) |
| `Trips:ArrivalRadiusMeters` | "Arrived" reported farther than this from the pickup adds an `arrival_distance_warning` event (default 300) |
| `Trips:PinMaxAttempts` | Wrong-PIN attempts before `pin_locked` (default 5) |
| `Trips:ScheduledLeadMinutes` | How long before `scheduledAt` the matcher starts searching for a scheduled trip (default 15) |
| `Matching:Enabled` | Runs the matching loop (`false` in tests, which call one pass explicitly) |
| `Matching:RadiusMeters` | Search radius around the pickup (default 5000) |
| `Matching:OfferTimeoutSeconds` | How long a driver has to accept an offer (default 20) |
| `Matching:SearchTimeoutSeconds` | Total search time before the trip becomes `no_drivers` (default 120) |
| `Matching:AllowUpgrade` | Also offer the trip to drivers of higher categories (default `false`) |
| `Matching:PollIntervalSeconds` | Matching loop interval (default 2) |
| `Matching:LocationMaxAgeSeconds` | Drivers whose last location is older than this are not matched (default 300) |
| `Realtime:LiveSnapshotEnabled`, `Realtime:LiveSnapshotSeconds` | `LiveSnapshot` push to the `admins` hub group (default every 5 s) |

### Development OTP behaviour

- Codes are 4 digits, stored hashed, valid 5 minutes, 5 wrong attempts lock the code (`otp_locked`).
- Resend allowed after 60 s; at most 3 requests per phone per 10 minutes, otherwise `429 rate_limited`
  with `details.retryAfterSeconds` (also sent as a `Retry-After` header).
- With `Otp:DevMode=true` the response carries `devCode`, and the "SMS" is only written to the log.

### Seeded data

- Ride categories: `saver`, `economy`, `comfort`, `family`, `premium`, `airport`, each with `FlatPricing` inputs
  (`base_fare`, `per_km`, `per_minute`, `booking_fee`, `min_fare`, `driver_share_percent` = 80). Rows that pre-date F8
  are back-filled once by the seeder when all their pricing columns are still zero.
- Document types: `national_id`, `driving_license`, `vehicle_registration`, `insurance`, `profile_photo` (all required).
- City: `riyadh`.
- Admin account: username `admin`, password `Admin@12345` (`POST /api/v1/auth/admin/login`).

## API summary (`/api/v1`, JSON camelCase, `Accept-Language: ar|en`, errors as `{ "error": { code, message, details } }`)

| Area | Endpoints |
|---|---|
| Auth | `POST /auth/otp/request`, `POST /auth/otp/verify`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/admin/login` |
| Me | `GET/PATCH/DELETE /me`, `GET/PUT /me/notification-preferences`, `PUT /me/devices` |
| Catalog | `GET /catalog/ride-categories`, `GET /catalog/document-types`, `GET /catalog/cities` |
| Passenger | `GET /passenger/saved-places`, `PUT/DELETE /passenger/saved-places/{label}`, `PATCH /passenger/preferences` |
| Passenger trips | `POST /passenger/trips/estimate`, `POST /passenger/trips` (409 `trip_active_exists`), `GET /passenger/trips/active` (`Trip` or `null`), `GET /passenger/trips/{id}` (PIN once a driver is assigned), `POST /passenger/trips/{id}/cancel`, `GET /passenger/trips?status=all|active|completed|cancelled` |
| Wallet | `GET /wallet`, `GET /wallet/transactions`, `POST /wallet/topups` (requires `Idempotency-Key`) |
| Driver | `GET /driver/application`, `PUT /driver/application/profile`, `PUT /driver/application/vehicle`, `POST /driver/documents` (multipart), `DELETE /driver/documents/{id}`, `POST /driver/application/submit`, `GET/PUT /driver/status`, `GET /driver/earnings/summary` (completed trips + online hours) |
| Driver trips | `PUT /driver/location` (204; broadcasts `DriverLocation` to the passenger during a trip), `GET /driver/offers/active` (`Offer` or `null`), `POST /driver/offers/{id}/accept` (409 `offer_expired`), `POST /driver/offers/{id}/reject`, `GET /driver/trips/active`, `POST /driver/trips/{id}/en-route`, `/arrived`, `/verify-pin` (400 `pin_invalid` with `attemptsLeft`, 429 `pin_locked`), `/start`, `/complete`, `/cancel`, `GET /driver/trips?status=` |
| Notifications | `GET /notifications`, `POST /notifications/read` |
| Files | `GET /files/{id}` (owner or admin, inline) |
| Admin | `GET /admin/dashboard/summary`, `GET /admin/drivers`, `GET /admin/drivers/{id}`, `POST /admin/drivers/{id}/{review,approve,reject,suspend,reinstate}`, `POST /admin/documents/{id}/verify`, `GET /admin/passengers`, `POST /admin/users/{userId}/{suspend,reinstate}`, `GET/POST /admin/ride-categories`, `PUT/DELETE /admin/ride-categories/{id}` (incl. pricing fields), `GET /admin/audit-logs` |
| Admin trips | `GET /admin/trips?status=&from=&to=&search=&page=`, `GET /admin/trips/{id}` (full events with actor names, offers, route), `POST /admin/trips/{id}/cancel` (audited as `trip.cancel`), `GET /admin/live` (drivers incl. recently-offline, active and searching trips) |
| Realtime | SignalR hub `/hubs/trips` (JWT via `?access_token=`): `TripUpdated`, `DriverLocation` (passenger), `OfferReceived`, `OfferExpired`, `TripUpdated` (driver), `LiveSnapshot` every 5 s + `TripUpdated` (`admins` group) |
| System | `GET /health` (MySQL check), `GET /openapi/v1.json`, `GET /docs` (Development) |

Roles: `passenger`, `driver`, `admin`, `operations` (JWT `roles` claim). Admin actions are recorded in `audit_logs`
and driver status changes create a notification row for the driver. Approving a driver requires every required
document type to be `verified`.

## Trips (F8)

- States: `requested → searching → driver_assigned → driver_en_route → driver_arrived → waiting → pin_verified → in_trip → completed | cancelled | no_drivers`.
  `/arrived` moves straight to `waiting` (the free-waiting timer starts); billable waiting = time between arrival and PIN
  verification minus `Trips:FreeWaitingMinutes`. Every transition writes a `trip_events` row (type, actor, coordinates, JSON data).
- Trip numbers are `T-YYYYMMDD-#####` (sequential per UTC day). The 4-digit PIN is verified against an HMAC keyed by the trip id
  (`pin_code_hash`) and additionally stored data-protected (`pin_code_protected`) so `GET /passenger/trips/{id}` can show it to the
  passenger once a driver is assigned; it is never returned to drivers or admins.
- `SimpleMatcher` (`IMatcher`): online + approved drivers without a current trip, active vehicle in the requested category
  (or higher with `Matching:AllowUpgrade`), female when `preferFemaleDriver`, fresh location inside `Matching:RadiusMeters`,
  nearest first (bounding box in SQL, Haversine in memory). `MatchingBackgroundService` polls `searching` trips every
  `Matching:PollIntervalSeconds`, sends one offer at a time (`Matching:OfferTimeoutSeconds`), expires it, moves to the next driver,
  and marks `no_drivers` after `Matching:SearchTimeoutSeconds`. Rejections/acceptances update `drivers.rejection_count` /
  `acceptance_count`. Scheduled trips start matching `Trips:ScheduledLeadMinutes` before `scheduledAt`.
- `FlatPricing` (`IPricingService`): distance = Haversine × 1.3 through the stops, duration at 30 km/h;
  `fare = base_fare + per_km × km + per_minute × (minutes + billable waiting minutes) + booking_fee`, never below `min_fare`;
  driver net = fare × `driver_share_percent`. `pricingMode=offer` uses `offeredPrice` as the fare. `/complete` uses the estimated
  distance/duration unless the client sends `finalDistanceMeters`/`finalDurationSeconds`.
- Payment on completion (one transaction with the state change):
  - `wallet`: passenger wallet debit (`trip_payment`) and driver wallet credit (`trip_earning`), both against `trip_revenue`.
    If the balance is insufficient the trip still completes: `payment_method` becomes `cash` and a `payment_fallback_cash`
    event is recorded. `card` also falls back to cash until the gateway lands (step 3).
  - `cash`: driver wallet credit (`trip_earning`) against `cash_collected`; the commission settlement for cash trips is part of payouts (step 3).
- Notifications rows: passenger on driver assigned / arrived / completed / cancelled / no drivers; driver on cancellation by the passenger or admin.
- `driver_locations` mirrors `drivers.is_online` and `current_trip_id`; `driver_location_history` is written only while on a trip.
  `drivers.current_trip_id` / `driver_locations.current_trip_id` are plain columns (no FK) to avoid a `drivers` ↔ `trips` FK cycle.

### Ledger accounts

| Account | Meaning |
|---|---|
| `passenger_wallet:{id}` / `driver_wallet:{id}` | User wallet liabilities (credit = balance grows) |
| `gateway_clearing` | Counterpart of top-ups (sandbox/gateway money in transit) |
| `trip_revenue` | Fares collected through wallets; the driver share is paid out of it, the remainder is the platform commission |
| `cash_collected` | Driver earnings recognised on cash trips (cash held by drivers until settlement) |

## Migrations

```bash
dotnet ef migrations add <Name> -p src/ATA.Infrastructure -s src/ATA.Infrastructure -o Persistence/Migrations
dotnet ef database update -p src/ATA.Infrastructure -s src/ATA.Infrastructure
```

`ATA.Infrastructure` ships an `IDesignTimeDbContextFactory`, so migrations can be scaffolded without a running database.

## Notes

- Pomelo.EntityFrameworkCore.MySql 9.0.0 is the newest release and pins EF Core to 9.0.x, so the solution targets
  `net10.0` with EF Core 9.0.20. Upgrade both together once Pomelo 10 ships.
- Wallet balance is a derived value updated only inside the same transaction as `wallet_transactions` and its two
  balanced `ledger_entries`. Step 1 has no card/gateway integration; only `method: "sandbox"` is accepted.
- Tests run on SQLite in-memory, so LINQ stays translatable on both providers (no MySQL-only functions); the matcher pre-filters
  with a bounding box and sorts by Haversine distance in memory. The background loops are disabled in the test host and the
  tests drive one matching pass with `AtaWebApplicationFactory.RunMatcherAsync()`.
- `PinCodeProtected` uses ASP.NET Data Protection; in production persist the key ring (e.g. `Storage:Root`) so PINs of in-flight
  trips survive restarts.
