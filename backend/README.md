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
F11/F13 add `Modules/Payments` (gateway abstraction with a sandbox and a Moyasar-style adapter, saved cards, card trips with
authorize/capture and cash fallback, card top-ups, webhooks, receipts, refunds with four-eyes approval, payouts and bank batches,
settlement statements, admin wallets/ledger) and `Modules/Notifications` (event catalogue, templates, `INotificationDispatcher`,
OneSignal push + SMS deliveries with retries, broadcast campaigns, document-expiry scanner).
F12/F14 add `Modules/Safety` (trip sharing with a public tracking page, trusted contacts, SOS → safety cases with on-duty ops fan-out,
masked in-trip chat, anomaly detection jobs with the "are you OK?" cycle, safety reports, lost items, the admin safety centre) and
`Modules/Cancellation` (reason catalogue, stage rules with free windows, fees and driver compensation through the F11 ledger, no-show,
excuse reviews, rolling reliability profiles with a restriction ladder that feeds the matcher and blocks requests / going online, KPIs).
F15 adds `Modules/Ratings` (two-way trip ratings with tags, a rolling weighted average, flags for ops), `Modules/Promotions` (promo codes with
restrictions, the unified discount engine, reservation at request / application at completion / release on cancellation, discounts in the fare
breakdown, receipt and ledger) and `Modules/Incentives` (weekly driver tiers with commission discounts and matcher weights, driver incentives /
quests with progress, payouts to the driver wallet reduced by the F14 reliability multiplier).
F16 adds `Modules/Favorites` (favourite drivers saved after a completed trip, the "available favourites" list, a trip request with `favoriteDriverId` that first offers the
trip exclusively to that driver, the favourite-driver discount rules that fill the favourite slot of the discount engine, admin rules CRUD and KPIs).
F17 adds `Modules/Scheduling` (scheduled rides up to 7 days ahead with the new `scheduled` trip status, the driver marketplace and reservations with confirmation
timeline, reminders, cancellation with a late fee, admin rules / dashboard) and `Modules/Airports` (airport geofence, terminals and pickup zones, flight number,
waiting policy, the FIFO driver queue of the waiting area, admin CRUD) — see "Scheduled rides and airport (F17)".
F18 adds `Modules/Support` (the public help center with FULLTEXT search, rider / driver support tickets with messages and attachments, agent console with internal notes, canned responses,
SLA policies with pause while waiting for the user, fare disputes resolved through the F11 refund service, CSAT, KPIs, auto-close and SLA-monitor jobs) and links the F12 lost items / safety
cases to tickets — see "Support (F18)".
F19 adds `Modules/Corporate` (corporate accounts: company admins signing in to the business portal by OTP, employee invitations / CSV import, travel policies and cost centres,
policy-checked corporate trips and guest bookings made from the portal, monthly VAT invoices with a ZATCA-style QR PDF, receivables through the F11 ledger, reports / CSV exports, API keys
and the platform-admin console) — see "Corporate accounts (F19)".
F20 adds `Modules/Rbac` (roles with a permission matrix synced from the code catalogue, admin users with temporary passwords, per-endpoint `RequirePermission` on every
`/admin/*` route, `/admin/me` with sessions and recovery codes), TOTP MFA and admin session limits in `Modules/Identity`, and `Modules/Reporting` (the unified KPI definitions,
daily `report_snapshots`, KPI / series / breakdown APIs and CSV exports) — see "Roles, permissions, MFA and reports (F20)".

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
| `Payments:SandboxEnabled` | `true` in Development: `POST /wallet/topups` with `method: "sandbox"` credits the wallet without a `payments` row |
| `Payments:Provider` | `sandbox` (default, Development/tests) or `moyasar`; existing payments keep their provider |
| `Payments:SandboxSkipAuthorization`, `Payments:CardAuthorizeOnRequest` | Skip the authorization at request time (card trips are then charged with a purchase on completion) — defaults `false` / `true` |
| `Payments:AuthBufferPercent` | Card trip authorization = `ceil(estimatedFare × (1 + x/100))` (default 30) |
| `Payments:ActionTimeoutSeconds` | 3-D Secure action lifetime; expired → payment `failed`, trip `cancelled (payment_failed)` (default 300) |
| `Payments:GatewayTimeoutSeconds`, `Payments:CaptureTimeoutSeconds`, `Payments:CaptureMaxAttempts` | Gateway timeouts 15 / 10 s; capture retries before the fare becomes a passenger wallet debt (5) |
| `Payments:MinTopup`, `Payments:MaxTopup`, `Payments:MaxTopupPerDay` | 10 / 5000 / 10000 SAR (daily limit → `422 validation_failed { amount: "daily_limit" }`) |
| `Payments:MaxCardsPerUser`, `Payments:RefundAutoApproveLimit`, `Payments:BlockOnOutstandingBalance` | 5 cards; refunds ≥ 50 SAR need a second admin; negative passenger balance blocks trip requests (`true`) |
| `Payments:PublicBaseUrl`, `Payments:DefaultReturnUrl` | Base URL for gateway callbacks and the sandbox challenge page (`https://api.ata.sa`); app return URL (`ata://payments/return`) |
| `Payments:JobsEnabled` | Runs the payment jobs (action expiry, capture retry, authorization reconcile, webhook retry, refund processor, weekly settlement) — `false` in tests |
| `Payments:Sandbox:WebhookSecret`, `Payments:Sandbox:SignatureHeader` | Sandbox webhook HMAC-SHA256 key and header (`X-Sandbox-Signature`, hex of the raw body) |
| `Payments:Moyasar:BaseUrl`, `:PublishableKey`, `:SecretKey`, `:WebhookSecret`, `:SignatureHeader`, `:ApplePayMerchantId` | Moyasar-style adapter (secrets via environment); without `SecretKey` no HTTP call is made (`payment_provider_unavailable`) |
| `Payouts:MinAmount`, `Payouts:MaxCashDebt`, `Payouts:AutoApprove`, `Payouts:AutoApproveLimit` | 100 SAR minimum payout; cash debt above 500 SAR blocks going online and matching; auto-approval off / ≤ 1000 |
| `Settlements:PeriodDays`, `Settlements:AutoGenerate`, `Settlements:AutoCreatePayouts` | 7-day periods (Sunday 00:00 Riyadh); weekly job on; payouts from finalized statements off |
| `OneSignal:Enabled`, `OneSignal:AppId`, `OneSignal:RestApiKey` | Push via OneSignal REST (External ID = `users.id`); without `Enabled=true` **and** both keys the `LoggingPushSender` is used |
| `OneSignal:ApiBaseUrl`, `OneSignal:AndroidChannels:{trips,offers,safety,wallet,promotions,system}` | `https://api.onesignal.com`; Android channel ids per category |
| `Sms:Provider`, `Sms:Sandbox`, `Sms:SenderName` | `logging` \| `unifonic` \| `taqnyat`; `Sandbox=true` (default) forces the logging sender (no HTTP); sender `ATA` |
| `Sms:Unifonic:BaseUrl`, `Sms:Unifonic:AppSid`, `Sms:Taqnyat:BaseUrl`, `Sms:Taqnyat:BearerToken` | SMS gateway adapters (placeholders covered by contract tests) |
| `Notifications:WorkerEnabled`, `Notifications:JobsEnabled` | Delivery worker and campaign/document-expiry jobs (`false` in tests, which call them directly) |
| `Notifications:WorkerPollSeconds`, `Notifications:MaxAttempts`, `Notifications:PushBatchSize` | 5 s poll; 5 attempts with `30 s × 2^(n−1)` back-off; 2000 users per OneSignal request |
| `Notifications:CampaignPushPerMinute`, `Notifications:CampaignPollSeconds` | 60 campaign pages per pass; campaign sender every 30 s |
| `Notifications:DocumentExpiryOffsetsDays`, `Notifications:DocumentScanHourLocal` | `[30, 7, 1]` (SMS on the last one); daily scan at 06:00 Riyadh |
| `RateLimiting:OtpPerIpPerHour` | IP-level limit on the OTP endpoints (default 10) |
| `Admin:Username`, `Admin:Password` | Seeded development admin (override in production); it receives the `super_admin` role |
| `Admin:MfaRequired`, `Admin:MfaTokenMinutes` | F20: every admin must enrol TOTP (`true`; `false` in `appsettings.Development.json` so the seed admin signs in with the password only) / lifetime of the `mfaToken` between the login steps (5) |
| `Admin:MaxFailedLogins`, `Admin:LockoutMinutes`, `Admin:MaxMfaAttempts` | 5 wrong passwords → `429 account_locked { retryAfterSeconds }` for 15 min; 5 wrong MFA codes → `429 mfa_locked` (a new password login starts over) |
| `Admin:AccessTokenMinutes`, `Admin:SessionAbsoluteHours`, `Admin:SessionIdleMinutes`, `Admin:JobsEnabled` | Admin access tokens 15 min; a session ends 12 h after the login or after 30 min without a refresh; `AdminSessionCleanupJob` (hourly) |
| `Reports:MaxRangeDays`, `Reports:MaxExportRows`, `Reports:RecomputeTrailingDays` | 366 days per report (`422 report_range_too_large { maxDays }`), 100 000 CSV rows (`{ maxRows }`), `ReportSnapshotJob` recomputes the 3 days before yesterday |
| `Reports:SnapshotHourLocal`, `Reports:SnapshotMinuteLocal`, `Reports:JobsEnabled` | `ReportSnapshotJob` runs daily at 01:30 Riyadh |
| `Rbac:SyncOnStartup` | Outside Development the API syncs the permission catalogue and the system roles at start (`true`; Development does it in `DataSeeder`) |
| `Trips:FreeWaitingMinutes` | Free waiting at the pickup before waiting time is billed (default 3) |
| `Trips:ArrivalRadiusMeters` | "Arrived" reported farther than this from the pickup adds an `arrival_distance_warning` event (default 300) |
| `Trips:PinMaxAttempts` | Wrong-PIN attempts before `pin_locked` (default 5) |
| `Trips:ScheduledLeadMinutes` | Removed in F17 (ignored): the search of a scheduled trip starts at `scheduled_ride_rules.search_start_minutes_before` (10) |
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
| `Safety:ShareBaseUrl`, `Safety:ShareExpiryMinutesAfterEnd`, `Safety:MaxSharesPerTrip` | Public link base (`https://ata.sa` → `/t/{token}`), link lifetime after the trip ends (30 min), links per trip (10) |
| `Safety:AutoShareSmsEnabled`, `Safety:PublicShareRatePerMinute`, `Safety:PublicShareRefreshSeconds` | SMS for automatic shares on assignment (`true`); public page limit per IP (30/min → `429 rate_limited`); polling interval returned to the page (10 s) |
| `Safety:EmergencyNumber`, `Safety:SosDedupMinutes`, `Safety:SosLocationIntervalSeconds`, `Safety:OpsHotlinePhones` | `911`; repeated SOS within 10 min returns the same case; app location interval (10 s); numbers texted on every SOS / escalated alert (`[]`) |
| `Safety:ChatMaskPhoneNumbers` | Replaces any run of ≥ 8 digits in chat messages with `••••` (`true`) |
| `Safety:MonitorIntervalSeconds`, `Safety:StopSpeedMps`, `Safety:StopMinutes`, `Safety:StopIgnoreRadiusMeters` | Monitor every 30 s; unexpected stop = speed < 1.5 m/s for > 5 min farther than 300 m from pickup, stops and dropoff |
| `Safety:DeviationMeters`, `Safety:DeviationMetersMaps`, `Safety:DeviationSeconds` | Route deviation > 2000 m (straight planned route) / 500 m (maps route) for > 120 s |
| `Safety:OverrunFactor`, `Safety:OverrunMinMinutes`, `Safety:AlertCooldownMinutes`, `Safety:CheckResponseSeconds` | Overrun = elapsed > estimate × 2.0 and > estimate + 15 min; 10 min cooldown per (trip, type) after a closed alert; 120 s to answer "are you OK?" |
| `Safety:ReportWindowDays`, `Safety:LostItemWindowDays`, `Safety:JobsEnabled` | Safety reports / lost items accepted 7 days after the trip; runs the safety jobs (`false` in tests) |
| `CallMasking:Provider` | `none` (default): `POST …/call` answers `{ mode: "unavailable", available: false }`; the real number is never returned |
| `Cancellation:NoShowWaitMinutes`, `Cancellation:ExcuseReviewSlaHours` | Wait at the pickup before a no-show (5 min, never below the free waiting time); excuse queue SLA (48 h) |
| `Reliability:WindowDays`, `Reliability:PointsExpiryDays`, `Reliability:RecalcHourLocal`, `Reliability:JobsEnabled` | Rolling window (30 days), penalty point lifetime (30 days), nightly recalculation at 02:00 Riyadh, jobs switch (`false` in tests) |
| `Retention:TripMessagesDays` | Chat messages older than this are purged by the share-expiry job (180) |
| `Ratings:WindowHours`, `Ratings:WindowSize`, `Ratings:MinWeight` | Rating window after completion (72 h); weighted average over the last 500 visible ratings, the oldest weighing 0.5 |
| `Ratings:LowRatingThreshold`, `Ratings:DriverMinAverage`, `Ratings:MinCountForAverageFlag` | `low_rating` flag at ≤ 2 stars; `low_average` flag for drivers under 4.30 with ≥ 50 ratings |
| `Ratings:ReminderAfterMinutes`, `Ratings:AbusiveWords`, `Ratings:JobsEnabled`, `Ratings:LowAverageHourLocal` | `rating.reminder` 30 min after completion; words that hide a comment and raise `abusive_comment` (`[]`); jobs switch (`false` in tests); daily flag job at 04:00 Riyadh |
| `Promotions:MinPayableFare` | Discounts never take the fare below this (0) |
| `Tiers:PeriodDays`, `Tiers:RecalcDayOfWeek`, `Tiers:RecalcHourLocal` | Completed trips counted over 28 days; weekly recalculation on Sunday (0) at 03:00 Riyadh |
| `Incentives:PayoutDelayHours`, `Incentives:JobsEnabled` | Achieved periods are paid 2 h after they end; runs the period / payout / tier jobs (`false` in tests) |
| `Favorites:MaxPerPassenger`, `Favorites:ExclusiveOfferTimeoutSeconds` | Favourite drivers per passenger (20 → `422 favorites_limit`); seconds the favourite has to answer the exclusive first offer (30) |
| `Scheduling:JobsEnabled`, `Scheduling:WorkerIntervalSeconds`, `Scheduling:ReminderIntervalSeconds` | Runs `ScheduledRideWorker` (confirmation timeline, search start, no-shows; 30 s) and `ScheduledReminderJob` (60 s); `false` in tests, which call `RunOnceAsync` |
| `Airport:JobsEnabled`, `Airport:JobIntervalSeconds`, `Airport:QueueExitGraceSeconds` | Runs `AirportQueueJob` (30 s); a queue entry not refreshed by a location update within 180 s leaves the queue (`exited_area` / `offline`) |
| `Airport:RejectAction`, `Airport:QueueMaxOffers` | Driver rejects / lets an airport-queue offer expire: `move_to_back` (default) or `remove`; single-driver queue offers per trip before the normal search (5) |
| `Support:AutoCloseDays`, `Support:DisputeWindowDays`, `Support:MaxAttachmentsPerMessage` | A `resolved` ticket without a user reply closes after 3 days; a fare can be disputed 14 days after the trip (inclusive); attachments per message (5 → `422 attachment_limit`) |
| `Support:JobsEnabled`, `Support:AutoCloseIntervalMinutes`, `Support:SlaMonitorIntervalMinutes` | Runs `SupportAutoCloseJob` (hourly) and `SupportSlaMonitorJob` (every 5 min); `false` in tests, which call `RunOnceAsync` |
| `Favorites:AvailabilityRadiusMeters` | Radius of `/passenger/favorite-drivers/available` and of the exclusive round; `null` (default) = the zone/category `matching_settings.radius_meters` (5000 by default) |
| `Corporate:PortalBaseUrl`, `Corporate:InvitationDays` | Base of the portal links sent in invitations / invoice e-mails (`https://ata.sa` → `/business/join/{token}`); invitation lifetime (7 days, then `410 invitation_expired`) |
| `Corporate:InvoiceDayOfMonth`, `Corporate:InvoiceHourLocal`, `Corporate:AutoIssueInvoices` | `CorporateInvoiceJob` generates the previous month's draft invoices on day 1 at 04:00 Riyadh; `AutoIssueInvoices=true` also issues them (default `false`: drafts wait for an admin) |
| `Corporate:SuspendAfterOverdueDays` | Suspends a company whose invoice is overdue for this many days (`0` = never) |
| `Corporate:MaxActiveGuestTripsPerAdmin`, `Corporate:MaxImportRows` | Guest trips one admin may have in flight (10 → `409 trip_active_exists` with `details.reason = guest_trips_limit`); rows accepted by the CSV import (500) |
| `Corporate:ApiKeysEnabled` | Enables `/corporate/api-keys` (default `false` → `404`); keys are stored hashed and shown once |
| `Corporate:SellerLegalNameAr`, `SellerLegalNameEn`, `SellerVatNumber`, `SellerAddress` | Seller block printed on the invoice PDF and encoded in its ZATCA TLV QR |
| `Corporate:JobsEnabled`, `Corporate:JobIntervalMinutes` | Runs the invoice, overdue and invitation-expiry jobs (15 min loop; `false` in tests, which call `RunOnceAsync`) |
| `Email:Provider`, `Email:From` | `logging` (default): invoice / invitation e-mails are written to the log through `IEmailSender`; replace the registration to send real mail |
| `Seed:DemoCorporate` | Seeds the demo company (Development only, `true` in `appsettings.Development.json`) |

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
- Admin account: username `admin`, password `Admin@12345` (`POST /api/v1/auth/admin/login`), role `super_admin` (`*`). With `Admin:MfaRequired=false` (Development) it signs in
  with the password only; with `true` the first login asks for the TOTP enrolment.
- F20 permissions (synced from `PermissionCatalog` at every start: added / renamed / removed) and the system roles `super_admin`, `operations_manager`, `finance`, `support_agent`,
  `analyst` (created once when missing; their permissions stay editable, their code and existence do not).
- Notification templates: one row per catalogue event and default channel (ar/en), never overwritten once present.
- Cancellation reasons (doc 09 §F14.6, unique per `(actor, code)`): passenger `changed_mind`, `driver_late`, `driver_too_far`, `wrong_pickup`, `found_other_ride`,
  `driver_asked_to_cancel` (excusable), `driver_not_moving` (excusable), `safety_concern` (emergency), `other` (note required); driver `passenger_not_responding`,
  `passenger_asked_to_cancel` (excusable), `wrong_pickup_location`, `pickup_too_far`, `vehicle_issue` (excusable), `safety_concern` (emergency), `other` (note),
  `passenger_no_show` (not selectable); system `no_drivers`, `payment_failed`, `admin_cancelled`, `scheduled_driver_unavailable`. The four codes of the F8 app
  (`changed_mind`, `driver_late`, `wrong_pickup`, `other`) are valid in every passenger stage.
- Cancellation rules (city-wide, seeded while the table is empty): passenger before accept free; after accept 120 s free then 5 SAR (50 % to the driver, 1 point);
  en route 120 s free then 10 SAR (70 %, 2); arrived pricing-rule fee (80 %, 2); waiting / no-show pricing-rule fee min 10 SAR (80 %, 3 / 4); driver after accept
  60 s window (2 points), en route 3, arrived 4, waiting 2.
- Reliability thresholds: driver warning 4 pts / 10 %, matching_deprioritized 8 / 15 % (×0.70), incentives_reduced 12 / 20 % (×0.60, −50 %),
  temporarily_restricted 18 / 30 % (24 h), suspended 30 / 45 %; passenger warning 4 / 15 %, temporarily_restricted 12 / 35 % (24 h), suspended 25 / 50 %
  (rates need ≥ 10 accepted trips).
- Rating tags (unique per rated role): driver `driving`, `cleanliness`, `behaviour`, `navigation`, `vehicle_condition`; passenger `punctuality`, `behaviour`, `cleanliness`.
- Driver tier rules (added per tier when missing): bronze 0 trips / 0 / 0 / ≤ 1.00, 0 %, norm .25; silver 60 / 4.70 / 0.80 / ≤ 0.08, 5 %, .50;
  gold 150 / 4.80 / 0.85 / ≤ 0.05, 10 %, .75; platinum 250 / 4.90 / 0.90 / ≤ 0.03, 15 %, 1.00.
- Promo codes (added by code when missing, valid 2026-01-01 → 2027-12-31): `WELCOME` (20 %, max 15 SAR, first trip only, once per user, public) and
  `ATA10` (10 SAR, min fare 30, 3 per user, not stackable, public).
- Favourite-driver discount rule "خصم الكابتن المفضل" (seeded once while the table is empty and no rule was ever created / deleted by an admin): 10 %, max 10 SAR,
  not stackable, every category / zone / booking type, valid from 2026-01-01, priority 0.
- Incentive (seeded once while the table is empty): "10 رحلات مساء الخميس والجمعة" — weekly, Riyadh, Thursday/Friday 16:00–23:59, 10 trips → 75 SAR.
- F17 (seeded once while the tables are empty and no audit entry exists): the global `scheduled_ride_rules` row (7 days, 30 min lead, 3 open trips, demand locked to normal, free cancel
  60 min, late fee fixed 10 SAR, driver compensation 50 %, first confirmation T−60 / 10 min, final T−15 / 5 min, search from T−10, reminders `[1440,60,15]` rider / `[1440,180]` driver,
  driver free release 120 min, penalties 3 / 3 / 6, no-show grace 10 min, 5 reservations per driver, 30 min gap) and an **approximate** King Khalid airport `RUH` (geofence box around
  24.925–24.99 N, 46.67–46.73 E; flagged approximate, refine it in the admin): terminals T1–T5 with two pickup zones each (`T1-P1` … `T5-P2`), the waiting area `WAIT-1` near
  (24.9355, 46.679), default free waiting 15 min and the queue enabled. The `airport` ride category (seeded earlier) only appears in quotes for airport trips.
- F18 (seeded by code / priority when missing; the help center and the canned responses once, while their tables are empty and no admin ever created or deleted a row): SLA policies
  (first response / resolution minutes) urgent 15 / 240, high 60 / 1440, normal 240 / 2880, low 1440 / 4320; help categories `trips`, `payments`, `safety`, `account`, `drivers` (audience `driver`)
  with 14 published bilingual articles (2–3 per category; riders see the four general categories, drivers `safety`, `account` and `drivers`); canned responses `greeting`, `need_more_info`,
  `refund_approved`, `refund_rejected`, `lost_item_contacted`, `closing` (placeholders `{userName}` `{ticketNumber}` `{tripNumber}`).
- F19 demo company (only with `Seed:DemoCorporate=true`, i.e. Development; seeded once when missing): "شركة ATA التجريبية" (`CA-00001`, active, credit limit 20 000 SAR, 30-day terms), default policy
  "السياسة العامة" (economy / comfort, Sunday–Thursday 06:00–23:00 Riyadh, max fare 150, monthly budget 1 500, purpose required) and "الإدارة التنفيذية" (budget 5 000), cost centres `FIN-01`, `SAL-01`, `IT-01`,
  the company admin Sara Al-Harbi `+966500000901`, active employees Ahmed `+966500000902` (finance) and Khaled `+966500000903` (sales, budget 400) and a pending invitation for `+966500000904`. Everyone signs in by OTP
  (`devCode` in Development). Notification templates `corporate.*` (ar / en) are seeded like every other event.

## API summary (`/api/v1`, JSON camelCase, `Accept-Language: ar|en`, errors as `{ "error": { code, message, details } }`)

| Area | Endpoints |
|---|---|
| Auth | `POST /auth/otp/request`, `POST /auth/otp/verify`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/admin/login` (F20: `AuthResponse` + `mustChangePassword` \| `{ mfaRequired, mfaToken, methods }` \| `{ mfaEnrollmentRequired, mfaToken }`), `POST /auth/admin/mfa/verify`, `POST /auth/admin/mfa/enroll`, `POST /auth/admin/mfa/enroll/confirm` |
| Me | `GET/PATCH/DELETE /me`, `GET/PUT /me/notification-preferences`, `PUT /me/devices` |
| Catalog | `GET /catalog/ride-categories`, `GET /catalog/document-types`, `GET /catalog/cities` |
| Passenger | `GET /passenger/saved-places`, `PUT/DELETE /passenger/saved-places/{label}`, `PATCH /passenger/preferences` |
| Pricing | `POST /pricing/quote` (passenger; stores `fare_quotes`, 5-minute expiry), `GET /pricing/demand?lat=&lng=` (any authenticated user: current zone + demand level) |
| Passenger trips | `POST /passenger/trips/estimate` (alias of `POST /pricing/quote`), `POST /passenger/trips` (optional `quoteId`, `promoCode`, `favoriteDriverId` (F16: must be one of the rider's favourites, else `422 validation_failed { favoriteDriverId: "not_favorite" }`); 409 `trip_active_exists`, 422 `quote_expired`, 422 `offer_out_of_range` with `details.offerMin/offerMax`), `GET /passenger/trips/active` (`Trip` or `null`), `GET /passenger/trips/{id}` (PIN once a driver is assigned), `POST /passenger/trips/{id}/cancel`, `GET /passenger/trips?status=all|active|completed|cancelled` |
| Wallet | `GET /wallet` (cards in `paymentMethods`; driver wallets add `cashDebt`, `cashDebtLimit`), `GET /wallet/transactions`, `POST /wallet/topups?kind=` (`Idempotency-Key` ≤ 60 chars; `method: sandbox\|card\|apple_pay` + `paymentMethodId`/`applePayToken`; `201` captured or `202 { paymentId, status: "initiated", action }`) |
| Payments | `GET /payments/config`, `GET /payments/{id}` (owner), `POST /payments/webhooks/{provider}` (anonymous, signature checked: `401 webhook_signature_invalid`, dedupe on `(provider, event_id)`), `GET /payments/return/{provider}?id=` (302), `GET/POST /payments/sandbox/challenge/{id}` (sandbox only; payment or pending card) |
| Passenger payments | `GET/POST /passenger/payment-methods` (`201` active / `202 { paymentMethod, action }` for 3-D Secure; `409 conflict` same card; `422 payment_failed`), `POST /passenger/payment-methods/{id}/default`, `DELETE /passenger/payment-methods/{id}` (`409 payment_method_in_use`), `GET /passenger/trips/{id}/receipt`. `POST /passenger/trips` accepts `paymentMethodId` (`422 payment_failed`, `422 payment_method_expired`, `422 outstanding_balance`); `Trip` adds `payment`, `collectCashAmount` (driver only), `discountTotal`; `Offer` adds `paymentMethod` |
| Driver money | `GET /driver/earnings/statement?from=&to=` (≤ 92 local days), `GET /driver/trips/{id}/earnings`, `GET /driver/payouts/summary`, `POST /driver/payouts` (`Idempotency-Key`; `payout_below_minimum`, `insufficient_balance`, `iban_missing`, `payout_pending_exists`, `account_suspended`), `GET /driver/payouts`, `POST /driver/payouts/{id}/cancel`, `GET /driver/settlements`, `GET /driver/settlements/{id}`. Going online with a cash debt above the limit → `403 cash_debt_limit_exceeded { cashDebt, limit }` |
| Driver | `GET /driver/application`, `PUT /driver/application/profile`, `PUT /driver/application/vehicle`, `POST /driver/documents` (multipart), `DELETE /driver/documents/{id}`, `POST /driver/application/submit`, `GET/PUT /driver/status`, `GET /driver/earnings/summary` (completed trips + online hours) |
| Driver trips | `PUT /driver/location` (204; broadcasts `DriverLocation` to the passenger during a trip), `GET /driver/offers/active` (`Offer` or `null`; includes `round`, `passengerOffered`, and since F16 `isFavoriteRequest` / `exclusive`), `POST /driver/offers/{id}/accept` (409 `offer_expired`), `POST /driver/offers/{id}/reject`, `GET /driver/trips/active`, `POST /driver/trips/{id}/en-route`, `/arrived`, `/verify-pin` (400 `pin_invalid` with `attemptsLeft`, 429 `pin_locked`), `/start`, `/complete`, `/cancel`, `GET /driver/trips?status=` |
| Notifications | `GET /notifications?category=` (`type` = event code, legacy types normalised; `category`; `data.deepLink`), `GET /notifications/unread-count`, `POST /notifications/read`, `POST /notifications/{id}/opened` (204) |
| Files | `GET /files/{id}` (owner or admin, inline) |
| Admin self (any admin) | `GET /admin/me`, `POST /admin/me/password`, `POST /admin/me/mfa/recovery-codes`, `POST /admin/me/mfa/enroll`, `POST /admin/me/mfa/enroll/confirm`, `GET /admin/me/sessions`, `DELETE /admin/me/sessions/{id}`, `POST /admin/me/sessions/revoke-others`, `GET/PUT /admin/me/duty` (`safety.manage`) |
| Admin users & roles | `GET/POST /admin/admin-users`, `GET/PUT /admin/admin-users/{id}`, `POST /admin/admin-users/{id}/disable\|enable\|reset-password\|reset-mfa\|unlock\|revoke-sessions`, `GET /admin/admin-users/{id}/sessions`, `DELETE /admin/admin-users/{id}/sessions/{sessionId}` (`admin.users.manage`); `GET /admin/permissions`, `GET/POST /admin/roles`, `GET/PUT/DELETE /admin/roles/{id}` (`admin.roles.manage`) |
| Admin reports | `GET /admin/reports/definitions`, `GET /admin/reports/kpis`, `GET /admin/reports/kpis/{code}/series`, `GET /admin/reports/breakdown` (`reports.view`); `GET /admin/reports/export`, `POST /admin/reports/snapshots/rebuild` (`reports.export`) |
| Admin | `GET /admin/dashboard/summary` (+ F20 `today`), `GET /admin/drivers`, `GET /admin/drivers/{id}`, `POST /admin/drivers/{id}/{review,approve,reject,suspend,reinstate}`, `POST /admin/documents/{id}/verify`, `GET /admin/passengers`, `POST /admin/users/{userId}/{suspend,reinstate}`, `GET/POST /admin/ride-categories`, `PUT/DELETE /admin/ride-categories/{id}` (`fallbackPricing` = flat-pricing columns, `pricingSource`), `GET /admin/audit-logs` |
| Admin pricing | `GET/POST /admin/zones`, `GET/PUT/DELETE /admin/zones/{id}` (nested `zoneCategorySettings`; the city default cannot be deleted), `GET/POST /admin/pricing-rules?rideCategoryId=&zoneId=`, `GET/PUT/DELETE /admin/pricing-rules/{id}` (nested `timeMultipliers`, replaced as a whole on PUT), `POST /admin/pricing/simulate` (quote inputs + `at`, nothing stored), `GET /admin/demand-levels`, `PUT /admin/demand-levels/{id}` (multiplier only), `GET/POST /admin/demand-rules`, `PUT/DELETE /admin/demand-rules/{id}`, `GET /admin/demand-overrides?active=`, `POST /admin/demand-overrides`, `PUT/DELETE /admin/demand-overrides/{id}` (DELETE ends a running override), `GET /admin/demand/current` (level per zone and category with the reading behind it). Every write is audited (`zone.*`, `pricing_rule.*`, `demand_level.update`, `demand_rule.*`, `demand_override.*`) |
| Admin matching | `GET/POST /admin/matching-settings`, `GET/PUT/DELETE /admin/matching-settings/{id}` (audited `matching_settings.*`), `GET /admin/trips/{id}/matching` (rounds with scored candidates and their answers), `GET /admin/matching/stats?from=&to=` (assignment rate and times, `no_drivers` rate, offer acceptance, rounds per trip) |
| Admin trips | `GET /admin/trips?status=&from=&to=&search=&page=`, `GET /admin/trips/{id}` (full events with actor names, offers, route), `POST /admin/trips/{id}/cancel` (audited as `trip.cancel`), `GET /admin/live` (drivers incl. recently-offline, active and searching trips) |
| Admin payments | `GET /admin/payments?status=&purpose=&provider=&method=&from=&to=&search=`, `GET /admin/payments/{id}` (+ `webhookEvents`, `refunds`, `ledger`), `POST /admin/payments/{id}/refunds`, `POST /admin/trips/{id}/refunds`, `GET /admin/trips/{id}/receipt`, `GET /admin/refunds?status=&from=&to=`, `POST /admin/refunds/{id}/approve\|reject\|retry` (`409 four_eyes_required`), `GET /admin/payouts?status=&driverId=&from=&to=`, `POST /admin/payouts/{id}/approve\|reject\|mark-paid`, `POST/GET /admin/payout-batches`, `GET /admin/payout-batches/{id}`, `GET /admin/payout-batches/{id}/export?format=csv`, `POST /admin/payout-batches/{id}/mark-paid`, `POST /admin/settlement-batches` (202; `409 settlement_period_overlap`), `GET /admin/settlement-batches`, `GET /admin/settlement-batches/{id}`, `GET /admin/settlement-batches/{id}/settlements?direction=&search=`, `GET /admin/settlement-batches/{id}/export?format=csv` (UTF-8 BOM), `POST /admin/settlement-batches/{id}/finalize\|regenerate`, `GET /admin/settlements/{id}`, `GET /admin/wallets?kind=&search=&negativeOnly=`, `GET /admin/wallets/{id}`, `POST /admin/wallets/{id}/adjustments\|freeze\|unfreeze`, `GET /admin/ledger/balances?from=&to=`. Audited: `refund.*`, `payout.*`, `payout_batch.*`, `settlement_batch.*`, `wallet.adjust\|freeze\|unfreeze` |
| Admin notifications | `GET /admin/notification-events`, `GET/POST /admin/notification-templates` (`422 unknown_event_code`, `422 template_placeholder_invalid`), `PUT /admin/notification-templates/{id}`, `POST /admin/notification-templates/{id}/preview\|test`, `GET/POST /admin/notification-campaigns`, `GET/PUT/DELETE /admin/notification-campaigns/{id}` (`409 campaign_not_editable`), `POST /admin/notification-campaigns/{id}/schedule\|send-now\|cancel`, `POST /admin/notification-campaigns/audience-preview`, `GET /admin/notification-deliveries?userId=&eventCode=&channel=&status=&campaignId=&from=&to=`, `POST /admin/notification-deliveries/{id}/retry`, `GET/PUT /admin/me/duty`. Audited: `notification_template.*`, `notification_campaign.*`, `notification_delivery.retry` |
| Public | `GET /public/trip-shares/{token}` (anonymous, rate limited: `404 share_not_found`, `410 share_expired`, `429 rate_limited`; no phone, family name, PIN, fare or payment method), `GET /public/trip-shares/{token}/driver-photo` |
| Safety (rider & driver) | `GET/POST /safety/trusted-contacts`, `PUT/DELETE /safety/trusted-contacts/{id}` (`422 trusted_contacts_limit`, `409 trusted_contact_exists`, `400 phone_invalid`, own number → `422 { phoneNumber: "self" }`), `POST/GET /safety/trips/{tripId}/shares` (`{ channel: link\|sms, contactIds? }` → `201 { shares: [...] }` with the links created; `409` outside `driver_assigned…in_trip`), `DELETE /safety/shares/{id}`, `POST /safety/sos` (`201`, or `200` for a repeated press), `POST /safety/sos/{caseId}/location` (204), `POST /safety/sos/{caseId}/cancel`, `POST /safety/reports` (`422 { tripId: "window_closed" }`), `GET /safety/cases`, `GET /safety/cases/{id}`, `GET /safety/alerts/pending` (JSON `null` when none), `POST /safety/alerts/{id}/respond` (`409` unless `pending_rider`) |
| Chat & calls | `GET/POST /passenger/trips/{id}/messages` and `/driver/trips/{id}/messages` (`?after=`; `{ body \| quickReplyCode }`; `409 chat_closed`; non-party `403`), `POST …/messages/read { upToId }` (204), `POST …/call` (`{ mode, available, proxyNumber, pin, expiresAt }`), `GET /catalog/chat-quick-replies?role=` |
| Lost items | `POST /passenger/trips/{id}/lost-items` (`contactPhone` defaults to the rider's phone; `422 lost_item_window_closed`), `GET /passenger/lost-items`, `GET /driver/lost-items?status=` (no rider phone), `POST /driver/lost-items/{id}/respond { found, note? }` (`409` when already answered) |
| Cancellation (rider & driver) | `GET /catalog/cancellation-reasons?actor=&stage=`, `POST /passenger/trips/{id}/cancel/preview`, `POST /passenger/trips/{id}/cancel` (`reasonCode` from the catalogue: `422 cancellation_reason_invalid`, note required → `422 { note: "required" }`, `expectedFee` → `409 cancellation_fee_changed { fee }`), `GET /passenger/reliability`, `POST /driver/trips/{id}/cancel/preview`, `POST /driver/trips/{id}/cancel` (`expectedPenaltyPoints`), `POST /driver/trips/{id}/no-show` (`422 no_show_too_early { secondsRemaining }`), `GET /driver/reliability` (+ `effects { matchingFactor, incentiveMultiplier }`). `Trip` adds `cancellation` (the driver sees `compensation`, not the fee). Restricted users: `POST /passenger/trips` / `PUT /driver/status` → `403 account_restricted { level, restrictedUntil }`; `GET /driver/status` returns `reason: "account_restricted"` + `restrictedUntil` |
| Admin safety (`safety.manage`) | `GET /admin/safety/summary`, `GET /admin/safety/cases?status=&priority=&type=&assignedTo=me\|unassigned\|{userId}&from=&to=&search=`, `POST /admin/safety/cases`, `GET /admin/safety/cases/{id}`, `POST /admin/safety/cases/{id}/assign\|status\|notes\|resolve`, `GET /admin/safety/alerts?status=&type=&tripId=&from=&to=&search=`, `POST /admin/safety/alerts/{id}/dismiss`, `GET /admin/trips/{id}/messages` (audited `trip_messages.view`), `GET /admin/trips/{id}/shares`, `GET /admin/users/{userId}/trusted-contacts` (only while the user is party to an open case; audited `trusted_contacts.view`), `GET /admin/lost-items`, `PATCH /admin/lost-items/{id}` (`safety.manage` or `support.manage`). Audited: `safety_case.assign\|status\|note\|resolve\|create`, `safety_alert.dismiss`, `lost_item.update` |
| Ratings (rider & driver) | `GET /catalog/rating-tags?target=driver\|passenger`, `POST /passenger/trips/{id}/rating` and `/driver/trips/{id}/rating` (`{ stars, tags, comment? }` → `201`; non-party `403`, not completed `409`, `422 rating_window_closed`, `409 rating_exists`, unknown tag `422 { tags: "invalid" }`), `GET /passenger\|driver/ratings/pending`, `GET /passenger\|driver/ratings/summary` (average, distribution, top tags, last 20 comments with the ISO week only). `Trip` adds `myRating`, `canRate`, `rateUntil`, `promotion`; the trip history lists add `driverName` (passenger) / `passengerName` (driver) first names, `myRating`, `canRate`, `rateUntil` |
| Promotions (rider) | `GET /passenger/promotions?status=available\|used\|expired` (default `available`), `POST /passenger/promotions/validate` (`{ code, quoteId?, rideCategoryId?, paymentMethod?, bookingType? }` → `{ valid, promotion, discountAmount, totalBefore, totalAfter }` or `404 promo_not_found`, `422 promo_expired`, `422 promo_usage_limit_reached { scope: total\|budget\|user }`, `422 promo_not_eligible { reason }`); `POST /pricing/quote` accepts `promoCode` (per category `totalBeforeDiscount`, `breakdown.discount`, `breakdown.discounts[]`; top-level `promotion { code, valid, reason }`); `POST /passenger/trips` accepts `promoCode` (reserved with the trip) |
| Driver tiers & incentives | `GET /driver/tier`, `GET /driver/incentives?status=active\|upcoming\|completed`, `GET /driver/incentives/{id}` (+ `zonesPolygons`), `POST /driver/incentives/{id}/opt-in` (`409 incentive_opt_in_closed`); items add `rewardMultiplier` and `effectiveRewardAmount` (reward × current F14 multiplier) |
| Favourite drivers (rider, F16) | `GET /passenger/favorite-drivers` (`[FavoriteDriver]`: `driverId, firstName, photoUrl, ratingAvg, vehicle, rideCategoryCode, tripsTogether, lastTripAt, createdAt`), `POST /passenger/favorite-drivers` (`{ driverId? \| tripId? }` exactly one → `201`; `422 favorite_not_eligible` without a completed trip together, `409 favorite_exists`, `422 favorites_limit`), `DELETE /passenger/favorite-drivers/{driverId}` (204; `404` when not saved), `GET /passenger/favorite-drivers/{driverId}/photo` (only for saved drivers), `GET /passenger/favorite-drivers/available?lat=&lng=&rideCategoryId=` (eligible favourites now: `driverId, firstName, photoUrl, ratingAvg, vehicle, etaMinutes, discount { percent, maxAmount, stackableWithPromotions } \| null, availableNow`; no coordinates) |
| Favourites (driver, F16) | `GET /driver/favorites/count` → `{ count }` (riders who saved the driver) |
| Scheduled rides (F17, rider) | `GET /passenger/scheduling/rules?rideCategoryId=&lat=&lng=` (window, lead, free-cancel minutes, late fee, reminders), `POST /passenger/trips` / `estimate` with `bookingType: "scheduled"` + `scheduledAt` (`422 schedule_window_exceeded { maxScheduledAt }`, `422 schedule_lead_too_short { minScheduledAt }`, `422 scheduled_limit_reached { max }`), `GET /passenger/trips/scheduled` (plain array of `scheduled` trips, soonest first), `GET /passenger/scheduled/{tripId}/driver-photo` (reserved driver only); `POST /passenger/trips/{id}/cancel/preview` / `cancel` use stage `scheduled` (`freeUntil`, `fee`) |
| Scheduled rides (F17, driver) | `GET /driver/scheduled/marketplace?lat=&lng=&from=&to=&page=&pageSize=` (paged, approximate pickup, area names), `GET /driver/scheduled?status=active\|history&page=&pageSize=` (paged `Reservation` envelope), `POST /driver/scheduled/{tripId}/reserve` (`201`; `409 reservation_taken \| reservation_conflict`, `422 reservation_limit_reached`, `404` for a trip the driver cannot see), `POST /driver/scheduled/{tripId}/confirm` (first / final confirmation; `409` outside a confirmation window), `POST /driver/scheduled/{tripId}/release { reason? }` (late release adds points) |
| Airports (F17) | `GET /catalog/airports` (airports, terminals, pickup zones), `GET /passenger/airports/resolve?lat=&lng=`, trip requests / quotes accept `airportPickupZoneId`, `airportTerminalCode`, `flightNumber` (optional, stored only; `422` unless `^[A-Z]{2}\d{1,4}[A-Z]?$` after upper-casing / stripping spaces), `GET /driver/airport-queue`, `POST /driver/airport-queue/join { lat?, lng? }` (`422 not_in_airport_waiting_area`), `POST /driver/airport-queue/leave` |
| Admin scheduling (`scheduling.manage`) | `GET/POST /admin/scheduled-ride-rules`, `PUT/DELETE /admin/scheduled-ride-rules/{id}` (audited `scheduled_ride_rule.*`; delete deactivates), `GET /admin/scheduled-trips?from=&to=&reservation=none\|reserved\|confirmed\|assigned&cityId=&rideCategoryId=&zoneId=&atRisk=&page=&pageSize=` (≤ 200), `POST /admin/scheduled-trips/{tripId}/assign { driverId }`, `POST /admin/scheduled-trips/{tripId}/release-reservation { reason }` (both idempotent-safe), `GET /admin/scheduling/stats?from=&to=`; the admin trip detail carries `scheduling { reservations[], reminders[] }` and `airport` |
| Admin airports (`airport.manage`) | `GET/POST /admin/airports`, `GET/PUT/DELETE /admin/airports/{id}`, `GET/POST /admin/airports/{id}/zones`, `PUT/DELETE /admin/airports/{id}/zones/{zoneId}`, `GET /admin/airports/{id}/queue`, `DELETE /admin/airports/{id}/queue/{entryId}` (`{ reason }` body; audited `airport.*`) |
| Admin favourites (`favorites.manage`) | `GET/POST /admin/favorite-discount-rules`, `GET/PUT/DELETE /admin/favorite-discount-rules/{id}` (audited `favorite_discount_rule.create\|update\|delete`; `discountPercent` 1–50 with ≤ 2 decimals, `maxDiscountAmount` > 0, `priority` ≥ 0; delete deactivates a rule pinned by trips), `GET /admin/favorites/stats?from=&to=&cityId=` |
| Admin ratings (`ratings.manage`) | `GET /admin/ratings?raterRole=&stars=&flagged=&userId=&tag=&status=visible\|hidden\|flagged&search=&from=&to=`, `POST /admin/ratings/{id}/hide { reason }` / `unhide` (recompute the average), `GET /admin/rating-flags?status=&type=`, `POST /admin/rating-flags/{id}/review { action: dismiss\|warn\|suspension_review, note }` (`409` when already reviewed; `suspension_review` adds `driver.suspension_review` to the driver's history, never suspends). Audited `rating.hide\|unhide`, `rating_flag.review` |
| Admin promotions (`promotions.manage`) | `GET /admin/promotions?status=active\|scheduled\|expired\|inactive&search=`, `POST /admin/promotions` (`409 conflict` for a taken code), `GET/PUT /admin/promotions/{id}` (`code`/`type` locked after the first reservation → `409`), `POST /admin/promotions/{id}/deactivate\|activate`, `GET /admin/promotions/{id}/redemptions?status=`, `GET /admin/promotions/{id}/stats`, `GET /admin/promotion-redemptions?promotionId=&status=&from=&to=`. Audited `promotion.create\|update\|activate\|deactivate` |
| Admin tiers & incentives (`incentives.manage`) | `GET /admin/driver-tier-rules` (+ `driversCount`), `PUT /admin/driver-tier-rules/{id}`, `POST /admin/driver-tiers/recalculate` (`202 { evaluated, changed }`, runs now), `GET /admin/drivers/{id}/tier-history`, `POST /admin/drivers/{id}/tier { tier, reason }`, `GET /admin/drivers/{id}/incentives`, `GET/POST /admin/incentives?status=&cityId=`, `GET/PUT /admin/incentives/{id}`, `POST /admin/incentives/{id}/deactivate\|activate`, `GET /admin/incentives/{id}/progress?status=`, `POST /admin/incentive-progress/{id}/void { reason }` (`409` once paid). Audited `driver_tier_rule.update`, `driver.tier_set`, `incentive.create\|update\|activate\|deactivate`, `incentive_progress.void` |
| Admin cancellation | `GET/POST /admin/cancellation-reasons`, `PUT/DELETE /admin/cancellation-reasons/{id}` (delete deactivates a used reason), `GET/POST /admin/cancellation-rules`, `PUT/DELETE /admin/cancellation-rules/{id}`, `POST /admin/cancellation-rules/simulate`, `GET /admin/reliability-thresholds?role=`, `PUT /admin/reliability-thresholds/{id}` (`cancellation.manage`); `GET /admin/cancellations?actor=&stage=&atFault=&feeStatus=&excuseStatus=&from=&to=&search=` (`trips.view`); `GET /admin/cancellations/excuses?status=` (+ `ageHours`, `slaBreached`, `pendingPenaltyPoints`), `POST /admin/cancellations/{eventId}/review { decision, note }` (`cancellation.review`); `GET /admin/reliability-profiles?role=&level=&search=`, `GET /admin/reliability-profiles/{userId}?role=`, `POST /admin/reliability-profiles/{userId}/adjust` (`reliability.manage`); `GET /admin/cancellations/stats?from=&to=&cityId=&zoneId=&rideCategoryId=` (`reports.view`); `POST /admin/trips/{id}/cancel { reason, atFault?, chargeFee? }` (`trips.cancel`). Audited: `cancellation_reason.*`, `cancellation_rule.*`, `reliability_threshold.update`, `cancellation.review`, `reliability.adjust`, `reliability.level_change` (system) |
| Help center (F18, public) | `GET /help/categories?audience=passenger\|driver` (active categories with ≥ 1 published article for the audience: `id, code, name, icon, articlesCount`), `GET /help/articles?categoryId=&q=&audience=&sort=&page=&pageSize=` (published only; page 1 without `q` = most read, inside a category the editorial order; `sort=popular\|newest\|order`; `q` = MySQL FULLTEXT boolean search, `LIKE` on SQLite; items `id, slug, title, excerpt (160 chars, no Markdown), categoryId, updatedAt` in the `Accept-Language`), `GET /help/articles/{slug}` (`body` Markdown, `category`, `tags`, `related`, counts a view; `404` for unpublished), `POST /help/articles/{id}/feedback { helpful }` (`204`; one vote per IP / article / Riyadh day, then `429 rate_limited { retryAfterSeconds }`) |
| Support (F18, rider & driver) | `POST /support/attachments` (multipart `file`, jpg / png / pdf ≤ 10 MB → `201 { fileId, fileName, contentType, sizeBytes }`; `422 unsupported_file_type \| file_too_large`), `POST /support/tickets { type, tripId?, subject, message, fileIds?, dispute?: { reason, requestedRefundAmount? }, lostItem?: { itemCategory?, contactPhone? } }` (`201 TicketDetail`; `422 validation_failed { tripId: "required" }` for `trip_issue` / `payment_issue` / `lost_item`, `403` when not a party of the trip, `409 dispute_exists`, `422 dispute_window_closed`, `422 attachment_limit`), `GET /support/tickets?status=open\|closed&page=&pageSize=` (`open` = every non-closed), `GET /support/tickets/{id}` (public messages only; resets the unread counter), `POST /support/tickets/{id}/messages { body, fileIds? }` (`201`; `409 ticket_closed`), `POST /support/tickets/{id}/csat { score 1-5, comment? }` (`204`; `409 conflict` when not resolved / already rated). `GET /files/{id}` is also open to the requester for the files agents attached to public messages |
| Admin support (F18) | `GET /admin/support/summary`, `GET /admin/support/stats?from=&to=` (KPIs), `GET /admin/support/tickets?status=&type=&priority=&channel=&requesterUserId=&assignedTo=me\|unassigned\|{userId}&sla=breached\|due_soon&search=&from=&to=&page=&pageSize=`, `GET /admin/support/tickets/{id}`, `GET /admin/support/disputes?status=&tripId=&page=` (`support.view`); `POST /admin/support/tickets` (phone ticket for a user), `POST …/tickets/{id}/messages { body, fileIds?, isInternal, cannedResponseCode? }`, `POST …/assign { userId \| null }`, `…/status { status: pending_user\|in_progress\|resolved\|closed, note? }`, `…/priority { priority }`, `…/type { type }`, `GET/POST /admin/canned-responses`, `PUT/DELETE /admin/canned-responses/{id}`, `GET/PUT /admin/support/sla-policies` (`support.manage`); `POST /admin/support/disputes/{id}/resolve { resolution: refund_full\|refund_partial\|no_refund, amount?, note }` (`support.disputes`; answers the dispute with its F11 `refund`). Audited `support_ticket.create\|assign\|status\|priority\|type`, `support_dispute.resolve`, `canned_response.*`, `support_sla.update` |
| Admin help center (F18, `help.manage`) | `GET/POST /admin/help/categories`, `PUT/DELETE /admin/help/categories/{id}` (`409` while it has articles), `GET /admin/help/articles?categoryId=&audience=&published=&q=&page=&pageSize=` (full rows incl. bodies, `categoryName`, `viewCount`, `helpfulYes`, `helpfulNo`), `POST /admin/help/articles`, `GET/PUT/DELETE /admin/help/articles/{id}`, `POST /admin/help/articles/{id}/publish\|unpublish`. Audited `help_category.*`, `help_article.create\|update\|delete\|publish\|unpublish` |
| Realtime | SignalR hub `/hubs/trips` (JWT via `?access_token=`): `TripUpdated`, `DriverLocation` (passenger), `OfferReceived`, `OfferExpired`, `TripUpdated` (driver), `LiveSnapshot` every 5 s + `TripUpdated` + `DemandChanged` + `PayoutRequested` (`admins` group), `PaymentUpdated`, `NotificationCreated` (user); F12: `TripMessage`, `TripMessagesRead` (trip parties), `SafetyCheck` (passenger), `SafetyCaseOpened`, `SafetyCaseUpdated`, `SafetyAlertRaised` (`admins`); F18: `SupportTicketUpdated { ticketId, status, lastMessageAt, unread }` (the requester) and `SupportTicketUpdated { ticketId, status, priority, lastMessageBy, slaState? }` + `SupportTicketCreated(ticketSummary)` (`admins`; `slaState` only from the SLA monitor) |
| System | `GET /health` (MySQL check), `GET /openapi/v1.json`, `GET /docs` (Development) |

Roles: `passenger`, `driver`, `admin`, `operations` (JWT `roles` claim). F11/F13 admin endpoints also check the JWT `perm` claim
(`payments.view`, `payments.refund`, `payments.refund_approve`, `payouts.approve`, `settlements.manage`, `wallets.adjust`, `notifications.view`,
`notifications.manage`, `notifications.sms_broadcast`, `safety.manage`, and since F12/F14 `support.manage`, `trips.view`, `trips.cancel`, `cancellation.manage`,
`cancellation.review`, `reliability.manage`, `reports.view`, and since F15 `ratings.manage`, `promotions.manage`, `incentives.manage`, and since F16 `favorites.manage`, and since F17 `scheduling.manage`, `airport.manage`, and since F18 `support.view`, `support.disputes`, `help.manage` (`support.manage` from F12); `*` grants all — the seeded admin has `*`). Admin actions are recorded in `audit_logs`
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
  `finalDistanceMeters`/`finalDurationSeconds`. Scheduled trips are `scheduled` until the F17 search window (see "Scheduled rides and airport (F17)").
- Payment on completion (one transaction with the state change; see "Payments (F11)" for cards):
  - `wallet`: passenger wallet debit (`trip_payment`) and driver wallet credit (`trip_earning`), both against `trip_revenue`.
    If the balance is insufficient the trip still completes: `payment_method` becomes `cash` and a `payment_fallback_cash`
    event is recorded.
  - `cash`: driver wallet credit (`trip_earning`) against `cash_collected`, then `cash_collection` of the whole fare (driver wallet → `cash_collected`,
    overdraft allowed): a 50 SAR cash trip with a 40 SAR share leaves the driver wallet at −10 = commission owed (cash debt).
- Notifications (through `INotificationDispatcher`, event codes `trip.driver_assigned|driver_arrived|started|completed|cancelled|no_drivers`, `offer.received`):
  passenger on driver assigned / arrived / started (push) / completed / cancelled / no drivers; driver on cancellation by the passenger or admin and on each offer (push only).
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
  `fare = subtotal × timeMult × demand + booking_fee`, `fare += fare × service_fee_percent / 100`, `fare −= discount` (the F15 discount engine, below),
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
  `favorite` 1 for the passenger's favourite drivers (`IFavoriteDriverProvider` → `favorite_drivers`, F16; applies to normal rounds without exclusivity or discount).
- Tests drive one pass with `AtaWebApplicationFactory.RunMatcherAsync()` / `RunDemandAsync()`; both loops are disabled in the test host.

### Ledger accounts (doc 08 §F11.3)

Every posting is balanced: a wallet movement (`wallet_transactions` + 2 `ledger_entries` with `transaction_id`) or a journal without a wallet
(`ledger_journals` + 2 entries with `journal_id`); each entry has exactly one of the two. Automatic postings are idempotent by key
(`trip:{id}:payment|earning|cash|capture`, `refund:{id}`, `payout:{id}`, `payout:{id}:reversal`, `payout:{id}:paid`, …).

| Account | Meaning |
|---|---|
| `passenger_wallet:{id}` / `driver_wallet:{id}` | User wallet liabilities (credit = balance grows); a negative balance is a debt of the owner |
| `platform_cash` | Platform bank/cash (payouts leave it) |
| `gateway_clearing` | Card money at the gateway: top-ups and card captures (debit), refunds to cards (credit) |
| `trip_revenue` | Non-cash fares (wallet `trip_payment`, card `trip_card_capture` journal); the driver share (`trip_earning`) is paid out of it, the remainder is commission |
| `cash_collected` | Cash trips: driver share (debit) against the cash fare collected (credit); the net credit is the cash commission |
| `payouts_pending` | Payouts requested and not transferred yet (`payout` credit, `payout_reversal` / `payout_paid` debit) |
| `refunds` | Refunds to passengers (`refund_card` journal or wallet `refund`) |
| `adjustments` | Manual wallet adjustments by admins |
| `cancellation_fees` | F14 cancellation fees (wallet `cancellation_fee` debit or `cancellation_fee_card` journal); driver compensation (`cancellation_compensation`) is paid out of it |
| `discount_promotion` | F15 promo discounts (`trip_discount` journal → `trip_revenue`, or `cash_collected` for cash trips; key `trip:{id}:discount:promotion`) |
| `incentives` | F15 incentive rewards (wallet `incentive` credit to the driver, key `incentive:{progressId}`) |
| `discount_favorite_driver` | F16: favourite-driver discounts (`trip_discount` journal → `trip_revenue` / `cash_collected`, like `discount_promotion`) |
| `corporate_receivable:{id}` | F19: corporate trip charges and cancellation fees (debit), invoice payments (credit) |

Journal types: `trip_card_capture`, `trip_discount`, `trip_corporate_charge`, `cancellation_fee_card`, `refund_card`, `payout_paid`,
`corporate_invoice_payment`, `manual`. Wallet movement types add `cash_collection`, `cancellation_compensation`, `payout_reversal`.

## Payments (F11)

- **Gateways** (`Modules/Payments/Gateways`): `IPaymentGateway` + `IPaymentGatewayResolver`. `SandboxGateway` works from fixed tokens
  (`tok_sandbox_mada|visa|mastercard` succeed, `tok_sandbox_3ds` needs the challenge page, `tok_sandbox_declined|insufficient` fail,
  `tok_sandbox_capture_fail` authorizes but its capture/purchase is declined, `tok_sandbox_timeout` authorizes but its capture/purchase times out,
  `applepay_sandbox`). `MoyasarGateway` is a placeholder adapter (halalas, `source.manual`, `transaction_url`, HMAC or `secret_token` webhooks).
  Gateway calls never run inside a database transaction.
- **Cards**: only provider tokens are stored (`payment_methods`, never PAN/CVV); fingerprint prevents duplicates; removal is logical.
- **Card trips**: authorized before the trip row is inserted; 3-D Secure keeps the trip `requested` until the payment is authorized (then
  `searching`) or the action expires (`cancelled`, `payment_failed`). On completion the fare is captured (void + purchase when above the
  authorization); a final decline falls back to cash (`payment_fallback_cash`, `card_capture_failed`, `payment.failed`, `collectCashAmount` for
  the driver); an unknown result keeps the trip on card with `metadata.capturePending` for `PaymentCaptureRetryJob`, which after
  `CaptureMaxAttempts` debits the passenger wallet (overdraft). Cancellation / no drivers voids the authorization.
- **Webhooks**: signature first (`401` + stored `signature_valid=false`), then `(provider, event_id)` dedupe, then one transaction applying the
  forward-only state transition (stale states → `ignored`) and its effect (top-up credit, trip start/cancel, pending capture journal).
- **Refunds**: fare-dispute refunds (F18) carry `reason_code = fare_dispute` and `dispute_id`; ≤ refundable (`422 refund_exceeds_amount { refundable }`); below `RefundAutoApproveLimit` approved and executed at once,
  otherwise `pending_approval` until another admin approves (`409 four_eyes_required` for the requester).
- **Payouts**: the request debits the wallet immediately; reject/cancel reverses; mark-paid posts `payout_paid`; approved payouts are grouped in
  `payout_batches` exported as CSV (`payout_number, beneficiary_name, iban, amount, currency, reference`).
- **Settlements**: statements per driver for `[start, end)`; `net = earnings + incentives + compensation + adjustments − cash_collected` and
  `closing = opening + net + topups − fees − payouts` (opening/closing = wallet balance at the period bounds). Finalize locks the batch.
- **Receipts**: lines from `trips.fare_breakdown` (stored at completion), zero lines dropped except `base_fare`, a `rounding` line makes them add
  up to `total`; `vatIncluded = round(total × 15/115, 2)`.

## Notifications (F13)

- `INotificationDispatcher.DispatchAsync` (inside the caller's unit of work) resolves the event in `NotificationEvents` (the catalogue of
  doc 08 §F13.2 — codes, category, critical flag, default/allowed channels, placeholders, deep link, default ar/en text), reads the active
  templates (cached, invalidated by the admin endpoints; a missing row falls back to the code default; an inactive row switches the channel off),
  creates the inbox row (`notifications`, with `category` and `data.deepLink`) and `notification_deliveries` for push/SMS. Preferences
  (`trips`, `wallet`, `safety`, `offers` for promotions) only stop push/SMS and never apply to critical events or to `offers`/`system`.
- After `SaveChanges` the delivery ids go to an in-process queue read by `NotificationDeliveryWorker` (also polling queued rows and due retries);
  inbox rows are pushed as `NotificationCreated`.
- `IPushSender`: `OneSignalPushSender` (`POST {ApiBaseUrl}/notifications`, `Authorization: Key …`, `include_aliases.external_id`, headings/contents
  `ar`+`en`, `android_channel_id`, `ttl`, `collapse_id`, `buttons`, `idempotency_key` = delivery id) or `LoggingPushSender`.
  `ISmsSender` returns `SmsSendResult`: `LoggingSmsSender`, `UnifonicSmsSender`, `TaqnyatSmsSender`.
- Campaigns resolve the audience (roles, cities, languages, genders, tiers, last activity, completed trip, or explicit user ids) and send pages of
  2000 users (one OneSignal request per page), resumable and cancellable; `DocumentExpiryScanner` sends `document.expiring` once per offset and
  expires documents (driver taken offline for required ones).
- Legacy notification types (`trip_completed`, …) are migrated to dotted codes by `AddPaymentsAndNotifications` and normalised on read.
- Tests call `RunNotificationWorkerAsync()`, `WithServiceAsync<CampaignService, …>`, `WithServiceAsync<DocumentExpiryScanner, …>` and
  `WithServiceAsync<PaymentJobs, …>`; all background jobs are disabled in the test host.

## Safety (F12)

- **Sharing**: only the trip's passenger, between `driver_assigned` and `in_trip`; tokens are 128 random bits in base64url (22 characters). `channel=sms` creates
  one link per chosen trusted contact and texts it (`safety.trip_shared`); contacts with `auto_share` get an `auto` link (+ SMS) when a driver accepts. Links
  get `expires_at = end + Safety:ShareExpiryMinutesAfterEnd` when the trip completes / is cancelled (`TripShareExpiryJob` catches missed ends); revoked or
  expired → `410`. Views are counted at most once per 60 s per IP. `pin_verified` is shown as `waiting`; `route.travelled` is downsampled to 300 points.
- **Trips** store `planned_route` (JSON `[[lat,lng],…]`, straight segments pickup → stops → dropoff, `planned_route_source = straight`) at creation.
- **SOS**: the role is the caller's role in the trip (else `role` / the single JWT role); a repeated press within `Safety:SosDedupMinutes` returns the same case
  (`200`) with a system note. New case: `sos`, `critical`, `rider_sos|driver_sos`; `SafetyCaseOpened` to `admins`, `safety.alert` push to every on-duty active admin
  holding `safety.manage`, SMS to `Safety:OpsHotlinePhones`; with `notifyTrustedContacts` every `notify_on_sos` contact gets a tracking link and `safety.sos_contact`.
  Location updates push `SafetyCaseUpdated`; a reporter cancel keeps the case open for ops and lowers `critical` to `high`.
- **Chat**: `trip_messages` open from `driver_assigned` to `in_trip`, read-only afterwards; quick replies are a code catalogue (stored in Arabic, returned in the
  viewer's language); each message pushes `TripMessage` + `trip.message` to the other party. `ICallMaskingProvider` (`NoneCallMaskingProvider`) never returns a real number.
- **Monitor** (`SafetyMonitor`): on `in_trip` trips with a live location fresher than `Matching:LocationMaxAgeSeconds`, continuity is measured on
  `driver_location_history` (stop = consecutive segments slower than `Safety:StopSpeedMps`; deviation = points farther than the threshold from the planned route,
  equirectangular projection); overrun only needs `started_at`. One open alert per (trip, type), then `Safety:AlertCooldownMinutes` after it closed. An alert asks
  the passenger (`safety.check` push with `alertId` / `alertType` + `SafetyCheck`) and tells admins (`SafetyAlertRaised`); `need_help` or no answer by `respond_by`
  (`SafetyCheckTimeoutJob`) opens a `high` case (`source = alert`) with the ops fan-out.
- **Reports / lost items**: safety reports within 7 days (`harassment` → `high`, others `medium`, `subject_user_id` = the other party). Lost items on completed trips
  within 7 days (`LI-YYYYMMDD-####`), `lost_item.reported` to the driver, the driver's answer → `found|not_found` + `lost_item.update` to the rider; ops continue
  `driver_contacted → returned → closed`. Since F18 every lost item report opens a linked `lost_item` support ticket
  (`support_ticket_id`; the `lost_item.update` deep link is `ata://support/tickets/{ticketId}`; a system line lands in the ticket on the driver's answer and on every ops update).
- Case numbers `SC-YYYYMMDD-####`; the first admin action (assign / note / status / resolve) stamps `first_response_at` once.

## Cancellation & reliability (F14)

- **Engine** (`CancellationEngine`): every F8 cancel endpoint (passenger, driver, admin) and the no-show go through it. Stage = `before_accept` (requested /
  searching), `after_accept`, `en_route`, `arrived` / `waiting` (free waiting from the pricing rule), `no_show`; the most specific active rule (booking type,
  category, pickup zone) wins, then priority, then the newest. Inside `free_window_seconds` → no fee and no points. Passenger fault only after acceptance and
  outside the window; driver cancellations after acceptance always count (the window only removes points/fees); system cancellations (`no_drivers`,
  `payment_failed`) are recorded with nobody at fault; admins choose `atFault` and `chargeFee`.
- **Fees** (through F11): card trips capture the fee from the trip authorization (`cancellation_fee_card` journal, then the rest of the hold is released); other
  trips (or a failed capture) debit the passenger wallet (`cancellation_fee`, overdraft → `outstanding_balance` blocks the next request); compensation
  `round(fee × driver_compensation_percent / 100, 2)` is credited to the driver (`cancellation_compensation`); notifications `cancellation.fee_charged`,
  `cancellation.compensation`, `trip.cancelled`. Excusable / emergency reasons leave the event `pending_review` with no fee, points or rate impact until reviewed;
  emergency reasons also open a `medium` safety report. Approval → `waived` (a fee already charged is refunded through F11, `cancellation_fee_waived` →
  `refunded`); rejection → the fee is charged now (to the wallet) and the rule's points apply.
- **Reliability** (`ReliabilityService`, `IReliabilityService`): profiles per (user, role) refreshed after each cancellation, completion and offer answer, nightly
  (`ReliabilityRecalcJob`) and when restrictions end (`RestrictionExpiryJob`, every 5 min). Points = non-waived events + manual adjustments within 30 days
  (reset by `clear_restriction`); the first active threshold (highest `sort_order` first) met by points or by rate (with ≥ `min_trips_for_rate` accepted trips) sets
  the level; a `set_level` adjustment still in force wins. `temporarily_restricted` lasts `restriction_hours` and is re-entered only after a new at-fault
  cancellation; automatic `suspended` stays until ops act. Level ups send `reliability.warning` / `reliability.restricted` and are audited
  (`reliability.level_change`); restricted drivers online without a trip are taken offline.
- **Matching**: `ProfileDriverReliability` (the F9 `IDriverReliabilityProvider`) feeds `1 − cancellation_rate`, blocks restricted drivers and multiplies the score by
  `deprioritize_factor` (`matching_deprioritized` 0.70, `incentives_reduced` 0.60); drivers without a profile keep the F9 counters.
- Tests drive the jobs with `RunSafetyMonitorAsync()`, `RunSafetyCheckTimeoutsAsync()`, `RunRestrictionExpiryAsync()` and `WithServiceAsync<SafetyMonitor, …>`.

## Ratings, promotions, tiers and incentives (F15)

- **Ratings** (`ratings`, `rating_tags`, `rating_flags`): one rating per (trip, rater role) within `Ratings:WindowHours` of `completed_at`; tags must belong to the
  rated role. The ratee's `rating_avg` = `round(Σ w_i × stars_i / Σ w_i, 2)` over the last `WindowSize` visible ratings, newest first, `w_i = 1 − (i/N)(1 − MinWeight)`
  (5.00 without ratings), `rating_count` = all visible ratings; both are updated in the rating's transaction and when a rating is hidden / unhidden. Flags:
  `low_rating` (≤ threshold), `low_average` (one open per driver; also the daily `LowAverageFlagJob`), `abusive_comment` (the comment is hidden with
  `comment_hidden`, the stars stay visible). `RatingReminderJob` sends `rating.reminder` once per trip (`trips.rating_reminded_at`).
- **Discount engine** (`IDiscountEngine`): `base` = the F10 fare before discount and rounding (`FareCalculation.Base`); promo amount = percent (capped by
  `max_discount`) / fixed (`min(value, base)`) / free booking fee; with a favourite discount (F16) both apply only when both are stackable, otherwise the larger
  wins (a tie goes to the favourite and the promotion is released `not_stacked`); `discount = min(Σ, base − MinPayableFare)`, `total = round(base − discount, 0.5)`.
  The driver share is always computed before discounts. No discounts with `pricingMode=offer` (`422 promo_not_eligible { reason: "pricing_mode" }`).
- **Promo lifecycle** (`promotions`, `promotion_redemptions`): validation order not found/inactive → validity → total usage & budget → per user → eligibility
  (`first_trip_only`, `new_users_only`, `city`, `category`, `zone` (resolved pickup zone), `payment_method`, `booking_type`, `min_fare`, `pricing_mode`). The request
  reserves inside the trip-creation transaction (`UPDATE … usage_count = usage_count + 1 WHERE usage_count < total_usage_limit` + a `reserved` row; a card
  authorization is voided if the code ran out meanwhile). Completion recomputes on the final fare: `applied` (+ `spent_amount`) or `released
  (not_eligible_at_completion)`; the discount goes to `trips.discount_total`, `fare_breakdown.discounts[]`, the receipt `discount` line and a `trip_discount`
  journal. Cancellation (`trip_cancelled`), `no_drivers` and `payment_failed` release the reservation (`usage_count − 1`). Quotes store the undiscounted price
  (`fare_quotes.base_amount` keeps the base) and show the discounted total per category.
- **Tiers** (`driver_tier_rules`, `driver_tier_history`): `DriverTierRecalcJob` (Sunday 03:00 Riyadh, or `POST /admin/driver-tiers/recalculate`) picks the highest
  tier whose inclusive thresholds hold on completed trips (28 days), `drivers.rating_avg` and the F14 profile rates (acceptance 1 / cancellation 0 without a
  profile), writes history and `driver.tier_changed`. Benefits: `effective share = share + (100 − share) × commission_discount_percent / 100` in the offer's
  `driverNetEarnings` and at completion (`trips.tier_commission_discount_percent`, shown by `/driver/trips/{id}/earnings`); the matcher's `norm_tier` is the
  rule's `matching_norm` (`TierRuleProvider`, cached and invalidated by the admin endpoint).
- **Incentives** (`driver_incentives`, `driver_incentive_progress`, `driver_incentive_trips`): periods are Riyadh days (daily), Sunday–Saturday weeks (weekly) or
  the whole campaign (zone quest / one time), clipped to `[starts_at, ends_at)`; a completed trip counts once per progress row when the driver's city, the day
  of week and `daily_from..daily_to` window, the pickup (inside one of `zone_ids`), the category, `min_trip_fare`, `min_tier`, `min_rating`, the opt-in and
  `max_participants` match. A fully refunded trip leaves unpaid progress. `IncentivePayoutJob` (hourly) pays `round(reward × IReliabilityService.IncentiveMultiplier,
  2)` once `period_end + PayoutDelayHours` passed, or voids with `budget_exhausted`; `IncentivePeriodJob` (15 min) expires missed periods and opens the current
  period for opted-in drivers of recurring incentives. An active incentive with `notify_on_publish` sends `incentive.new` once (`published_at`).
- Tests drive the jobs with `WithServiceAsync<RatingService, …>`, `WithServiceAsync<IncentiveService, …>` and `POST /admin/driver-tiers/recalculate`.

## Favourite drivers (F16)

- **Saving** (`favorite_drivers`, UNIQUE(passenger, driver)): needs at least one `completed` trip between the two (`driverId` → their latest shared trip becomes `source_trip_id`;
  `tripId` → that trip, `404` if it is not the rider's, `422 favorite_not_eligible` if it is not completed). Order of checks: eligibility → duplicate (`409`) → `Favorites:MaxPerPassenger`.
  Removing a favourite never touches a running trip (the trip keeps `favorite_driver_id` and its pinned rule).
- **Available favourites**: the F9 eligibility of `ScoringMatcher` restricted to the rider's favourites (`MatchCriteria.OnlyDriverIds`): online, approved, free, fresh location, category (or
  higher when allowed), zone allows it, documents, F14 restrictions, cash debt and the rider's `preferFemaleDriver`, within `Favorites:AvailabilityRadiusMeters`; `etaMinutes` = the F9
  estimate (distance × 1.3 at 30 km/h, rounded up, at least 1). The `discount` is the rule that would apply to a "now" trip (`min_fare` unknown → not checked).
- **Request with `favoriteDriverId`** (`trips.favorite_driver_id`, `favorite_status`, `favorite_discount_rule_id`): `favorite_status` starts `requested`, or `unavailable` at once when the
  zone/category has `prefer_favorite_driver = false` or (for "now" bookings) the driver is not eligible at that moment. When matching starts (for scheduled trips: at `T −
  Trips:ScheduledLeadMinutes`, the hook where F17's favourite window plugs in) `MatchingService` re-checks: eligible → round 0 (`matching_attempts.mode = favorite`) with an **exclusive**
  offer (`Offer.isFavoriteRequest = exclusive = true`, `Favorites:ExclusiveOfferTimeoutSeconds`); not eligible → `unavailable` and normal round 1 in the same pass. A rejection or expiry
  sets `rejected` / `expired`, closes round 0, publishes `TripUpdated` and continues with normal round 1 (radius = the settings radius; the favourite is never offered the trip again).
  `search_timeout_seconds` counts from the end of the exclusive round. `trip.favorite_fallback` (push) is sent when the replacement driver is assigned (its text names them).
  An `unavailable` request whose favourite later takes the trip in normal matching becomes `accepted` (the discount is about who is assigned). Offers to a requested favourite carry
  `isFavoriteRequest` (also outside the exclusive round, then `exclusive = false`).
- **Discount** (`favorite_driver_discount_rules`): applies only when the assigned driver is the requested favourite. The rule is chosen when the favourite accepts — the active, currently valid
  rule matching the ride category, the pickup zone (`zone_ids`), the booking type and `min_fare` (checked on the quote's base), with the highest `priority`; **equal priorities are broken by the
  larger `discount_percent`, then the most recently created rule** — and pinned in `trips.favorite_discount_rule_id` (`estimated_fare` drops to the discounted total). At completion
  `FavoriteDiscountService` recomputes `min(base × percent / 100, max_discount_amount)` on the final base (`min_fare` re-checked) and `IDiscountEngine` combines it with a promo code:
  both apply only when the promotion `is_stackable` **and** the rule `stackable_with_promotions`, otherwise the larger wins (a tie goes to the favourite and the code is released
  `not_stacked`). No discount with `pricingMode = offer`. The result lands in `trips.discount_total`, `fare_breakdown.discounts[]` / the receipt (`source = favorite_driver`, label
  "خصم الكابتن المفضل" / "Favourite driver discount") and a `trip_discount` journal `discount_favorite_driver` → `trip_revenue` / `cash_collected`; driver earnings are unchanged.
  `POST /pricing/quote` with `favoriteDriverId` shows the discount "assuming acceptance" (`favoriteDiscountConditional`, `totalBeforeDiscount`); a promotion that loses to the favourite
  discount is reported as `promotion { valid: true, reason: "not_stacked" }`.
- **Trip payload**: `favorite { driverId, driverName, status, discountApplied }` (admin detail adds `discountRuleId`, `discountRuleName`; passengers see the driver's first name),
  `driver.isFavorite` in the rider's and admin's copies. `GET /admin/trips/{id}/matching` shows `mode` per round.
- **KPIs** (`GET /admin/favorites/stats`, no range = all time, `cityId` = the pickup's zone city): `favoriteRequests` (trips with a favourite), `accepted`, `fallback` (`unavailable` +
  `rejected` + `expired`), `favoriteBookingRate` (0..1 = favoriteRequests / all trips requested), `discountUsageCount` / `discountTotal` (from the `discount_favorite_driver` postings of completed
  trips), `topDrivers` (most saved drivers with their completed favourite trips).
- Tests: `FavoriteDriverTests`, `FavoriteMatchingTests`, `FavoriteDiscountTests`, `FavoriteStatsTests` (`FavoritesFlow` helpers), `FavoriteUnitTests`.

## Scheduled rides and airport (F17)

- **Booking**: `bookingType = scheduled` creates a trip in the new `scheduled` status (not an "active" trip: the rider can book while another trip runs; the card in use and open-trip counts include it).
  The window is measured from the booking time: `scheduledAt ≤ now + max_days_ahead × 24 h` and `≥ now + min_lead_minutes` (also enforced by `/pricing/quote` and the estimate alias);
  at most `max_open_per_passenger` open scheduled trips. Rule choice: city + category → city → category → global (active rows), else the in-code defaults. The fare is locked with a **normal**
  demand level (`lock_demand_normal`) and stored in the quote. A card trip only validates the card at booking; the authorization happens when the search starts / at the final
  confirmation and falls back to cash (`payment_fallback_cash`, `payment.failed`) when declined. Rider reminders are created at booking (offsets before `scheduled_at`, only future ones).
- **Marketplace and reservation**: `GET /driver/scheduled/marketplace` lists open trips the driver could serve (city, `marketplace_radius_km`, category / upgrade via matching settings, female
  preference, restrictions, cash debt, favourite exclusive window `min(requested + favorite_exclusive_minutes, search start)`, `marketplace_enabled`), with the pickup rounded to 3 decimals.
  `reserve` is an atomic claim on `trips.reserved_driver_id` (one winner; `409 reservation_taken`), limited by `max_reservations_per_driver` (not for admins) and a time-overlap check
  `[T, T + duration + reservation_gap_minutes]`. Timeline (`ScheduledRideWorker`): T−60 first confirmation requested (`confirm`, or `confirmed` at once when reserved later), +10 min without
  it → `confirmation_missed` (points, back to the market); T−15 final confirmation requested, +5 min → `final_confirmation_missed` (points, trip → `searching`, `scheduled.rematched` to the rider);
  the final confirmation moves the trip to `driver_assigned`; T−10 without a confirmed driver → normal search (`scheduled → searching`, favourite exclusive round first); T + grace without arrival →
  reservation `no_show`, the trip is reassigned. A driver cancelling an assigned scheduled trip also rematches it (F14 points, 0 for an excusable reason). Faulty reservations count for the reliability profile.
  Release: free until `driver_free_release_minutes_before`, then `is_late_release` and points. Reminders (`ScheduledReminderJob`) run after `send_at`; rows late by more than 10 min are `skipped`, rows of a trip that ended `cancelled`.
- **Cancellation**: stage `scheduled` — free while `minutesBefore ≥ free_cancel_minutes_before`, otherwise `late_cancel_fee_*` (fixed / percent / pricing rule; at fault: passenger, counts toward the rate);
  the reserved driver receives `late_cancel_driver_compensation_percent` of the fee; the reservation becomes `cancelled` (`trip_cancelled`). The preview's `freeUntil` is always `T − free_cancel`.
- **Airport**: a pickup / dropoff inside an active airport geofence is an airport trip (`trips.airport_id`, `airport_direction`); an airport pickup needs `airportPickupZoneId` (its coordinates replace the
  pickup, `422 airport_pickup_zone_required`), `airportTerminalCode` is checked against the terminals, `flightNumber` is stored only. Waiting policy (free minutes / per minute) for airport
  pickups = pickup zone → airport default → pricing rule, used for the PIN check, the free-waiting stage of cancellations, the no-show wait and the fare; the trip and offer payloads carry `airport { … }`.
  The `airport` ride category is hidden from non-airport quotes (`422 airport_category_not_applicable` if requested explicitly).
- **Airport queue** (`airport_queue_entries`): an online, free driver inside a `driver_waiting_area` polygon joins automatically on `PUT /driver/location` (or with `join`, which validates the position); leaving is
  `left`. `AirportQueueJob` removes stale entries (`exited_area` / `offline`) and marks drivers who took a trip `trip_assigned`. Position = waiting entries ahead in the same airport and ride category + 1;
  the estimate is position × the average gap between dispatches in the last 2 hours (needs ≥ 2). `MatchingMode.AirportQueue` offers an airport pickup to the queue head, one driver at a time in FIFO order
  (up to `Airport:QueueMaxOffers`), before the normal rounds; reject / expiry moves the driver to the back or removes him (`Airport:RejectAction`). SignalR `AirportQueueUpdated` pushes the driver's position.
- **Admin**: rules CRUD, the scheduled-trips board (`reservationStatus` is `none` without an active reservation; `atRisk` = pickup within 90 min and no confirmation), assign / release (idempotent), KPIs
  (`booked`, `completed`, `cancelled`, `cancellationRate`, `driverCommitmentRate`, `driverNoShows`, `driverNoShowRate`, all rates 0..1), airports / zones CRUD (validated polygons and codes), queue view / removal with audit.
- Migration `AddScheduledRidesAndAirport`; tests: `ScheduleWindowTests`, `ScheduledBookingTests`, `ScheduledTimelineTests`, `ScheduledMarketplaceTests`, `ScheduledCancellationTests`, `ScheduledPaymentTests`,
  `ScheduledFavoriteTests`, `ScheduledAdminTests`, `AirportTripTests`, `AirportWaitingPolicyTests`, the `Airport*Queue*` classes, `AirportAdminTests`, `SchedulingUnitTests` (`SchedulingFlow` helpers).
  The jobs are driven with `RunScheduledWorkerAsync` / `RunScheduledRemindersAsync` / `RunAirportQueueJobAsync` and `FakeClock.Set`.

## Support (F18)

- **Tickets** (`support_tickets`, `support_messages`, `support_message_attachments`): numbers `ST-YYYYMMDD-#####`; `trip_issue` / `payment_issue` / `lost_item` require a trip the user took part in (`422 validation_failed
  { tripId: "required" }`, `403` for a stranger's trip, `404` for an unknown one); `account` / `other` / `safety` need none. Default priority by type: `safety` urgent, `payment_issue` high, the rest normal; an agent
  can change it (both SLA due dates are recomputed from the policy: `created + minutes`, resolution `+ sla_paused_seconds`). A rider / driver ticket starts `open`; requester role = the user's role in the trip (else the login role).
- **Status machine**: assignment or the first public agent message moves `open → in_progress`; the agent sets `pending_user` (pauses the SLA clock), `in_progress`, `resolved` (stamps `resolved_at`; the requester is invited to rate
  with `support.status`) or `closed` (final: any user / agent message answers `409 ticket_closed`; internal notes are still allowed). A user reply to a `pending_user` / `resolved` ticket goes to `in_progress` (assigned) or `open`,
  resumes the clock (`resolution_due_at += time paused`) and clears `resolved_at`; an agent can reopen a resolved ticket with `in_progress`. `SupportAutoCloseJob` closes `resolved` tickets older than `Support:AutoCloseDays`.
  The `note` of `POST …/status` is a public agent message for `pending_user` / `resolved` (the question / the solution) and an internal note otherwise.
- **SLA**: `first_response_due_at = created + first_response_minutes`, `resolution_due_at = created + resolution_minutes + sla_paused_seconds`; `first_response_at` = the first public agent message. `slaState` (`ok` / `due_soon` /
  `breached`) is computed for active tickets only: `due_soon` = within 30 minutes of a deadline; while paused the effective resolution deadline moves with the clock (a paused ticket neither breaches nor gets closer).
  `SupportSlaMonitorJob` pushes `SupportTicketUpdated { …, slaState }` to the `admins` group when a ticket's state changes.
- **Messages**: agents appear to the requester as "فريق دعم ATA" / "ATA Support" (real names only for agents); internal notes never reach the user. A public agent message raises `unread_by_user`, sends `support.reply`
  (deep link `ata://support/tickets/{ticketId}`) and pushes `SupportTicketUpdated` to the user; `support.status` goes out for `pending_user`, `resolved`, `closed` and dispute outcomes; a user message pushes `SupportTicketUpdated` to
  the admins. System lines (lost item updates, dispute outcomes) never change the status or the unread counter.
- **Attachments**: `POST /support/attachments` stores a file under `support/{userId}/…` (`stored_files.owner_user_id` = the uploader); a message takes up to `Support:MaxAttachmentsPerMessage` of the sender's own support
  uploads, each attached once. `GET /files/{id}`: owner, any admin, or the requester of the ticket for files on public messages.
- **Linking with F12**: a `safety` ticket also creates a `safety_cases` row (`type = safety_report`, `source = support`, priority `high`, reporter / subject from the trip) with `safety_cases.support_ticket_id` and
  `support_tickets.safety_case_id` set; a `lost_item` ticket of a rider creates the `lost_item_reports` row (driver notified) linked both ways (the same 7-day window as the F12 endpoint: `422 lost_item_window_closed`);
  the other direction: `POST /passenger/trips/{id}/lost-items` opens the ticket. Safety reports / SOS of F12 do not open tickets.
- **Fare disputes** (`fare_disputes`, UNIQUE trip and ticket): `payment_issue` + `dispute { reason, requestedRefundAmount? }` by the trip's passenger on a `completed` trip (charged = final fare) or a trip cancelled with a fee charged
  (charged = the fee), within `Support:DisputeWindowDays` of completion / cancellation (`422 dispute_window_closed`), one per trip (`409 dispute_exists`), otherwise `422 validation_failed { tripId: "not_disputable" }`.
  `POST /admin/support/disputes/{id}/resolve` (`support.disputes`): `refund_full` refunds the charged amount, `refund_partial` the `amount` (0 < amount ≤ charged), `no_refund` nothing; the refund is created first through the F11
  service (`reason_code = fare_dispute`, `dispute_id`; card trips → the original card, otherwise the passenger wallet; below `Payments:RefundAutoApproveLimit` executed at once, else `pending_approval` until a second admin
  approves it at `POST /admin/refunds/{id}/approve`), then the dispute becomes `approved` / `partially_approved` / `rejected`, a system line is added to the ticket and `support.status` is sent. The first agent action on the ticket
  moves an `open` dispute to `under_review`.
- **Help center** (`help_categories`, `help_articles`): public endpoints need no authentication and show active categories with published articles only; the audience filter applies to the article and its category. MySQL uses the
  `FULLTEXT(title_ar, title_en, body_ar, body_en)` index (`MATCH … AGAINST` in boolean mode, every word as `+word*`, ordered by relevance) and falls back to `LIKE` when it finds nothing (words shorter than
  `innodb_ft_min_token_size`); SQLite (tests) always uses `LIKE` (every word in a title or body of either language, title hits first). The result is shown in the request language. Views and votes are plain counters.
- **KPIs** (`GET /admin/support/stats`): created / resolved / closed in the range, `openNow`, mean and median resolution time (`resolved_at − created_at − sla_paused_seconds`) and first-response time, SLA compliance
  (share of tickets resolved before `resolution_due_at`), CSAT mean and count, per-type counts; `GET /admin/support/summary` has the live queue counters (`open`, `unassigned`, `pendingUser`, `breachingFirstResponse`,
  `breachingResolution`, all-time averages and `csatAvg`).
- Migration `AddSupport`; tests: `HelpCenterTests`, `SupportTicketTests`, `SupportConversationTests`, `SupportWorkflowTests`, `SupportQueueTests`, `SupportAdminTests`, `SupportStatsTests`, `FareDisputeTests`,
  `SupportUnitTests` (`SupportFlow` helpers, `SupportFixture` with a recording `ISupportNotifier` and `Payments:RefundAutoApproveLimit = 20`). The jobs are driven with `RunSupportAutoCloseAsync` / `RunSupportSlaMonitorAsync`.

## Corporate accounts (F19)

- **Model** (migration `AddCorporate`): `corporate_accounts`, `corporate_users` + `corporate_invitations` (membership: role `corporate_admin` / `employee`, status `invited` / `active` / `disabled`; each invitation has its own expiry and token hash), `corporate_policies`,
  `corporate_cost_centers`, `corporate_invoices` + `corporate_invoice_lines`, `corporate_adjustments`, `corporate_api_keys`; `trips` gain `corporate_account_id`, `corporate_user_id`, `booked_by_user_id`, `is_guest`, `guest_name`,
  `guest_phone`, `trip_purpose`, `cost_center_id`; `refresh_tokens.session_kind` (`app` / `admin` / `corporate`). `PaymentMethodKind.Corporate` (`payment.method = corporate`).
- **Portal auth**: `POST /auth/otp/request` and `/auth/otp/verify` with `role: "corporate_admin"`. The token carries only the role `corporate_admin` plus the claim `corp` (the account id); every `/corporate/*` call re-checks that
  the user is still an active admin of a non-closed company (`403 corporate_not_member` / `corporate_account_inactive`). A first sign-in accepts the admin invitation implicitly; a pending company can be entered so its first
  admin can be set up, but it cannot book until an admin activates it. Refresh keeps the session kind.
- **Company admin API** (`/corporate/*`, `Policies.CorporateAdmin`): `account` (GET/PUT), `dashboard`, `employees` (list, invite, `import` CSV, get, update, `disable`, `enable`, `resend-invitation`, DELETE revokes / removes),
  `policies` (CRUD + `default`), `cost-centers` (CRUD), `bookings` (`quote`, create for an employee or a guest, list, get, `cancel/preview`, `cancel`), `invoices` (list, get, `pdf`, `export`), `reports/summary`, `reports/trips`,
  `reports/trips/export` (CSV), `api-keys` (behind `Corporate:ApiKeysEnabled`).
- **Employee / rider API** (`/passenger/corporate*`, rider token): `GET /passenger/corporate` (`null` or `{ membership, policy, budget, costCenters }`), `GET /passenger/corporate/invitations`,
  `POST …/invitations/{id}/accept|decline`. `POST /passenger/trips/estimate` and `/passenger/trips` accept `paymentMethod: "corporate"`, `tripPurpose`, `costCenterId`; the quote carries
  `corporate { allowed, violations[{ rule, limit?, allowed? }], remainingBudget }`; a blocked request answers `422 corporate_policy_violation` (`details.violations`), an exhausted budget
  `422 corporate_budget_exceeded` (`details.remaining`). `Trip` and `Receipt` carry `corporate { companyName, purpose, costCenter }` (the driver never sees it; the admin trip view adds the account and the
  `corporate_policy_exceeded` event). Promo codes and the favourite-driver discount never apply to corporate trips; "offer your price" does.
- **Platform admin API** (`/admin/corporate/*`, permission `corporate.manage`): `accounts` (list, create, get, update, `activate`, `suspend`, `close`, `admins`), mirrors under `accounts/{id}/` for `employees`
  (+ `disable`, `enable`, `resend-invitation`, DELETE), `policies`, `cost-centers`, `trips` (+ `export`), `adjustments`, `invoices/generate`; `invoices` (filters `status`, `from` / `to` on the issue date, `accountId`),
  `invoices/{id}` (+ `issue`, `mark-paid`, `void`, `pdf`), `receivables` (array). Audit rows use `entityType = corporate_account`, `entityId` = the account id.
- **Policy rules** (`CorporatePolicyEvaluator`, pure): `guest_booking`, `category`, `day`, `time_window` (Riyadh time), `zone`, `max_fare`, `scheduled`, `purpose_required`, `cost_center_required`; the monthly budget is
  per employee (an employee override wins over the policy) and counts the Riyadh calendar month. The company credit check counts unbilled + unpaid + overdue invoices + in-flight trips.
- **Money** (F11 ledger, doc 08 §F11.3): a completed corporate trip posts `corporate_receivable:{account} ← trip_revenue` (`trip_corporate_charge`) and credits the driver as usual; cancellation fees post the same way
  (a waived fee is reversed on the company account); invoice payment posts `corporate_invoice_payment` and manual adjustments post journals, so the receivable nets to zero when an invoice is paid in full. Refunds on corporate
  trips are refused. VAT 15 % is included in fares: per line `excl = round(incl / 1.15, 2, AwayFromZero)`, `vat = incl − excl`.
- **Invoices**: one non-void invoice per account and month (`period_active` makes the pair unique except for void invoices); the monthly job generates drafts for the previous Riyadh month, an admin issues them
  (`corporate.invoice_issued` notification + e-mail), marks them paid (full or partial) or voids them; `CorporateInvoiceOverdueJob` marks issued invoices past their due date `overdue`, and `CorporateInvitationExpiryJob` expires old invitations.
- **Invoice PDF**: rendered by QuestPDF (2026.9.1) behind `IInvoicePdfRenderer` (swap point: register another implementation in `Program.cs`). QuestPDF is used under the **Community licence**, which is free for companies below the
  revenue threshold of the licence; above it a commercial licence is required, or replace the renderer. The layout is bilingual / RTL, uses the embedded **IBM Plex Sans Arabic** (SIL OFL, `Modules/Corporate/Fonts`) so nothing is
  fetched at run time, and includes a QR of the ZATCA TLV payload (seller name, VAT number, ISO timestamp, total, VAT; base64; matrix from QRCoder drawn as SVG).
- **Tests**: `CorporateAccountTests`, `CorporateApiKeyTests`, `CorporateMembershipTests`, `CorporatePolicyTests`, `CorporateTripTests`, `CorporateInvoiceTests`, `CorporateInvoiceJobTests`, `CorporateAutoIssueTests`,
  `CorporateReportTests`, `CorporateUnitTests` (helpers in `Infrastructure/CorporateFlow.cs`: `CorporateFixture`, `RecordingEmailSender`; the jobs are driven with `RunCorporateInvoiceJobAsync`,
  `RunCorporateOverdueJobAsync`, `RunCorporateInvitationExpiryAsync` and the `FakeClock`).

## Roles, permissions, MFA and reports (F20)

- **Model** (migration `AddRbacMfaReports`): `roles`, `permissions` (read-only copy of the code catalogue), `role_permissions`, `admin_account_roles`, `admin_recovery_codes`, `report_snapshots`
  (UNIQUE(snapshot_date, scope_key, metric_code)); `admin_accounts` gain `failed_login_count`, `locked_until`, `must_change_password`, `password_changed_at`, `mfa_enrolled_at`, `mfa_last_step`,
  `mfa_failed_count` (`mfa_secret` widened to 512 for the Data Protection payload; the old `permissions` JSON is kept for compatibility only and cleared once the sync grants the role);
  `refresh_tokens` gain `user_agent`, `last_used_at`, `absolute_expires_at` (`session_kind` came with F19).
- **Authorization** (doc 12 §F20.3): every `/admin/*` route keeps `Policies.Admin` and declares `RequirePermission("<code>")` (group or endpoint) — or `AllowAnyAdmin()` for `/admin/me*` and reading
  ride categories. The metadata is enforced by `AdminAccessMiddleware` right after authorization, before parameter binding: `403 forbidden { permission }` (`{ permissions }` for an any-of rule).
  The route-table test fails when a new admin route has neither. The admin JWT carries `roles` (incl. `admin`), `perm` = union of the role permissions (`["*"]` for `super_admin`), `sid` (the
  session) and `pwdc` while a temporary password must be changed. Permissions are re-derived from the roles at every refresh (app / corporate sessions never get `perm`).
- **Rules**: a role change, disabling, a password reset or an MFA reset revokes every refresh token of the user (access tokens end within `Admin:AccessTokenMinutes`); nobody disables themselves
  (`409 conflict { reason: "cannot_disable_self" }`); the last active `super_admin` keeps the role and stays enabled (`409 { reason: "last_super_admin" }`); only a caller holding `*` grants or removes
  `super_admin` (`403 { permission: "*" }`); system roles keep their code and cannot be deleted, `super_admin` always means `*` (`409 { reason: "system_role" }`); a role still assigned cannot be deleted
  (`409 { reason: "role_in_use", userCount }`).
- **Passwords**: ≥ 12 characters with upper, lower, digit and symbol (`422 password_policy_violation { rules: ["min_length:12", "uppercase", "lowercase", "digit", "symbol"] }`, plus
  `different_from_current`). A created / reset admin gets a temporary password (shown once) and `must_change_password`: until `POST /admin/me/password` every other admin call answers
  `403 password_change_required`. A wrong current password answers `400 invalid_credentials`; a successful change keeps the current session and revokes the others.
- **MFA** (RFC 6238, SHA-1, 6 digits, 30 s, ±1 step): 20-byte Base32 secret encrypted with Data Protection; a step at or before `mfa_last_step` is refused (no replay); 10 recovery codes
  `xxxx-xxxx` hashed with the PBKDF2 hasher, single use, replaced by `POST /admin/me/mfa/recovery-codes { code }`. The `mfaToken` (Data Protection, 5 min, stage `verify` / `enroll`) links the login steps.
  `otpauth://totp/ATA%20Admin:{username}?secret=…&issuer=ATA%20Admin&digits=6&period=30`. Audit: `admin.login`, `admin.login_failed`, `admin.mfa_enrolled`, `admin.mfa_failed`, `admin.password_changed`.
- **Sessions**: login creates an `admin` refresh token with `absolute_expires_at = login + 12 h`; each refresh rotates it, keeps the session start / absolute end / user agent and records `last_used_at`;
  refreshing after 30 idle minutes or past the absolute end → `401` (the token is revoked). `AdminSessionCleanupJob` revokes such sessions hourly.

| Permission | Module | Covers |
|---|---|---|
| `dashboard.view` | dashboard | `/admin/dashboard/summary` |
| `drivers.view` / `drivers.review` | drivers | driver lists and details / review, approve, reject, suspend, reinstate, `/admin/documents/{id}/verify` |
| `passengers.view` | passengers | `/admin/passengers` |
| `users.suspend` | users | `/admin/users/{id}/suspend\|reinstate` |
| `catalog.manage` | catalog | ride-category writes (reads open to every admin) |
| `trips.view` / `trips.cancel` / `live.view` | trips | trips, details, matching, cancellation log / admin cancel / live map |
| `pricing.view` / `pricing.edit` | pricing | zones, pricing rules, demand, `demand/current`, matching settings (read) / writes and `pricing/simulate` |
| `matching.edit` | matching | matching-settings writes |
| `payments.view`, `payments.refund`, `payments.refund_approve`, `payouts.approve`, `settlements.manage`, `wallets.adjust` | payments | payments, refunds, payouts, wallets, ledger / refunds / refund decisions / payouts & batches / settlements / adjustments & freezing |
| `notifications.view`, `notifications.manage`, `notifications.sms_broadcast` | notifications | catalogue, templates, deliveries / template edits, campaigns, retries / SMS campaigns |
| `safety.manage` | safety | safety cases, alerts, lost items, duty |
| `cancellation.manage`, `cancellation.review`, `reliability.manage` | cancellation | reasons, rules, thresholds / excuse reviews / reliability profiles |
| `ratings.manage`, `promotions.manage`, `incentives.manage`, `favorites.manage` | … | ratings & flags, promotions, incentives & tiers, favourite discount rules |
| `scheduling.manage`, `airport.manage` | … | scheduling rules & scheduled trips, airports & queue |
| `support.view`, `support.manage`, `support.disputes`, `help.manage` | support | tickets & disputes (read) / replies, assignment, canned responses, SLA / dispute resolution / help center |
| `corporate.manage` | corporate | corporate accounts and invoices |
| `reports.view` / `reports.export` | reports | KPIs, statistics (`/admin/matching/stats`, `/admin/cancellations/stats`) / CSV exports and snapshot rebuilds |
| `admin.users.manage` / `admin.roles.manage` / `audit.view` | admin | admin users / roles & permissions / audit log |

| System role | Permissions (defaults) |
|---|---|
| `super_admin` | `*` (immutable) |
| `operations_manager` | dashboard, drivers view/review, passengers, users.suspend, trips view/cancel, live, pricing.view, safety, cancellation manage/review, reliability, ratings, scheduling, airport, support.view, notifications.view, reports.view |
| `finance` | dashboard, trips.view, payments.*, payouts.approve, settlements.manage, wallets.adjust, corporate.manage, reports view/export |
| `support_agent` | dashboard, trips.view, passengers.view, drivers.view, support view/manage/disputes, help.manage, cancellation.review, notifications.view |
| `analyst` | dashboard, trips.view, pricing.view, reports view/export |

- **KPIs** (doc 12 §F20.6, `Modules/Reporting/KpiDefinitions.cs`): 32 metrics (`completed_trips` … `scheduled_ride_cancellation_rate`, the v1.1 ones with `group = "v1.1"`), computed by `KpiCalculator` from the
  F8–F19 tables per Riyadh day and scope (`all`, `city:{id}`, `zone:{id}`, `cat:{id}`, `city:{id}|cat:{id}`, `zone:{id}|cat:{id}`; the zone is the trip's `fare_quotes.pickup_zone_id`, else the resolved
  pickup zone). `sum` metrics add up, `ratio` / `avg` keep numerator and denominator (Σ ÷ Σ across days), `distinct` metrics (`active_riders`, `active_drivers`, `repeat_rate`, `trips_per_active_rider`,
  `repeat_cancellation_rate`) are recomputed for the requested range. Percent metrics are shares 0..1 (with numerator / denominator); `online_hours`, `driver_earnings_per_online_hour`,
  `incentives_paid` and `support_resolution_time` have no place / category dimension (null under a filter).
- **Snapshots**: `ReportSnapshotJob` (01:30 Riyadh) rewrites yesterday and the 3 days before it (rows of a day are replaced in one transaction: no duplicates); `POST /admin/reports/snapshots/rebuild { from, to }`
  → `202 { from, to, days, rows }` does the same for any past range (audited `report_snapshots.rebuild`). Days without a snapshot (today, or not yet computed) are computed live by the report APIs.
- **Reports API**: `kpis?from=&to=&cityId=&zoneId=&rideCategoryId=&compare=previous_period&metrics=` → `{ from, to, filters, compare, previousFrom, previousTo, metrics[{ code, name, unit, aggregation, group,
  value, previousValue, changePercent, numerator, denominator }] }`; `kpis/{code}/series?granularity=day|week|month` (weeks start on Sunday) → `{ code, name, unit, granularity, points[{ periodStart, value,
  numerator, denominator }] }`; `breakdown?metric=&groupBy=city|zone|category` (+ the same filters) → `{ metric, rows[{ key, label, value, numerator, denominator }] }`; `export?dataset=kpis|trips|payments|payouts|
  cancellations|ratings|support_tickets|drivers|incentives&format=csv` streams UTF-8 CSV with BOM and the doc's columns (users as UUIDs, never names or phones).
- **Tests**: `RbacPermissionTests` (sync, route table, 403 of every admin endpoint), `RbacAdminUserTests`, `AdminMfaTests` (RFC 6238 vectors, fake-clock TOTP), `AdminSessionTests`, `ReportKpiTests`
  (hand-built data in `Infrastructure/ReportData.cs`), `ReportSnapshotJobTests`. Limited admins in tests are created with `TestAdmins` (a role per test user; `*` → `super_admin`).

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
  balanced `ledger_entries`. `Wallet.Post(..., allowOverdraft)` may go negative only for `cash_collection`, `cancellation_fee`, `trip_payment`
  and `adjustment`; a frozen wallet rejects top-ups and payouts but keeps receiving system postings.
- Tests run on SQLite in-memory, so LINQ stays translatable on both providers (no MySQL-only functions); the matcher pre-filters
  with a bounding box and sorts by Haversine distance in memory. The background loops are disabled in the test host and the
  tests drive one matching pass with `AtaWebApplicationFactory.RunMatcherAsync()` and one demand pass with `RunDemandAsync()`.
- Zones are matched in memory (ray casting) after a bounding-box check; the demand loop resolves trips and drivers to zones the same way.
- `PinCodeProtected` uses ASP.NET Data Protection; in production persist the key ring (e.g. `Storage:Root`) so PINs of in-flight
  trips survive restarts.
