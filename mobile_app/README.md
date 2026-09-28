# ATA mobile app (Flutter)

Rider + driver app for the ATA ride-hailing platform. Step 1 covers sign-in
(language, role, phone, OTP), rider onboarding, the rider home (painted map +
request sheet), rides, wallet, safety, account, driver onboarding status and
the approved-driver dashboard. F8 adds the trip lifecycle: real ride
requests, dispatch offers for drivers, the passenger/driver trip screens,
live updates over SignalR and driver location streaming. Google Maps arrives
with the Maps integration (the map is still the painted canvas).

## Run

```bash
flutter pub get
flutter gen-l10n            # also runs automatically on build
flutter run \
  --dart-define=API_BASE_URL=http://10.0.2.2:5000/api/v1 \
  --dart-define=DRIVER_PORTAL_URL=http://10.0.2.2:5173/driver
```

| `--dart-define`     | Default                          | Purpose                                                   |
|---------------------|----------------------------------|-----------------------------------------------------------|
| `API_BASE_URL`      | `http://10.0.2.2:5000/api/v1`    | Backend base URL (`10.0.2.2` = host machine on Android)   |
| `DRIVER_PORTAL_URL` | `http://10.0.2.2:5173/driver`    | Website page opened by "الانتقال لموقع رفع المستندات"     |
| `SHOW_DEV_OTP`      | `true`                           | Show the `devCode` returned by the API on the OTP screen  |
| `HUB_URL`           | derived: `<API origin>/hubs/trips` | SignalR trips hub (JWT sent as `access_token` query param) |
| `SIMULATE_LOCATION` | `false`                          | Emit a fake Riyadh position instead of the GPS (emulators / tests) |

Quality gates:

```bash
flutter analyze   # must report no issues
flutter test      # unit + cubit + widget + smoke tests
```

### Dev OTP behaviour

When the backend runs with `Otp:DevMode=true` it does not send an SMS and
returns the code as `devCode` in `POST /auth/otp/request`. The OTP screen
shows it as a small badge ("رمز التطوير: 1234") while `SHOW_DEV_OTP` is
true. Build with `--dart-define=SHOW_DEV_OTP=false` for release.

### Trip lifecycle (F8)

- Rider: the home sheet estimates (`POST /passenger/trips/estimate`) and
  creates the trip (`POST /passenger/trips`, `pricingMode: fixed`, or `offer`
  when a price is suggested). The app-wide `ActiveTripCubit` watches
  `GET /passenger/trips/active` merged with the hub `TripUpdated` /
  `DriverLocation` events; while a trip exists the router pins the rider to
  `/trip` (searching → driver card with ETA, PIN, call/share → waiting timer →
  in trip → receipt / cancelled / no drivers). Cancelling asks for a reason.
- Driver: going online starts `DriverOfferCubit` (`GET /driver/offers/active`
  + `OfferReceived` / `OfferExpired`, 20 s countdown, `/driver/offer`) and
  `LocationStreamCubit` (`geolocator`, 10 m distance filter, `PUT
  /driver/location` every 3–5 s). `DriverTripCubit` drives `/driver/trip`:
  en-route → arrived → PIN entry (`verify-pin`) → start → complete, or cancel
  with a reason. The driver feed restores an active trip on app start.
- Realtime: `TripRealtimeDataSource` (`signalr_netcore`) connects to
  `HUB_URL?access_token=<jwt>` with automatic reconnect; `TripWatcher` merges
  hub events with polling (every 5 s, backing off while the hub is
  connected) into one stream, so the UI works without the socket.
- Platform: `ACCESS_FINE_LOCATION` / `ACCESS_COARSE_LOCATION` and the `tel:`
  query are declared in `AndroidManifest.xml`;
  `NSLocationWhenInUseUsageDescription` and `LSApplicationQueriesSchemes`
  (`tel`) in `Info.plist`.

### Pricing, offers and demand (F10 / F9)

- `pricing` feature: `QuoteCubit` prices the route through
  `POST /pricing/quote` whenever the stops or booking time change (600 ms
  debounce, injectable for tests), keeps the last quote while re-pricing and
  flags it `expired` at `expiresAt` (injectable clock) so a stale `quoteId`
  is never sent; `refresh()` re-prices at once. `DemandCubit` polls
  `GET /pricing/demand?lat&lng` for the pickup every 60 s (injectable ticker)
  and drives the coloured badge under the sheet title ("الطلب مرتفع الآن
  ×1.5": normal → brand, moderate → ink, high → `AtaColors.warning`,
  very high → danger).
- Home sheet: category tiles show the quoted total and ETA (catalog estimate
  while the first quote loads, with a plain "جارٍ حساب السعر…" label);
  "تفاصيل السعر" opens `FareBreakdownSheet` (base, distance, time, min-fare,
  time / demand multipliers, booking and service fees, discount, total —
  driver net earnings are never shown to passengers). `HomeSync` holds the
  cubit-to-cubit listeners: route changes → re-quote, usable quote →
  `HomeCubit.applyQuote` (prices, offer bounds, `quoteId`), `422
  offer_out_of_range` → offer clamped to the returned bounds, `422
  quote_expired` → quote refreshed and the rider asked to confirm again.
- Offer your price: a `Slider` bounded by the quote's `offerMin..offerMax`
  (70 %..130 % of the catalog estimate until a quote exists) with ±1 SAR
  buttons and the min / max captions; the request sends `quoteId` and
  `offeredPrice` (`pricingMode: offer`). With a `quoteId` the legacy
  `/passenger/trips/estimate` call is skipped.
- Driver offer page: "اقتراح من الراكب" chip when the offer carries
  `passengerOffered` (or `pricingMode: offer`) and a "الجولة n" chip for
  matching rounds above 1; net earnings stay the prominent figure.

### Fonts and assets

IBM Plex Sans Arabic (400/500/600/700, OFL) is bundled in `assets/fonts/` and
declared in `pubspec.yaml`, so no network access is needed at runtime. The
logo lives in `assets/images/logo.png` (copy of `res/logo1.png`).

## Architecture

Feature-first Clean Architecture with **flutter_bloc / Cubit** only.

```
lib/
  main.dart                 bootstrap: BlocObserver, DI, runApp
  app/                      AtaApp (MaterialApp.router), bootstrap, splash,
                            router/ (routes, redirect rules, GoRouter), shell/ (rider header, menu, bottom nav)
  core/                     di/ (get_it, manual registration), env/, errors/ (Failure, AppException),
                            network/ (dio ApiClient + interceptors), storage/ (secure tokens, prefs),
                            usecases/ (UseCase base), utils/, localization/ (failure text, l10n ext), widgets/
  design/                   tokens/ (colors, shadows, radii, spacing, text), theme/,
                            painting/ (SVG path parser, map painter),
                            widgets/ (AtaButton, AtaCard, DarkCard, StatCard, SettingRow, AtaToggle,
                                      Keypad, OtpBoxes, AtaIcon, BottomNav, ScreenTitle, MapCanvas, ...)
  features/<feature>/
    data/                   models (fromJson/toJson), datasources (remote/local), repositories (impl)
    domain/                 entities, repository interfaces, usecases (one class per use case, `call()`)
    presentation/           cubit/ (<name>_cubit.dart + <name>_state.dart), pages/, widgets/
  l10n/                     app_ar.arb (template), app_en.arb, generated/
```

Features: `auth`, `driver_onboarding`, `passenger_home`, `rides`, `wallet`,
`safety`, `account`, `notifications`, `driver_dashboard`, `catalog`, `trip`,
`pricing`.

### Rules

- **No `setState` anywhere.** Every piece of screen state (keypad digits,
  OTP code, countdown, toggles, tabs, stops, selected category, top-up
  amount, menus) lives in a Cubit with an immutable `Equatable` state and
  `copyWith`. Pages are `StatelessWidget`s using `BlocProvider`,
  `BlocBuilder`, `BlocSelector` and `BlocListener`.
- Cubits call **use cases only**; use cases call repository interfaces;
  implementations live in `data/`. Results are `Either<Failure, T>` (fpdart).
- Cubit-to-cubit communication goes through listeners (for example the OTP
  page pushes the verified session into `SessionCubit`) or shared
  repositories, never through widget state.
- Dependency injection: `core/di/injector.dart` registers infrastructure,
  `data_module.dart` registers repositories, `use_case_module.dart` registers
  use cases. Tests register fake repositories and reuse the real use cases.
- The router (`go_router`) redirects from `SessionCubit` state: unknown →
  splash, signed-out → `/auth/*`, new rider → terms, driver not approved →
  `/driver/pending`, approved driver → `/driver`, rider → `/home`. The trip
  cubits add: rider with an active trip → `/trip`, driver with an active trip
  → `/driver/trip`, driver with a pending offer → `/driver/offer`.
- App-wide trip cubits live in `TripCubits` (bootstrap): it binds them to the
  session (passenger feed for riders, driver feed for approved drivers, all
  stopped on sign-out) and feeds the router's `refreshListenable`.
- Lints: `flutter_lints` plus `prefer_const_constructors`,
  `always_use_package_imports`, `avoid_print`, trailing commas, single quotes.
- Files stay small (< ~250 lines); constants replace magic numbers
  (`AtaSpacing`, `AtaSizes`, `AtaRadii`).

### Cubits

| Cubit                    | Feature            | Owns                                              |
|--------------------------|--------------------|---------------------------------------------------|
| `SessionCubit`           | auth               | signed-in session, role, driver status, sign-out  |
| `PhoneCubit`             | auth               | keypad digits, OTP request                        |
| `OtpCubit`               | auth               | code digits, verify, resend countdown             |
| `TermsCubit`             | auth               | rider name + terms acceptance                     |
| `LocaleCubit`            | account            | app locale (persisted, synced to `/me`)           |
| `AccountCubit`           | account            | `GET /me` profile                                 |
| `NotificationPrefsCubit` | account            | notification toggles (optimistic)                 |
| `DeleteAccountCubit`     | account            | `DELETE /me` confirmation                         |
| `NotificationsCubit`     | notifications      | inbox + unread badge                              |
| `HomeCubit`              | passenger_home     | categories, stops, time, payment, female driver, applied quote, bounded price offer |
| `QuoteCubit`             | pricing            | debounced `POST /pricing/quote`, expiry tracking, refresh |
| `DemandCubit`            | pricing            | `GET /pricing/demand` for the pickup, 60 s refresh, badge level |
| `TripRequestCubit`       | trip               | `POST /passenger/trips` with `quoteId` / `offeredPrice` (legacy estimate without a quote), `offer_out_of_range` / `quote_expired`, cancel while searching |
| `ActiveTripCubit`        | trip (app-wide)    | passenger trip feed, driver ETA, waiting timer, cancel, dismiss |
| `DriverOfferCubit`       | trip (app-wide)    | offer feed while online, 20 s countdown, accept / reject |
| `DriverTripCubit`        | trip (app-wide)    | driver trip feed, next step, PIN entry, cancel            |
| `LocationStreamCubit`    | trip (app-wide)    | permission, GPS stream, `PUT /driver/location`           |
| `RidesCubit`             | rides              | trip history                                      |
| `WalletCubit`            | wallet             | balance + payment methods                         |
| `TopUpCubit`             | wallet             | amount, confirm, success                          |
| `DriverPendingCubit`     | driver_onboarding  | application + documents status, portal link       |
| `DriverTabsCubit`        | driver_dashboard   | selected tab                                      |
| `OnlineStatusCubit`      | driver_dashboard   | online toggle (`PUT /driver/status`)              |
| `DriverOverviewCubit`    | driver_dashboard   | earnings summary + recent trips                   |
| `DriverDocumentsCubit`   | driver_dashboard   | documents + vehicle                               |

## API

Typed clients for sections 1–7 of `docs/05-api-contract.md`, the F8
endpoints of `docs/06-feature-f8-trip-lifecycle.md` and the F10 pricing
endpoints of `docs/07-feature-f9-f10-matching-pricing.md` live in each
feature's `data/datasources`. `core/network/api_client.dart` adds
`Accept-Language`, `X-Device-Id` and the Bearer token, refreshes the token
once on 401 through `/auth/refresh`, and maps the error envelope
`{ error: { code, message, details } }` to `AppException` → `Failure`.
Financial calls send an `Idempotency-Key` UUID.
