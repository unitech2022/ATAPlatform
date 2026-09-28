# ATA mobile app (Flutter)

Rider + driver app for the ATA ride-hailing platform. Step 1 covers sign-in
(language, role, phone, OTP), rider onboarding, the rider home (painted map +
request sheet), rides, wallet, safety, account, driver onboarding status and
the approved-driver dashboard. Trip dispatch, live tracking and Google Maps
arrive in Step 2.

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
`safety`, `account`, `notifications`, `driver_dashboard`, `catalog`.

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
  `/driver/pending`, approved driver → `/driver`, rider → `/home`.
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
| `HomeCubit`              | passenger_home     | categories, stops, time, payment, female driver, request |
| `RidesCubit`             | rides              | trip history                                      |
| `WalletCubit`            | wallet             | balance + payment methods                         |
| `TopUpCubit`             | wallet             | amount, confirm, success                          |
| `DriverPendingCubit`     | driver_onboarding  | application + documents status, portal link       |
| `DriverTabsCubit`        | driver_dashboard   | selected tab                                      |
| `OnlineStatusCubit`      | driver_dashboard   | online toggle (`PUT /driver/status`)              |
| `DriverOverviewCubit`    | driver_dashboard   | earnings summary + recent trips                   |
| `DriverDocumentsCubit`   | driver_dashboard   | documents + vehicle                               |

## API

Typed clients for sections 1–7 of `docs/05-api-contract.md` live in each
feature's `data/datasources`. `core/network/api_client.dart` adds
`Accept-Language`, `X-Device-Id` and the Bearer token, refreshes the token
once on 401 through `/auth/refresh`, and maps the error envelope
`{ error: { code, message, details } }` to `AppException` → `Failure`.
Financial calls send an `Idempotency-Key` UUID.
