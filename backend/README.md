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

## API summary (`/api/v1`, JSON camelCase, `Accept-Language: ar|en`, errors as `{ "error": { code, message, details } }`)

| Area | Endpoints |
|---|---|
| Auth | `POST /auth/otp/request`, `POST /auth/otp/verify`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/admin/login` |
| Me | `GET/PATCH/DELETE /me`, `GET/PUT /me/notification-preferences`, `PUT /me/devices` |
| Catalog | `GET /catalog/ride-categories`, `GET /catalog/document-types`, `GET /catalog/cities` |
| Passenger | `GET /passenger/saved-places`, `PUT/DELETE /passenger/saved-places/{label}`, `PATCH /passenger/preferences` |
| Pricing | `POST /pricing/quote` (passenger; stores `fare_quotes`, 5-minute expiry), `GET /pricing/demand?lat=&lng=` (any authenticated user: current zone + demand level) |
| Passenger trips | `POST /passenger/trips/estimate` (alias of `POST /pricing/quote`), `POST /passenger/trips` (optional `quoteId`; 409 `trip_active_exists`, 422 `quote_expired`, 422 `offer_out_of_range` with `details.offerMin/offerMax`), `GET /passenger/trips/active` (`Trip` or `null`), `GET /passenger/trips/{id}` (PIN once a driver is assigned), `POST /passenger/trips/{id}/cancel`, `GET /passenger/trips?status=all|active|completed|cancelled` |
| Wallet | `GET /wallet` (cards in `paymentMethods`; driver wallets add `cashDebt`, `cashDebtLimit`), `GET /wallet/transactions`, `POST /wallet/topups?kind=` (`Idempotency-Key` ≤ 60 chars; `method: sandbox\|card\|apple_pay` + `paymentMethodId`/`applePayToken`; `201` captured or `202 { paymentId, status: "initiated", action }`) |
| Payments | `GET /payments/config`, `GET /payments/{id}` (owner), `POST /payments/webhooks/{provider}` (anonymous, signature checked: `401 webhook_signature_invalid`, dedupe on `(provider, event_id)`), `GET /payments/return/{provider}?id=` (302), `GET/POST /payments/sandbox/challenge/{id}` (sandbox only; payment or pending card) |
| Passenger payments | `GET/POST /passenger/payment-methods` (`201` active / `202 { paymentMethod, action }` for 3-D Secure; `409 conflict` same card; `422 payment_failed`), `POST /passenger/payment-methods/{id}/default`, `DELETE /passenger/payment-methods/{id}` (`409 payment_method_in_use`), `GET /passenger/trips/{id}/receipt`. `POST /passenger/trips` accepts `paymentMethodId` (`422 payment_failed`, `422 payment_method_expired`, `422 outstanding_balance`); `Trip` adds `payment`, `collectCashAmount` (driver only), `discountTotal`; `Offer` adds `paymentMethod` |
| Driver money | `GET /driver/earnings/statement?from=&to=` (≤ 92 local days), `GET /driver/trips/{id}/earnings`, `GET /driver/payouts/summary`, `POST /driver/payouts` (`Idempotency-Key`; `payout_below_minimum`, `insufficient_balance`, `iban_missing`, `payout_pending_exists`, `account_suspended`), `GET /driver/payouts`, `POST /driver/payouts/{id}/cancel`, `GET /driver/settlements`, `GET /driver/settlements/{id}`. Going online with a cash debt above the limit → `403 cash_debt_limit_exceeded { cashDebt, limit }` |
| Driver | `GET /driver/application`, `PUT /driver/application/profile`, `PUT /driver/application/vehicle`, `POST /driver/documents` (multipart), `DELETE /driver/documents/{id}`, `POST /driver/application/submit`, `GET/PUT /driver/status`, `GET /driver/earnings/summary` (completed trips + online hours) |
| Driver trips | `PUT /driver/location` (204; broadcasts `DriverLocation` to the passenger during a trip), `GET /driver/offers/active` (`Offer` or `null`; includes `round` and `passengerOffered`), `POST /driver/offers/{id}/accept` (409 `offer_expired`), `POST /driver/offers/{id}/reject`, `GET /driver/trips/active`, `POST /driver/trips/{id}/en-route`, `/arrived`, `/verify-pin` (400 `pin_invalid` with `attemptsLeft`, 429 `pin_locked`), `/start`, `/complete`, `/cancel`, `GET /driver/trips?status=` |
| Notifications | `GET /notifications?category=` (`type` = event code, legacy types normalised; `category`; `data.deepLink`), `GET /notifications/unread-count`, `POST /notifications/read`, `POST /notifications/{id}/opened` (204) |
| Files | `GET /files/{id}` (owner or admin, inline) |
| Admin | `GET /admin/dashboard/summary`, `GET /admin/drivers`, `GET /admin/drivers/{id}`, `POST /admin/drivers/{id}/{review,approve,reject,suspend,reinstate}`, `POST /admin/documents/{id}/verify`, `GET /admin/passengers`, `POST /admin/users/{userId}/{suspend,reinstate}`, `GET/POST /admin/ride-categories`, `PUT/DELETE /admin/ride-categories/{id}` (`fallbackPricing` = flat-pricing columns, `pricingSource`), `GET /admin/audit-logs` |
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
| Admin cancellation | `GET/POST /admin/cancellation-reasons`, `PUT/DELETE /admin/cancellation-reasons/{id}` (delete deactivates a used reason), `GET/POST /admin/cancellation-rules`, `PUT/DELETE /admin/cancellation-rules/{id}`, `POST /admin/cancellation-rules/simulate`, `GET /admin/reliability-thresholds?role=`, `PUT /admin/reliability-thresholds/{id}` (`cancellation.manage`); `GET /admin/cancellations?actor=&stage=&atFault=&feeStatus=&excuseStatus=&from=&to=&search=` (`trips.view`); `GET /admin/cancellations/excuses?status=` (+ `ageHours`, `slaBreached`, `pendingPenaltyPoints`), `POST /admin/cancellations/{eventId}/review { decision, note }` (`cancellation.review`); `GET /admin/reliability-profiles?role=&level=&search=`, `GET /admin/reliability-profiles/{userId}?role=`, `POST /admin/reliability-profiles/{userId}/adjust` (`reliability.manage`); `GET /admin/cancellations/stats?from=&to=&cityId=&zoneId=&rideCategoryId=` (`reports.view`); `POST /admin/trips/{id}/cancel { reason, atFault?, chargeFee? }` (`trips.cancel`). Audited: `cancellation_reason.*`, `cancellation_rule.*`, `reliability_threshold.update`, `cancellation.review`, `reliability.adjust`, `reliability.level_change` (system) |
| Realtime | SignalR hub `/hubs/trips` (JWT via `?access_token=`): `TripUpdated`, `DriverLocation` (passenger), `OfferReceived`, `OfferExpired`, `TripUpdated` (driver), `LiveSnapshot` every 5 s + `TripUpdated` + `DemandChanged` + `PayoutRequested` (`admins` group), `PaymentUpdated`, `NotificationCreated` (user); F12: `TripMessage`, `TripMessagesRead` (trip parties), `SafetyCheck` (passenger), `SafetyCaseOpened`, `SafetyCaseUpdated`, `SafetyAlertRaised` (`admins`) |
| System | `GET /health` (MySQL check), `GET /openapi/v1.json`, `GET /docs` (Development) |

Roles: `passenger`, `driver`, `admin`, `operations` (JWT `roles` claim). F11/F13 admin endpoints also check the JWT `perm` claim
(`payments.view`, `payments.refund`, `payments.refund_approve`, `payouts.approve`, `settlements.manage`, `wallets.adjust`, `notifications.view`,
`notifications.manage`, `notifications.sms_broadcast`, `safety.manage`, and since F12/F14 `support.manage`, `trips.view`, `trips.cancel`, `cancellation.manage`,
`cancellation.review`, `reliability.manage`, `reports.view`; `*` grants all — the seeded admin has `*`). Admin actions are recorded in `audit_logs`
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
| `discount_promotion`, `discount_favorite_driver`, `incentives`, `corporate_receivable:{id}` | Reserved for F15/F16/F19 |

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
- **Refunds**: ≤ refundable (`422 refund_exceeds_amount { refundable }`); below `RefundAutoApproveLimit` approved and executed at once,
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
  `driver_contacted → returned → closed`. `support_ticket_id` stays `null` until F18.
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
