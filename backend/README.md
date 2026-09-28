# ATA Platform — Backend (Step 1)

ASP.NET Core 10 Minimal API (modular monolith) + EF Core (Pomelo MySQL) for the ATA ride-hailing platform.
Step 1 covers identity (OTP login, JWT + refresh tokens), passenger profile, driver onboarding, catalog, wallet
(sandbox top-ups with a double-entry ledger), notifications, files and the admin review console.

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

### Development OTP behaviour

- Codes are 4 digits, stored hashed, valid 5 minutes, 5 wrong attempts lock the code (`otp_locked`).
- Resend allowed after 60 s; at most 3 requests per phone per 10 minutes, otherwise `429 rate_limited`
  with `details.retryAfterSeconds` (also sent as a `Retry-After` header).
- With `Otp:DevMode=true` the response carries `devCode`, and the "SMS" is only written to the log.

### Seeded data

- Ride categories: `saver`, `economy`, `comfort`, `family`, `premium`, `airport`.
- Document types: `national_id`, `driving_license`, `vehicle_registration`, `insurance`, `profile_photo` (all required).
- City: `riyadh`.
- Admin account: username `admin`, password `Admin@12345` (`POST /api/v1/auth/admin/login`).

## API summary (`/api/v1`, JSON camelCase, `Accept-Language: ar|en`, errors as `{ "error": { code, message, details } }`)

| Area | Endpoints |
|---|---|
| Auth | `POST /auth/otp/request`, `POST /auth/otp/verify`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/admin/login` |
| Me | `GET/PATCH/DELETE /me`, `GET/PUT /me/notification-preferences`, `PUT /me/devices` |
| Catalog | `GET /catalog/ride-categories`, `GET /catalog/document-types`, `GET /catalog/cities` |
| Passenger | `GET /passenger/trips`, `GET /passenger/saved-places`, `PUT/DELETE /passenger/saved-places/{label}`, `PATCH /passenger/preferences` |
| Wallet | `GET /wallet`, `GET /wallet/transactions`, `POST /wallet/topups` (requires `Idempotency-Key`) |
| Driver | `GET /driver/application`, `PUT /driver/application/profile`, `PUT /driver/application/vehicle`, `POST /driver/documents` (multipart), `DELETE /driver/documents/{id}`, `POST /driver/application/submit`, `GET/PUT /driver/status`, `GET /driver/earnings/summary`, `GET /driver/trips` |
| Notifications | `GET /notifications`, `POST /notifications/read` |
| Files | `GET /files/{id}` (owner or admin, inline) |
| Admin | `GET /admin/dashboard/summary`, `GET /admin/drivers`, `GET /admin/drivers/{id}`, `POST /admin/drivers/{id}/{review,approve,reject,suspend,reinstate}`, `POST /admin/documents/{id}/verify`, `GET /admin/passengers`, `POST /admin/users/{userId}/{suspend,reinstate}`, `GET/POST /admin/ride-categories`, `PUT/DELETE /admin/ride-categories/{id}`, `GET /admin/audit-logs` |
| System | `GET /health` (MySQL check), `GET /openapi/v1.json`, `GET /docs` (Development) |

Roles: `passenger`, `driver`, `admin`, `operations` (JWT `roles` claim). Admin actions are recorded in `audit_logs`
and driver status changes create a notification row for the driver. Approving a driver requires every required
document type to be `verified`.

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
