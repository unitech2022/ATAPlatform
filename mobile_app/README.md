# ATA mobile app (Flutter)

Rider + driver app for the ATA ride-hailing platform. Step 1 covers sign-in
(language, role, phone, OTP), rider onboarding, the rider home (painted map +
request sheet), rides, wallet, safety, account, driver onboarding status and
the approved-driver dashboard. F8 adds the trip lifecycle: real ride
requests, dispatch offers for drivers, the passenger/driver trip screens,
live updates over SignalR and driver location streaming. F9/F10 add fare
quotes, price offers and demand. F11 adds saved cards, card top-ups, trip
receipts and the driver wallet (statement, cash debt, payouts); F13 adds
OneSignal push with `ata://` deep links. F12 adds safety (trip sharing,
trusted contacts, SOS, "are you OK?" checks, in-trip masked chat, lost items,
safety reports) and F14 the cancellation engine (reasons from the API, fee
preview, no-show) and reliability (rates, points, restrictions). F15 adds
trip ratings, promo codes, the driver tier and driver incentives (quests);
F16 adds favourite drivers (add after a trip, request them first with the
favourite discount).
Google Maps arrives with the Maps integration (the map is still the painted canvas).

## Run

```bash
flutter pub get
flutter gen-l10n            # also runs automatically on build
flutter run \
  --dart-define=API_BASE_URL=http://10.0.2.2:5000/api/v1 \
  --dart-define=DRIVER_PORTAL_URL=http://10.0.2.2:5173/driver \
  --dart-define=ONESIGNAL_APP_ID=<onesignal-app-id>   # optional
```

| `--dart-define`     | Default                          | Purpose                                                   |
|---------------------|----------------------------------|-----------------------------------------------------------|
| `API_BASE_URL`      | `http://10.0.2.2:5000/api/v1`    | Backend base URL (`10.0.2.2` = host machine on Android)   |
| `DRIVER_PORTAL_URL` | `http://10.0.2.2:5173/driver`    | Website page opened by "الانتقال لموقع رفع المستندات"     |
| `SHOW_DEV_OTP`      | `true`                           | Show the `devCode` returned by the API on the OTP screen  |
| `HUB_URL`           | derived: `<API origin>/hubs/trips` | SignalR trips hub (JWT sent as `access_token` query param) |
| `SIMULATE_LOCATION` | `false`                          | Emit a fake Riyadh position instead of the GPS (emulators / tests) |
| `ONESIGNAL_APP_ID`  | empty                            | OneSignal app id; empty = push disabled (`NoopPushService`) |

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

### Payments (F11)

- `payments` feature: `PaymentMethodsPage` (`/wallet/payment-methods`) lists
  the saved cards (`GET /passenger/payment-methods`), sets the default
  (`POST …/{id}/default`) and deletes (`DELETE …/{id}`, `409
  payment_method_in_use` shown inline). `AddCardPage`
  (`/wallet/payment-methods/add`) validates number (Luhn), expiry (`MM/YY`,
  not past) and CVC on the device, then `AddPaymentMethod` tokenises through
  the `CardTokenizer` abstraction and sends **only the token**
  (`{ token, setDefault, returnUrl: ata://payments/return }`); the PAN never
  reaches our API. `SandboxCardTokenizer` (registered in `data_module.dart`,
  swap for the provider SDK tokenizer in production) maps test cards:
  `4000 0000 0000 0002` → `tok_sandbox_declined`, `4000 0000 0000 3220` →
  `tok_sandbox_3ds`, mada BINs (`4406 4700 0000 0007`) → `tok_sandbox_mada`,
  anything else → `tok_sandbox_visa`.
- 3-D Secure (`202` with `action`): the card / top-up flow shows
  `PaymentActionView`, which opens `action.url` in the browser; the final
  state always comes from the API (never trusted from the return URL).
- Top-up: `TopUpCubit` picks a saved card (default preselected) or the
  sandbox (`POST /wallet/topups` `method: card|sandbox`, `paymentMethodId`,
  `Idempotency-Key`); the driver variant (`/driver/top-up?amount=`) sends
  `?kind=driver` to settle the cash debt.
- Receipts: `ReceiptPage` (`/rides/:tripId/receipt`, also `/rides/:tripId`)
  renders `GET /passenger/trips/{id}/receipt`: API-localized lines, discount
  source badges (promotion / favourite driver), subtotal, total, VAT,
  payment (card brand •••• last4), card→cash fallback notice, refunds and net
  paid; `409` shows "no receipt". Reached from completed trips in the rides
  list and from the end-of-trip view ("عرض الإيصال").
- Blocks and fallbacks: a negative wallet shows a "top up to continue"
  banner on the wallet page; `422 outstanding_balance` on `POST
  /passenger/trips` replaces the home request error with the same banner and
  disables the button until the rider comes back from the top-up. Payment
  error codes (`payment_failed`, `payment_method_expired`,
  `payment_method_in_use`, `payment_provider_unavailable`, …) have their own
  texts; `payment_fallback_cash` trip events show a notice to the rider and
  `collectCashAmount` a "collect in cash" line to the driver.
- `driver_wallet` feature: `DriverEarningsPage` (`/driver/earnings`,
  today / week / month filter over `GET /driver/earnings/statement`),
  `DriverPayoutsPage` (`/driver/payouts`: cash-debt card, history with
  status badges, cancel while `requested`) and `PayoutRequestPage`
  (`/driver/payouts/request`: available balance, masked IBAN, amount checked
  against `minPayoutAmount` and `availableForPayout` before `POST
  /driver/payouts`). The overview shows the cash-debt card (debt vs. limit,
  "سداد") and links; "تحويل الأرباح" opens the payout request.
  `OnlineStatusCubit` turns `403 cash_debt_limit_exceeded` (`details {
  cashDebt, limit }`) into a "can't go online" card with a settle button.

### Notifications and deep links (F13)

- `core/push/`: `PushService` with `OneSignalPushService`
  (`onesignal_flutter`, initialized in `configureDependencies` when
  `ONESIGNAL_APP_ID` is set) and `NoopPushService` (empty id, tests).
  Foreground pushes for `offer.received`, `trip.*` and `safety.check` are
  rendered by the app (`PushForegroundPolicy`), others as system banners.
- `PushSessionBinder` (`app/push/`, bound in `bootstrapApp`) follows
  `SessionCubit` / `LocaleCubit`: sign-in → `login(userId)`, tags `role` /
  `lang` / `city` (`riyadh` until the profile exposes a city),
  `setLanguage`, the permission prompt (only after a sign-in) and `PUT
  /me/devices` with the subscription id; role / locale changes update the
  tags; sign-out → `logout()`.
- `DeepLinkCubit` (app-wide) receives tapped pushes (`data.deepLink`,
  `notificationId` → `POST /notifications/{id}/opened`) and inbox rows,
  parks links until the session is ready, and `AtaApp` navigates with
  `router.go`. `ParseDeepLink` implements the `docs/08` §F13.7 table
  (`ata://trip/{id}` → `/trip` when active else `/rides/{id}`,
  `ata://rides/{id}/receipt`, `ata://wallet`, `ata://driver/documents` →
  `/driver?tab=documents`, `ata://driver/payouts`, `ata://notifications` →
  inbox sheet, …); unknown links or a role mismatch fall back to `/home` /
  `/driver`, and routes not shipped yet hit the router's `onException`
  fallback. Redirect rules (active trip, pending offer) still win.
- `NotificationsCubit` reloads the inbox on every foreground push; rows use
  the event category icon, legacy `snake_case` types are normalized and
  older rows get a derived link.
- Platform setup for real pushes (not in this repo): OneSignal app with the
  FCM / APNs credentials, iOS Push capability and Notification Service
  Extension.

### Safety (F12)

- `safety` feature (`docs/09` §F12.7). Trip share: the driver card's
  "مشاركة" calls `POST /safety/trips/{tripId}/shares {channel: link}` and
  hands the returned `https://ata.sa/t/{token}` URL to share_plus
  (`TripShareCubit.shareRequest` triggers the page listener); "إدارة" opens
  `TripShareSheet` (links with view counts + revoke `DELETE
  /safety/shares/{id}`, SMS to selected trusted contacts with `channel: sms,
  contactIds`). Trusted contacts (`/safety/contacts`,
  `TrustedContactsCubit`): list / inline add-edit form / delete, max 5,
  Saudi mobile validated and sent as E.164, per-contact auto-share toggle
  (optimistic) and "alert in an emergency"; `trusted_contacts_limit`,
  `trusted_contact_exists`, `phone_invalid` and `validation_failed
  {phoneNumber: self}` are mapped.
- SOS: red `SosButton` on `/trip` (once a driver is assigned),
  `/driver/trip` and the safety page. Press-and-hold 2 s confirmation is a
  `SosHoldCubit` driven by a ticker (no widget state); confirming calls the
  app-wide `SosCubit`: reads the position (5 s timeout, falls back to the
  trip pickup / driver's last position), `POST /safety/sos {tripId, role,
  lat, lng, accuracy, notifyTrustedContacts: true}`, then `POST
  /safety/sos/{caseId}/location` every 10 s and refreshes the case status
  every 30 s. `SosPanel` shows the case number / status, contacts notified,
  "اتصل بالطوارئ 911" (`tel:` the returned `emergencyNumber`) and "ضغطت
  بالخطأ" (`POST …/cancel {reason: accidental}`).
- "هل أنت بخير؟": app-wide `SafetyCheckCubit` (riders) listens to the hub
  `SafetyCheck` event and foreground `safety.check` pushes, restores `GET
  /safety/alerts/pending` on start, and is pointed at
  `ata://safety/check/{alertId}` links by `SafetyCubits` (the push `ok` /
  `help` buttons answer immediately). `SafetyCheckPrompt` (trip overlay,
  safety page, `/safety/check/:alertId`) counts down to `respondBy` and
  sends `POST /safety/alerts/{id}/respond`.
- Reports: `/safety/report?tripId=` (non-emergency report), `/safety/cases`
  and `/safety/cases/:caseId` (status + public notes), lost items
  `/rides/:tripId/lost-item` (form) and `/safety/lost-items` (statuses);
  both reached from the receipt page (`TripHelpActions`). Drivers answer
  lost items at `/driver/lost-items` (settings tab).
- `trip_chat` feature: app-wide `TripChatCubit` bound by `SafetyCubits` to
  the active trip (driver assigned … in trip). Feed = hub `TripMessage` +
  polling every 5 s (`TripWatcher`), optimistic sending (pending / failed →
  tap to retry), quick replies from `GET /catalog/chat-quick-replies`,
  unread badge on the chat button, `POST …/messages/read` while
  `/trip/chat` / `/driver/trip/chat` is visible (router listener),
  `409 chat_closed` → read-only. Calls go through `MaskedCallCubit`
  (`POST …/call`: `proxy` dials the proxy number and shows the PIN,
  `unavailable` opens the chat); the real number is never dialled.
- Redirects: during a trip riders may open `/trip/chat` and `/safety/*`,
  drivers `/driver/trip/chat`.

### Cancellation and reliability (F14)

- `CancelReasonSheet` is built on `CancelFlowCubit`: reasons from `GET
  /catalog/cancellation-reasons?actor=&stage=` (stage estimated on the
  device from the trip status / `arrivedAt`, the API decides), selecting a
  reason runs `POST …/cancel/preview` (fee for riders, penalty points for
  drivers, free window, scheduled-booking label, review notice for
  excusable reasons, safety notice for emergency reasons), reasons with
  `requiresNote` require a note, confirming sends `expectedFee` /
  `expectedPenaltyPoints`; `409 cancellation_fee_changed` re-runs the
  preview. The sheet pops the cancelled `Trip` which the trip cubit adopts;
  the ended views show the fee charged / under review and the driver's
  compensation (`Trip.cancellation`).
- Driver no-show: `NoShowSection` on the waiting screen (`NoShowCubit`
  counts down from `arrivedAt` + 5 min, `Cancellation:NoShowWaitMinutes`;
  `422 no_show_too_early {secondsRemaining}` resyncs), confirm dialog →
  `POST /driver/trips/{id}/no-show {lat, lng}`.
- Reliability: `ReliabilityCubit` + `ReliabilityCard` on the account page
  (rider) and the dashboard overview (driver); details at
  `/account/reliability` and `/driver/reliability` (rates, points, next
  level, recent cancellations, driver effects). `403 account_restricted
  {level, restrictedUntil}` becomes a blocking `RestrictionBlockCard` in
  `OnlineStatusCubit` and a dated message in `TripRequestCubit`; the
  deprioritized / reduced-incentives levels are explained on the card.

### Ratings, promo codes, tier and incentives (F15)

`docs/10` §F15 (favourite drivers are the next section, F16).

- `rating` feature. `RatingCubit` drives the form: 1–5 stars (key
  `star-<n>`), tags from `GET /catalog/rating-tags?target=driver|passenger`
  (the seeded codes are shown with local labels if the catalog fails), the
  tag header follows the stars (≤ 3 "ما الذي لم يعجبك؟", ≥ 4 "ما الذي
  أعجبك؟", crossing the boundary clears the chosen tags), a comment of at
  most 500 characters and one submission (`POST /passenger|driver/trips/{id}/
  rating`). `SubmitRating` validates stars / comment locally; `409
  rating_exists` finishes the form, `422 rating_window_closed` closes it.
  `RatingSheet.show` (bottom sheet, usable on the pinned `/trip` and
  `/driver/trip` pages) is opened by `TripRateButton` on the rider receipt
  view ("قيّم الرحلة", replaces the old "قريباً" button) and the driver trip
  summary ("قيّم الراكب"), by the "قيّم" pill of completed trips in the rides
  list inside the 72 h window (`canRate` / `rateUntil` / `myRating`, falling
  back to `completedAt` + 72 h), by the pending prompt and by `/rate/:tripId`
  (rider) · `/driver/rate/:tripId` (driver) for `ata://rate/{tripId}`.
- `PendingRatingCubit` (app-wide, bound by `RatingPromptBinder` in
  `bootstrapApp`): loads `GET /passenger|driver/ratings/pending` on sign-in /
  app start and again when the rider's or driver's trip completes, and
  exposes one prompt at a time. `PendingRatingCard` (top of the home request
  sheet, top of the driver overview) shows it with "قيّم الآن" / dismiss;
  a dismissed prompt is not shown again this session. Ratings sent from any
  screen are remembered so the rate buttons turn into "تم تقييم الرحلة".
- `/driver/ratings` (`RatingSummaryCubit`): average, distribution, top tags
  (positive / negative) and anonymous recent comments (week only).
- `promotions` feature. Home sheet row "كود خصم" → `PromoCodeSheet`:
  `PromoCodeCubit` normalizes the code (uppercase, 4–20 letters / digits,
  otherwise `promo_not_found` locally) and validates it with `POST
  /passenger/promotions/validate {code, quoteId, rideCategoryId,
  paymentMethod, bookingType}`. The applied code goes to `HomeCubit`
  (`HomeSync`), which re-quotes with `promoCode`; category tiles show the
  struck-through price before the discount, `FareBreakdownSheet` lists
  `breakdown.discounts[]` (label + source) and the total before the
  discount, and `POST /passenger/trips` carries `promoCode`. A quote with
  `promotion.valid = false` or a `promo_*` error on the request removes the
  code with the localized reason (`promo_not_found`, `promo_expired`,
  `promo_usage_limit_reached` (+ `scope: user`), `promo_not_eligible` with
  every `details.reason`). Offering a price disables the promo row (the code
  is kept but never sent; `TripRequestCubit` drops it for `pricingMode:
  offer`). The receipt view shows the trip's `promotion`.
- `/promotions` ("العروض", `ata://promotions`, wallet page + header menu):
  `PromotionsCubit` with available / used / expired tabs, copy the code or
  "استخدم" (opens `/home?promo=CODE`; the code is validated once the first
  quote arrives).
- `driver_rewards` feature. `DriverTierCubit` + `TierCard` on the overview
  (badge, progress to the next tier, criteria met, commission discount) and
  `/driver/tier` (`ata://driver/tier`: each criterion vs. the next tier,
  next recalculation). `IncentivesCubit` (`GET /driver/incentives?status=`
  active / upcoming / completed) + `/driver/incentives`, the "أقرب حافز" card
  on the overview; `IncentiveDetailCubit` + `/driver/incentives/:id`
  (window, zones, categories, opt-in with `409 incentive_opt_in_closed`).
  Rewards show the reliability `effects.incentiveMultiplier` (`GET
  /driver/reliability`): below 1 the reduced amount is shown with a notice.
  The earnings statement lists incentives in the totals and per day.
- Deep links / events: `rating.reminder` → `ata://rate/{tripId}`,
  `promo.new` → `ata://promotions`, `incentive.new` / `incentive.achieved` →
  `ata://driver/incentives/{id}`, `driver.tier_changed` → `ata://driver/tier`
  (also derived for inbox rows without `data.deepLink`).

### Favourite drivers (F16)

`docs/10` §F16 and §1 (discount engine).

- `favorite_drivers` feature (`GET|POST /passenger/favorite-drivers`, `DELETE
  …/{driverId}`, `GET …/available?lat&lng&rideCategoryId`). Use cases
  `GetFavoriteDrivers`, `AddFavoriteDriver` (exactly one of `driverId` /
  `tripId`), `RemoveFavoriteDriver`, `GetAvailableFavorites`.
- `/account/favorite-drivers` (header menu "السائقون المفضلون" + the "إدارة"
  link of the request sheet): `FavoriteDriversCubit` lists the favourites
  (name, rating, vehicle, trips together, last trip); an `AvailableFavoritesCubit`
  around the fixed pickup adds the "متاح الآن · يصل خلال n" badge; removal
  asks for confirmation (`RemoveFavoriteDialog`) and shows a snackbar;
  empty state explains how to add.
- Adding: the rating form (riders) has the option "أضف إلى المفضلة"
  (`RatingCubit.toggleAddToFavorites`, calls `AddFavoriteDriver {tripId}`
  only after the rating was sent; a failure is a notice on the thank-you
  screen), and `AddFavoriteButton` (own `AddFavoriteCubit`, `409
  favorite_exists` = already a favourite) sits on the end-of-trip summary,
  the receipt page and, as a heart, on completed trips of the rides list.
  `favorite_not_eligible`, `favorites_limit` and `favorite_exists` have
  their own texts (`favorites_failure_text.dart`).
- Home request sheet: `FavoriteDriversRow` shows the favourites available now
  from `AvailableFavoritesCubit` (debounce 600 ms on pickup / category, 30 s
  refresh) as chips (name, rating, ETA, "خصم 10%"). Picking one calls
  `HomeCubit.toggleFavorite` (tap again or "إزالة" deselects) and sets
  `favoriteDriverId` on the quote (`POST /pricing/quote`) and on `POST
  /passenger/trips`; the sheet states "سيصل طلبك أولاً إلى {name}" and "إن لم
  يكن متاحاً سنبحث عن أقرب كابتن". The quote is priced assuming acceptance
  (`favoriteDiscountConditional`): the favourite line (`discounts[]` source
  `favorite_driver`) is shown as "وفّرت …" plus "يُطبّق الخصم عند قبول
  {name}", the category tiles show the price before / after and
  `FareBreakdownSheet` labels the line with the "الكابتن المفضل" source.
  Promo interplay follows the API: a quote with `promotion.reason:
  not_stacked` (or a favourite line without a promo line) keeps the code but
  explains "لم يُطبَّق كود الخصم…" on both rows; a promo line without a
  favourite line explains "لم يُطبَّق خصم المفضل…" (`FavoritePromoOutcome`).
  Offering your own price disables the row ("غير متاح مع اقتراح السعر"; the
  selection is kept but `TripRequestCubit` drops `favoriteDriverId` for
  `pricingMode: offer`). `422 validation_failed {favoriteDriverId:
  not_favorite}` clears the selection and refreshes the availability.
- Searching: `Trip.favorite { driverId, driverName, status, discountApplied }`
  drives the trip page. While `status = requested` the view says "نتواصل مع
  كابتنك المفضل..." (+ "طلبك موجّه أولاً إلى {name}…"); after `rejected` /
  `expired` / `unavailable` it goes back to the normal searching copy with a
  notice ("لم يتمكن {name} من الرد، نبحث لك عن كابتن آخر" / "{name} غير متاح
  حالياً…"). The hub `TripUpdated` and polling carry the field; the push
  `trip.favorite_fallback` opens `ata://trip/{tripId}` (`/trip`).
- Trip end: the summary shows "خصم الكابتن المفضل · مطبّق" when
  `favorite.discountApplied`, the itemised receipt already lists the
  discount with the "الكابتن المفضل" badge, and the driver card shows a heart
  when the assigned driver is a favourite (`favorite.status = accepted` for
  that driver, or the optional `driver.isFavorite` flag).
- Driver offers carry `isFavoriteRequest` / `exclusive`: the offer page shows
  "من راكب يفضّلك" / "عرض حصري لك". (`GET /driver/favorites/count` and the
  scheduled favourite priority of F17 are not part of this step.)
- Assumptions beyond the spec: the driver photo (`photoUrl`) needs auth and
  is shown as a placeholder; `promotion.reason` may be `not_stacked`;
  `Trip.driver.isFavorite` is optional.

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
                            network/ (dio ApiClient + interceptors), push/ (PushService, OneSignal, no-op),
                            storage/ (secure tokens, prefs),
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
`pricing`, `payments`, `driver_wallet`, `trip_chat`, `rating`, `promotions`,
`driver_rewards`, `favorite_drivers`.

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
  use cases (with `payments_module.dart`, `safety_module.dart` and
  `rewards_module.dart` for F11/F13, F12/F14 and F15/F16). Tests register fake
  repositories and reuse the real use cases.
- The router (`go_router`) redirects from `SessionCubit` state: unknown →
  splash, signed-out → `/auth/*`, new rider → terms, driver not approved →
  `/driver/pending`, approved driver → `/driver`, rider → `/home`. The trip
  cubits add: rider with an active trip → `/trip`, driver with an active trip
  → `/driver/trip`, driver with a pending offer → `/driver/offer`.
- App-wide trip cubits live in `TripCubits` (bootstrap): it binds them to the
  session (passenger feed for riders, driver feed for approved drivers, all
  stopped on sign-out) and feeds the router's `refreshListenable`.
  `SafetyCubits` (`app/safety_cubits.dart`) holds the app-wide SOS, safety
  check and chat cubits and binds them to the session, the trip cubits, the
  deep links and the router.
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
| `NotificationsCubit`     | notifications      | inbox + unread badge, reload on foreground push   |
| `DeepLinkCubit`          | notifications (app-wide) | `ata://` links from pushes / inbox → router location |
| `HomeCubit`              | passenger_home     | categories, stops, time, payment, female driver, applied quote, bounded price offer |
| `QuoteCubit`             | pricing            | debounced `POST /pricing/quote`, expiry tracking, refresh |
| `DemandCubit`            | pricing            | `GET /pricing/demand` for the pickup, 60 s refresh, badge level |
| `TripRequestCubit`       | trip               | `POST /passenger/trips` with `quoteId` / `offeredPrice` (legacy estimate without a quote), `offer_out_of_range` / `quote_expired`, cancel while searching |
| `ActiveTripCubit`        | trip (app-wide)    | passenger trip feed, driver ETA, waiting timer, cancel, dismiss |
| `DriverOfferCubit`       | trip (app-wide)    | offer feed while online, 20 s countdown, accept / reject |
| `DriverTripCubit`        | trip (app-wide)    | driver trip feed, next step, PIN entry, cancel            |
| `LocationStreamCubit`    | trip (app-wide)    | permission, GPS stream, `PUT /driver/location`           |
| `RidesCubit`             | rides              | trip history                                      |
| `WalletCubit`            | wallet             | balance (negative → banner) + payment methods     |
| `TopUpCubit`             | wallet             | amount, card / sandbox source, 3-D Secure step, success, driver `kind` |
| `PaymentMethodsCubit`    | payments           | saved cards, default card, delete                 |
| `AddCardCubit`           | payments           | card form validation, tokenise → save → 3-D Secure |
| `ReceiptCubit`           | payments           | itemised trip receipt                             |
| `EarningsStatementCubit` | driver_wallet      | statement for today / week / month                |
| `PayoutSummaryCubit`     | driver_wallet      | balance, cash debt vs. limit, payout eligibility  |
| `PayoutRequestCubit`     | driver_wallet      | payout amount validation + request                |
| `PayoutsCubit`           | driver_wallet      | payout history, cancel                            |
| `DriverPendingCubit`     | driver_onboarding  | application + documents status, portal link       |
| `DriverTabsCubit`        | driver_dashboard   | selected tab                                      |
| `OnlineStatusCubit`      | driver_dashboard   | online toggle (`PUT /driver/status`), cash-debt block |
| `DriverOverviewCubit`    | driver_dashboard   | earnings summary + recent trips                   |
| `DriverDocumentsCubit`   | driver_dashboard   | documents + vehicle                               |
| `TrustedContactsCubit`   | safety             | trusted contacts, inline form, limit 5, auto-share |
| `TripShareCubit`         | safety             | tracking link → share_plus, SMS to contacts, revoke |
| `SosHoldCubit`           | safety             | press-and-hold progress (ticker) of the SOS button |
| `SosCubit`               | safety (app-wide)  | raise SOS, 10 s location stream, cancel, status   |
| `SafetyCheckCubit`       | safety (app-wide)  | "are you OK?" prompt, countdown, respond          |
| `SafetyCasesCubit`       | safety             | my reports / one case                             |
| `SafetyReportCubit`      | safety             | non-emergency report form                         |
| `LostItemCubit`          | safety             | lost item form                                    |
| `LostItemsCubit` / `DriverLostItemsCubit` | safety | lost item statuses / driver found-not-found |
| `TripChatCubit`          | trip_chat (app-wide) | messages, optimistic send, quick replies, unread |
| `MaskedCallCubit`        | trip_chat          | proxy call or fallback to chat                    |
| `CancelFlowCubit`        | trip               | reasons → fee / points preview → confirm          |
| `NoShowCubit`            | trip               | no-show countdown from `arrivedAt`, confirm       |
| `ReliabilityCubit`       | trip               | reliability summary (rider / driver)              |
| `RatingCubit`            | rating             | stars, tags by stars, comment, single submit, rider add-to-favourites |
| `PendingRatingCubit`     | rating (app-wide)  | unrated recent trips, one prompt, dismissed / rated |
| `RatingSummaryCubit`     | rating             | driver's own rating summary                       |
| `PromoCodeCubit`         | promotions         | promo input, validate against the quote, apply / remove, errors |
| `PromotionsCubit`        | promotions         | available / used / expired promotions             |
| `DriverTierCubit`        | driver_rewards     | tier, next-tier criteria, benefits                |
| `IncentivesCubit`        | driver_rewards     | quests per tab, reliability multiplier, nearest quest |
| `IncentiveDetailCubit`   | driver_rewards     | one quest, opt-in                                 |
| `FavoriteDriversCubit`   | favorite_drivers   | my favourite drivers, removal                     |
| `AvailableFavoritesCubit`| favorite_drivers   | favourites available now around the pickup (debounce + 30 s refresh) |
| `AddFavoriteCubit`       | favorite_drivers   | add the driver of a completed trip (`tripId`), already-a-favourite |

## API

Typed clients for sections 1–7 of `docs/05-api-contract.md`, the F8
endpoints of `docs/06-feature-f8-trip-lifecycle.md` and the F10 pricing
endpoints of `docs/07-feature-f9-f10-matching-pricing.md` and the passenger /
driver endpoints of `docs/08-feature-f11-f13-payments-notifications.md` and
`docs/09-feature-f12-f14-safety-cancellation.md` and the F15 / F16 endpoints of
`docs/10-feature-f15-f16-ratings-promotions-favorites.md` live in each feature's
`data/datasources`. `core/network/api_client.dart` adds
`Accept-Language`, `X-Device-Id` and the Bearer token, refreshes the token
once on 401 through `/auth/refresh`, and maps the error envelope
`{ error: { code, message, details } }` to `AppException` → `Failure`.
Financial calls send an `Idempotency-Key` UUID.
