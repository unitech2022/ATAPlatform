# ATA Platform — Backend

ASP.NET Core 10 Minimal API (modular monolith) + EF Core (Pomelo MySQL) for the ATA ride-hailing platform.
Step 1 covers identity (OTP login, JWT + refresh tokens), passenger profile, driver onboarding, catalog, wallet
(sandbox top-ups with a double-entry ledger), notifications, files and the admin review console.
F8 adds the trip lifecycle (`Modules/Trips`): estimates, requests, driver offers via a background matcher, the
en-route → arrived → PIN → in-trip → completed state machine, fare settlement through the ledger, the admin trips
console + live map data, and a SignalR hub for real-time updates.
F9/F10 replace the F8 stand-ins with rule-based engines (`Modules/Pricing`, `Modules/Trips/Matching`): zones with
point-in-polygon resolution, pricing rules with time-of-day multipliers, a demand engine (levels, rules, snapshots, manual
overrides) with surge caps, fare quotes that lock the price of a trip request, "offer your price" bounds, and a scoring
matcher with expanding search rounds recorded for the admin console.

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
| `Matching:RadiusMeters`, `Matching:MaxRadiusMeters`, `Matching:RadiusStepMeters` | First-round radius, widest radius and step between rounds, used when no `matching_settings` row applies (defaults 5000 / 12000 / 2500) |
| `Matching:OfferTimeoutSeconds` | How long a driver has to accept an offer when no `matching_settings` row applies (default 20) |
| `Matching:SearchTimeoutSeconds` | Total search time before the trip becomes `no_drivers` when no `matching_settings` row applies (default 120) |
| `Matching:MaxCandidates` | Candidates ranked per round when no `matching_settings` row applies (default 8) |
| `Matching:AllowUpgrade` | Also offer the trip to drivers of higher categories (default `false`; a `matching_settings` row can enable it per zone/category) |
| `Matching:PollIntervalSeconds` | Matching loop interval (default 2) |
| `Matching:LocationMaxAgeSeconds` | Drivers whose last location is older than this are not matched or counted as available (default 300) |
| `Pricing:OfferMinPercent`, `Pricing:OfferMaxPercent` | "Offer your price" bounds as a percentage of the quoted total (defaults 70 / 130) |
| `Pricing:QuoteExpiryMinutes` | Validity of a `fare_quotes` row (default 5) |
| `Pricing:UtcOffsetMinutes` | Local offset for time multipliers and zone operating hours (default 180 = Riyadh) |
| `Demand:Enabled` | Runs the demand loop (`false` in tests, which call one pass explicitly) |
| `Demand:IntervalSeconds` | Demand loop interval (default 60) |
| `Demand:SnapshotMaxAgeMinutes` | A snapshot older than this no longer drives the level; the engine falls back to `normal` (default 20 = 2 × the 10-minute window) |
| `Demand:SnapshotRetentionHours` | Older `demand_snapshots` rows are pruned by the loop (default 48) |
| `Realtime:LiveSnapshotEnabled`, `Realtime:LiveSnapshotSeconds` | `LiveSnapshot` push to the `admins` hub group (default every 5 s) |

### Development OTP behaviour

- Codes are 4 digits, stored hashed, valid 5 minutes, 5 wrong attempts lock the code (`otp_locked`).
- Resend allowed after 60 s; at most 3 requests per phone per 10 minutes, otherwise `429 rate_limited`
  with `details.retryAfterSeconds` (also sent as a `Retry-After` header).
- With `Otp:DevMode=true` the response carries `devCode`, and the "SMS" is only written to the log.

### Seeded data

- Ride categories: `saver`, `economy`, `comfort`, `family`, `premium`, `airport`, each with `FlatPricing` inputs
  (`base_fare`, `per_km`, `per_minute`, `booking_fee`, `min_fare`, `driver_share_percent` = 80). Rows that pre-date F8
  are back-filled once by the seeder when all their pricing columns are still zero. Since F10 these columns are only the
  fallback used when no pricing rule matches.
- Zone `city_default` for Riyadh (a large rectangle 24.2–25.6 N / 46.0–47.3 E around the city; pickups outside every zone
  fall back to the nearest city default).
- Demand levels `normal` ×1.00 (`#19B7A5`), `moderate` ×1.20 (`#123650`), `high` ×1.50 (`#E0A100`), `very_high` ×1.90 (`#C23B4A`).
- One city-wide pricing rule per ride category, migrated once from the category's flat-pricing columns (service fee 0 %,
  waiting billed at `per_minute`, 3 free waiting minutes, effective from 2026-01-01) with the example `night` multiplier
  00:00–05:00 ×1.15. Categories that already have a city-wide rule are left alone.
- One global demand rule (`requests_per_driver`, 10-minute window, thresholds 0.8 / 1.5 / 2.5) and one global
  `matching_settings` row (5 km → 12 km in 2.5 km steps, 20 s offers, 120 s search, 8 candidates, weights
  `{distance:0.35, eta:0.20, rating:0.15, acceptance:0.10, cancellation:0.10, tier:0.05, favorite:0.05}`).
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
| Pricing | `POST /pricing/quote` (passenger; stores `fare_quotes`, 5-minute expiry), `GET /pricing/demand?lat=&lng=` (any authenticated user: current zone + demand level) |
| Passenger trips | `POST /passenger/trips/estimate` (alias of `POST /pricing/quote`), `POST /passenger/trips` (optional `quoteId`; 409 `trip_active_exists`, 422 `quote_expired`, 422 `offer_out_of_range` with `details.offerMin/offerMax`), `GET /passenger/trips/active` (`Trip` or `null`), `GET /passenger/trips/{id}` (PIN once a driver is assigned), `POST /passenger/trips/{id}/cancel`, `GET /passenger/trips?status=all|active|completed|cancelled` |
| Wallet | `GET /wallet`, `GET /wallet/transactions`, `POST /wallet/topups` (requires `Idempotency-Key`) |
| Driver | `GET /driver/application`, `PUT /driver/application/profile`, `PUT /driver/application/vehicle`, `POST /driver/documents` (multipart), `DELETE /driver/documents/{id}`, `POST /driver/application/submit`, `GET/PUT /driver/status`, `GET /driver/earnings/summary` (completed trips + online hours) |
| Driver trips | `PUT /driver/location` (204; broadcasts `DriverLocation` to the passenger during a trip), `GET /driver/offers/active` (`Offer` or `null`; includes `round` and `passengerOffered`), `POST /driver/offers/{id}/accept` (409 `offer_expired`), `POST /driver/offers/{id}/reject`, `GET /driver/trips/active`, `POST /driver/trips/{id}/en-route`, `/arrived`, `/verify-pin` (400 `pin_invalid` with `attemptsLeft`, 429 `pin_locked`), `/start`, `/complete`, `/cancel`, `GET /driver/trips?status=` |
| Notifications | `GET /notifications`, `POST /notifications/read` |
| Files | `GET /files/{id}` (owner or admin, inline) |
| Admin | `GET /admin/dashboard/summary`, `GET /admin/drivers`, `GET /admin/drivers/{id}`, `POST /admin/drivers/{id}/{review,approve,reject,suspend,reinstate}`, `POST /admin/documents/{id}/verify`, `GET /admin/passengers`, `POST /admin/users/{userId}/{suspend,reinstate}`, `GET/POST /admin/ride-categories`, `PUT/DELETE /admin/ride-categories/{id}` (`fallbackPricing` = flat-pricing columns, `pricingSource`), `GET /admin/audit-logs` |
| Admin pricing | `GET/POST /admin/zones`, `GET/PUT/DELETE /admin/zones/{id}` (nested `zoneCategorySettings`; the city default cannot be deleted), `GET/POST /admin/pricing-rules?rideCategoryId=&zoneId=`, `GET/PUT/DELETE /admin/pricing-rules/{id}` (nested `timeMultipliers`, replaced as a whole on PUT), `POST /admin/pricing/simulate` (quote inputs + `at`, nothing stored), `GET /admin/demand-levels`, `PUT /admin/demand-levels/{id}` (multiplier only), `GET/POST /admin/demand-rules`, `PUT/DELETE /admin/demand-rules/{id}`, `GET /admin/demand-overrides?active=`, `POST /admin/demand-overrides`, `PUT/DELETE /admin/demand-overrides/{id}` (DELETE ends a running override), `GET /admin/demand/current` (level per zone and category with the reading behind it). Every write is audited (`zone.*`, `pricing_rule.*`, `demand_level.update`, `demand_rule.*`, `demand_override.*`) |
| Admin matching | `GET/POST /admin/matching-settings`, `GET/PUT/DELETE /admin/matching-settings/{id}` (audited `matching_settings.*`), `GET /admin/trips/{id}/matching` (rounds with scored candidates and their answers), `GET /admin/matching/stats?from=&to=` (assignment rate and times, `no_drivers` rate, offer acceptance, rounds per trip) |
| Admin trips | `GET /admin/trips?status=&from=&to=&search=&page=`, `GET /admin/trips/{id}` (full events with actor names, offers, route), `POST /admin/trips/{id}/cancel` (audited as `trip.cancel`), `GET /admin/live` (drivers incl. recently-offline, active and searching trips) |
| Realtime | SignalR hub `/hubs/trips` (JWT via `?access_token=`): `TripUpdated`, `DriverLocation` (passenger), `OfferReceived`, `OfferExpired`, `TripUpdated` (driver), `LiveSnapshot` every 5 s + `TripUpdated` + `DemandChanged` (`admins` group) |
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
- Matching and pricing are provided by the F9/F10 engines below; `FlatPricing` (distance = Haversine × 1.3 through the stops,
  duration at 30 km/h; `fare = base_fare + per_km × km + per_minute × min + booking_fee`, never below `min_fare`, from the
  `ride_categories` columns) remains the route estimator and the fare fallback when no pricing rule matches.
  `pricingMode=offer` uses `offeredPrice` as the fare. `/complete` uses the estimated distance/duration unless the client sends
  `finalDistanceMeters`/`finalDurationSeconds`. Scheduled trips start matching `Trips:ScheduledLeadMinutes` before `scheduledAt`.
- Payment on completion (one transaction with the state change):
  - `wallet`: passenger wallet debit (`trip_payment`) and driver wallet credit (`trip_earning`), both against `trip_revenue`.
    If the balance is insufficient the trip still completes: `payment_method` becomes `cash` and a `payment_fallback_cash`
    event is recorded. `card` also falls back to cash until the gateway lands (step 3).
  - `cash`: driver wallet credit (`trip_earning`) against `cash_collected`; the commission settlement for cash trips is part of payouts (step 3).
- Notifications rows: passenger on driver assigned / arrived / completed / cancelled / no drivers; driver on cancellation by the passenger or admin.
- `driver_locations` mirrors `drivers.is_online` and `current_trip_id`; `driver_location_history` is written only while on a trip.
  `drivers.current_trip_id` / `driver_locations.current_trip_id` are plain columns (no FK) to avoid a `drivers` ↔ `trips` FK cycle.

## Pricing, zones and demand (F10)

- **Zones** (`zones`, `zone_category_settings`): `ZoneResolver` keeps every active zone parsed in memory (`ZoneCache`, invalidated by
  the admin zone endpoints) and resolves a point with ray casting; the highest `priority` wins on overlap, zones outside their
  `operating_hours` are skipped, and a point in no zone falls back to the nearest `city_default`. A zone can disable a category
  (no candidates are matched there) and cap the demand multiplier (`surge_cap`, default 2.5).
- **Rules** (`pricing_rules`, `pricing_time_multipliers`): `RulePricingService` (`IPricingService`) picks the active rule effective at
  the pickup time with the highest priority for the category and pickup zone, then the city-wide rule (`zone_id` null):
  `subtotal = max(base_fare + per_km × km + per_minute × min + waiting_per_minute × billable waiting min, min_fare)`,
  `fare = subtotal × timeMult × demand + booking_fee`, `fare += fare × service_fee_percent / 100`, `fare −= discount` (0 until F15/F16),
  `total = round(fare, 0.5 SAR)`, `driverNet = subtotal × timeMult × demand × driver_share_percent / 100`. `timeMult` is the highest
  multiplier whose window (local time, `Pricing:UtcOffsetMinutes`) contains the pickup time; windows may span midnight.
  The rule's `free_waiting_minutes` drives the free-waiting timer at the pickup (`Trips:FreeWaitingMinutes` is the fallback).
- **Demand** (`demand_levels`, `demand_rules`, `demand_snapshots`, `demand_overrides`): `DemandBackgroundService` runs every
  `Demand:IntervalSeconds`; for each zone (and each category a rule targets) it counts `requested/searching` trips in the rule's window
  against available online drivers in the zone (`requests_per_driver`; no drivers → the request count itself), maps the ratio to a level
  (`≥ threshold_very_high` → `very_high`, …), stores a snapshot and pushes `DemandChanged` to the `admins` hub group when the level
  moved. Reads (`DemandService`) resolve override → latest snapshot not older than `Demand:SnapshotMaxAgeMinutes` → `normal`, and cap the
  multiplier with the zone's `surge_cap`. The most specific rule wins (zone+category → zone → category → global).
- **Quotes** (`fare_quotes`): `POST /pricing/quote` prices every active category (with the nearest eligible driver's ETA) and stores one
  row per category sharing a `group_id`; `quoteId` (top-level = the requested/first category, also per category) can be sent with
  `POST /passenger/trips` for any category of the group. The trip then keeps the quoted `total`, driver share and demand multiplier
  (`used_trip_id`), even if demand changed meanwhile; an expired or already used quote is rejected with `422 quote_expired`. Requests
  without a quote are priced on the spot and stored as an already-used quote. `offeredPrice` must lie in `[offerMin, offerMax]` =
  `total × Pricing:OfferMinPercent/OfferMaxPercent` (rounded to 0.5), otherwise `422 offer_out_of_range` with both bounds. The driver's
  net for an offered price is `offeredPrice × driver_share_percent`. On completion the fare is recomputed with the actual distance,
  duration and billable waiting, the request-time multiplier and the locked demand multiplier.

## Matching engine (F9)

- **Settings** (`matching_settings`): resolved zone+category → zone → category → global row → `Matching:*` configuration; cached and
  invalidated by the admin endpoints.
- **Rounds**: `MatchingService` (one pass per `Matching:PollIntervalSeconds`) starts round 1 with `radius_meters`; a round with no
  eligible driver is recorded and the radius immediately grows by `radius_step_meters` up to `max_radius_meters`. Each round is a
  `matching_attempts` row with its ranked `matching_candidates` (best `max_candidates` by score). Offers go to candidates in rank order,
  one at a time with `offer_timeout_seconds`; a rejected/expired offer moves to the next candidate, an exhausted round opens the next
  wider one, and at the widest radius the engine re-scans at most once per offer timeout until `search_timeout_seconds` → `no_drivers`
  (`outcome = timeout`). Accepting closes the round with `assigned`; cancelling a searching trip closes it with `cancelled`.
- **Eligibility** (`ScoringMatcher`): online, approved, no current trip, location fresher than `Matching:LocationMaxAgeSeconds`, active
  vehicle in the category (or higher when `allow_category_upgrade`), female when requested, the pickup zone allows the category, no
  expired driver document, not blocked (`IDriverReliabilityProvider`, F14 hook), not already offered this trip.
- **Score** in [0, 1] with the settings' weights (normalised): `distance = 1 − d/max_radius`, `eta = 1 − eta/900 s`,
  `rating = (rating − 3)/2`, `acceptance = acceptance_count/(acceptance_count + rejection_count)` (1 without history),
  `cancellation = 1 − driver cancellations/assigned trips in 30 days`, `tier` bronze .25 / silver .5 / gold .75 / platinum 1,
  `favorite` 1 for the passenger's favourite drivers (`IFavoriteDriverProvider`, F16 hook; none before F16).
- Tests drive one pass with `AtaWebApplicationFactory.RunMatcherAsync()` / `RunDemandAsync()`; both loops are disabled in the test host.

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
  tests drive one matching pass with `AtaWebApplicationFactory.RunMatcherAsync()` and one demand pass with `RunDemandAsync()`.
- Zones are matched in memory (ray casting) after a bounding-box check; the demand loop resolves trips and drivers to zones the same way.
- `PinCodeProtected` uses ASP.NET Data Protection; in production persist the key ring (e.g. `Storage:Root`) so PINs of in-flight
  trips survive restarts.
