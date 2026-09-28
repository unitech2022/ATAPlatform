# F17 الرحلات المجدولة والمطار + F18 الدعم

الطبقات: MySQL → API (وحدات `Scheduling`, `Airport`, `Support` + تعديلات على `Trips` و`Matching` و`Cancellation`) → Flutter (features `scheduled_rides`, `airport`, `support`) → الموقع (مركز المساعدة `/help`) → لوحة الإدارة (قواعد الجدولة، الرحلات المجدولة، المطارات وطابورها، التذاكر، النزاعات، الردود الجاهزة، مقالات المساعدة).

يعتمد على: `06`، `07`، `08` (الاستردادات §F11.4، الإشعارات §F13.2، الروابط §F13.7)، `09` (محرك الإلغاء والموثوقية، المفقودات)، `10` (المفضل). الصلاحيات من `12` §F20.

---

# F17 — الرحلات المجدولة

## F17.1 التغييرات على دورة الرحلة
- حالة جديدة **`scheduled`** (قيمة JSON `scheduled`): الرحلة المجدولة تُنشأ بها بدلاً من `searching` (تعديل على F8). تُعدّ غير نهائية لكنها **لا** تُحتسب "رحلة نشطة" لقاعدة `trip_active_exists`، ولا تظهر في `GET /passenger/trips/active`. للراكب حتى `max_open_per_passenger` رحلة مجدولة مفتوحة (`422 scheduled_limit_reached { max }`).
- الانتقالات الجديدة:

| الانتقال | من يقوم به | الشرط |
|---|---|---|
| scheduled → driver_assigned | السائق المحجوز (التأكيد النهائي) | ضمن نافذة التأكيد النهائي، السائق متصل وبلا رحلة |
| scheduled → searching | النظام | عند `T − search_start_minutes_before` بلا سائق مؤكَّد نهائياً |
| scheduled → cancelled | الراكب / الإدارة / النظام | — |
| driver_assigned / driver_en_route → searching | النظام | **رحلات مجدولة فقط**: إلغاء السائق أو عدم حضوره → إعادة مطابقة بدل الإلغاء (`Trip.Reassign()` يمسح `driver_id`, `vehicle_id`, `assigned_at`؛ حدث `driver_unassigned`) |

  حيث `T = scheduled_at`. الإعداد `Trips:ScheduledLeadMinutes` يُستبدل بـ`scheduled_ride_rules.search_start_minutes_before`.
- التسعير: عند الحجز يُسعَّر بمضاعِفات الوقت عند `scheduled_at` ومستوى طلب **`normal` مثبّت** (`lock_demand_normal`)؛ الأجرة النهائية تُحسب بالمسافة الفعلية مع نفس الطلب المثبّت (`LockedDemand` في `TripPricing`).
- الدفع: رحلات البطاقة المجدولة تُفوَّض عند `T − search_start_minutes_before` (أو عند التأكيد النهائي) وليس عند الحجز؛ فشل التفويض → إشعار `payment.failed` وتحويل إلى نقد مع حدث `payment_fallback_cash (authorization_failed)`.

## F17.2 الجداول

| جدول | الأعمدة |
|---|---|
| `scheduled_ride_rules` | id, city_id NULL, ride_category_id NULL, max_days_ahead INT (7)، min_lead_minutes INT (30)، max_open_per_passenger INT (3)، lock_demand_normal BOOL (true)، marketplace_enabled BOOL (true)، marketplace_radius_km INT (30)، favorite_exclusive_minutes INT (30)، driver_assignment_lead_minutes INT (60) (موعد طلب التأكيد الأول)، confirmation_timeout_minutes INT (10)، final_confirmation_minutes_before INT (15)، final_confirmation_timeout_minutes INT (5)، search_start_minutes_before INT (10)، rider_reminder_offsets JSON (`[1440, 60, 15]` دقائق)، driver_reminder_offsets JSON (`[1440, 180]`)، free_cancel_minutes_before INT (60)، late_cancel_fee_type (`none`/`fixed`/`percent`/`pricing_rule`)، late_cancel_fee_amount NULL (10)، late_cancel_fee_percent NULL, late_cancel_driver_compensation_percent DECIMAL(5,2) (50)، driver_free_release_minutes_before INT (120)، driver_late_release_penalty_points INT (3)، driver_confirmation_missed_penalty_points INT (3)، driver_no_show_penalty_points INT (6)، driver_no_show_grace_minutes INT (10)، max_reservations_per_driver INT (5)، reservation_gap_minutes INT (30)، is_active, created_at, updated_at — UNIQUE(city_id, ride_category_id) |
| `scheduled_ride_reservations` | id, trip_id (FK)، driver_id (FK)، source (`marketplace`/`favorite`/`admin`)، status (`reserved`/`confirmed`/`assigned`/`released`/`no_show`/`completed`/`cancelled`)، reserved_at, confirm_requested_at NULL, confirmed_at NULL, final_confirm_requested_at NULL, assigned_at NULL, released_at NULL, release_reason NULL (`driver_released`/`confirmation_missed`/`final_confirmation_missed`/`no_show`/`trip_cancelled`/`admin`)، is_late_release BOOL, penalty_points INT DEFAULT 0, created_at, updated_at — INDEX(driver_id, status)، INDEX(trip_id, status) (حجز فعّال واحد لكل رحلة) |
| `scheduled_ride_reminders` | id, trip_id, reservation_id NULL, recipient_user_id, recipient_role (`passenger`/`driver`)، kind (`reminder`/`confirm_request`/`final_confirm_request`)، offset_minutes INT, send_at, sent_at NULL, status (`pending`/`sent`/`skipped`/`cancelled`)، created_at — INDEX(status, send_at) |

تعديلات `trips`: `reserved_driver_id CHAR(36) NULL`، و`status` يقبل `scheduled`.

اختيار القاعدة: (المدينة + الفئة) ← المدينة ← الفئة ← العامة (كلاهما NULL)، النشطة فقط. البذور تحتوي قاعدة عامة واحدة بالقيم الافتراضية أعلاه.

## F17.3 القواعد

### نافذة الحجز (مواصفة v1.1)
- `scheduledAt ≤ now + max_days_ahead × 24h` (مقاسة من **وقت الحجز**؛ مثال: حجز في 26 سبتمبر 10:00 يسمح حتى 3 أكتوبر 10:00) وإلا `422 schedule_window_exceeded { maxScheduledAt }`.
- `scheduledAt ≥ now + min_lead_minutes` وإلا `422 schedule_lead_too_short { minScheduledAt }`.
- تحل هذه محل حدود F8 الحالية (10 دقائق / 30 يوماً) في `TripValidation.Booking`، وتنطبق أيضاً على `/pricing/quote` و`/passenger/trips/estimate` عند `bookingType=scheduled`.

### الخط الزمني
```
الحجز ──► [نافذة المفضل: favorite_exclusive_minutes] ──► السوق مفتوح للسائقين ──► T−60 طلب التأكيد الأول ──► T−15 طلب التأكيد النهائي ──► T−10 بدء البحث العادي (إن لم يُسنَد) ──► T
```
1. **الحجز**: الرحلة `scheduled` + تذكيرات الراكب (`rider_reminder_offsets` التي لم يفت وقتها) + إشعار `scheduled.booked`.
2. **المفضل**: إن حدد الراكب `favoriteDriverId` → `scheduled.favorite_request` للسائق، وخلال `favorite_exclusive_minutes` لا تظهر الرحلة في السوق إلا له. المفضل يحجز بنفس نقطة الحجز (`source=favorite`). بعد النافذة تُفتح للجميع ويبقى المفضل قادراً على الحجز. الخصم (F16) يُطبق إن كان السائق المُسنَد نهائياً هو المفضل وقاعدة الخصم تشمل `scheduled`؛ حينها `favorite_status = accepted`.
3. **السوق**: يرى السائق الرحلات `scheduled` بلا حجز فعّال، في مدينته، ضمن `marketplace_radius_km` من موقعه الحالي/الأخير، المتوافقة مع فئة مركبته (وأعلى إن `allow_category_upgrade`)، وجنس السائق إن طلبت الراكبة سائقة، والسائق غير مقيّد (`09`) ولا يتجاوز دين النقد. تُعرض بلا هوية الراكب وبموقع التقاط تقريبي (3 خانات عشرية) حتى الحجز.
4. **الحجز من السائق**: متاح حتى `T − search_start_minutes_before`. الشروط: لا حجز فعّال آخر للرحلة (`409 reservation_taken`)، عدد حجوزاته الفعّالة < `max_reservations_per_driver` (`422 reservation_limit_reached`)، ولا تداخل: `[T, T + estimated_duration + reservation_gap_minutes]` لا يتقاطع مع حجوزاته الأخرى (`409 reservation_conflict { conflictingTripId }`). النتيجة: `reserved`، `trips.reserved_driver_id`، تذكيرات السائق، إشعار `scheduled.driver_reserved` للراكب. إن كان الحجز بعد `T − driver_assignment_lead_minutes` فهو `confirmed` مباشرة.
5. **التأكيد الأول** عند `T − driver_assignment_lead_minutes`: `scheduled.confirm_request` (حرج). بلا تأكيد خلال `confirmation_timeout_minutes` → `released (confirmation_missed)` + نقاط `driver_confirmation_missed_penalty_points` + عودة الرحلة للسوق + `scheduled.reservation_released` للطرفين.
6. **التأكيد النهائي** عند `T − final_confirmation_minutes_before`: `scheduled.confirm_request` (نهائي). التأكيد يتطلب أن يكون السائق متصلاً وبلا رحلة جارية (وإلا `409 reservation_not_confirmable { reason: "offline" | "on_trip" }`) → `assigned` + `Trip.Assign(driver)` → `driver_assigned` (ثم يكمل F8 المعتاد). بلا تأكيد خلال `final_confirmation_timeout_minutes` → `released (final_confirmation_missed)` + نقاط + الرحلة → `searching` فوراً + `scheduled.rematched` للراكب.
7. **بدء البحث** عند `T − search_start_minutes_before` لرحلة ما زالت `scheduled` بلا إسناد → `searching`؛ المطابقة تعالج الرحلات المجدولة قبل الفورية في كل دورة.
8. **عدم حضور السائق**: رحلة مجدولة مُسنَدة لم يصل سائقها (`arrived_at` فارغ) حتى `T + driver_no_show_grace_minutes` → الحجز `no_show` + نقاط `driver_no_show_penalty_points` + `no_show_count` للسائق + `Trip.Reassign()` → `searching` + `scheduled.rematched`. للراكب أيضاً إلغاء مجاني في هذه الحالة (قاعدة: `stage` محسوب كـ`before_accept` بعد الـReassign).
9. **تحرير السائق للحجز** قبل الإسناد: مجاني قبل `T − driver_free_release_minutes_before`؛ بعده `is_late_release = true` + نقاط `driver_late_release_penalty_points`. إلغاء السائق بعد الإسناد لرحلة مجدولة = Reassign (لا إلغاء للرحلة) + نقاط حسب قواعد `cancellation_rules` لـ`actor=driver` (المرحلة المعنية).
10. **الموثوقية**: `ReliabilityService` (`09`) يحتسب حجوزات `released` المتأخرة/`confirmation_missed`/`final_confirmation_missed`/`no_show` كإلغاءات مخطئة للسائق (`cancellations_at_fault`، والنقاط من `penalty_points`)؛ و`trips_accepted` يشمل الحجوزات.

### إلغاء الراكب لرحلة `scheduled`
`CancellationEngine` (`09`) يفوّض لـ`IScheduledCancellationPolicy` عند `status = scheduled` (`stage = scheduled`):
```
minutesBefore = (scheduled_at − now) بالدقائق
if minutesBefore ≥ free_cancel_minutes_before → fee = 0, at_fault = none
else fee = late_cancel_fee (نفس أنواع الرسوم في 09)، at_fault = passenger، counts_toward_rate = true
compensation = (يوجد حجز فعّال) ? fee × late_cancel_driver_compensation_percent / 100 : 0
```
المعاينة `POST /passenger/trips/{id}/cancel/preview` تعيد `freeUntil = scheduled_at − free_cancel_minutes_before`. بعد خروج الرحلة من `scheduled` تُطبق `cancellation_rules` بـ`booking_type = scheduled` (أو NULL). الإلغاء يحرر الحجز (`trip_cancelled`، بلا نقاط للسائق) ويلغي التذكيرات ويشعر السائق المحجوز بـ`trip.cancelled`.

## F17.4 الـAPI

### الراكب
- GET `/passenger/scheduling/rules?rideCategoryId=` → `{ "maxDaysAhead": 7, "minLeadMinutes": 30, "minScheduledAt": "…", "maxScheduledAt": "…", "freeCancelMinutesBefore": 60, "lateCancelFee": 10.00 | null, "reminderOffsets": [1440, 60, 15] }`
- `POST /passenger/trips` بـ`bookingType: "scheduled"` → `201 Trip` بحالة `scheduled`؛ الأخطاء: `schedule_window_exceeded`, `schedule_lead_too_short`, `scheduled_limit_reached`.
- GET `/passenger/trips/scheduled` → `[ Trip ]` (المجدولة المفتوحة مرتبة بـ`scheduledAt`).
- `Trip` يضاف إليه:
```json
"scheduling": { "freeCancelUntil": "…", "searchStartsAt": "…",
                "reservation": { "status": "confirmed", "driverFirstName": "محمد", "driverPhotoUrl": "…", "ratingAvg": 4.9,
                                 "vehicle": { "make", "model", "color", "plateNumber" }, "reservedAt": "…" } | null } | null
```

### السائق
- GET `/driver/scheduled/marketplace?lat=&lng=&from=&to=&page=` → صفحة من
```json
{ "tripId": "uuid", "scheduledAt": "…", "rideCategory": { "code", "name" }, "pickupArea": "حي الملقا",
  "pickupApprox": { "lat": 24.812, "lng": 46.611 }, "dropoffArea": "مطار الملك خالد", "distanceToPickupKm": 8.4,
  "tripDistanceMeters": 32000, "estimatedFare": 95.00, "driverNetEarnings": 76.00, "isAirport": true,
  "isFavoriteRequest": false, "exclusiveUntil": null }
```
- POST `/driver/scheduled/{tripId}/reserve` → `201 Reservation`؛ الأخطاء: `reservation_taken`, `reservation_conflict`, `reservation_limit_reached`, `driver_not_approved`, `account_restricted`.
- GET `/driver/scheduled?status=active|history&page=` → `[ Reservation ]`
```json
{ "id": "uuid", "tripId": "uuid", "status": "confirmed", "source": "marketplace", "scheduledAt": "…",
  "pickup": { "name", "address", "lat", "lng" }, "dropoff": { … }, "passengerFirstName": "سارة",
  "estimatedFare": 95.00, "driverNetEarnings": 76.00, "confirmDeadline": "…" | null, "finalConfirmDeadline": "…" | null,
  "freeReleaseUntil": "…", "reservedAt": "…" }
```
  (الموقع الدقيق واسم الراكب الأول يظهران بعد الحجز فقط.)
- POST `/driver/scheduled/{tripId}/confirm` → `200 Reservation` (ينفذ التأكيد المستحق: الأول أو النهائي؛ النهائي يعيد أيضاً `trip: Trip`)؛ `409 reservation_not_confirmable`.
- POST `/driver/scheduled/{tripId}/release` `{ "reason"?: "…" }` → `200 Reservation` مع `penaltyPoints`.

### الإدارة (`scheduling.manage`)
- GET/POST `/admin/scheduled-ride-rules`، PUT/DELETE `/admin/scheduled-ride-rules/{id}` (كل الأعمدة بصيغة camelCase).
- GET `/admin/scheduled-trips?from=&to=&reservation=none|reserved|confirmed|assigned&cityId=&page=` → `{ tripId, tripNumber, scheduledAt, passengerName, categoryName, pickupName, dropoffName, reservationStatus, driverName, minutesToPickup, atRisk }` (`atRisk` = بلا تأكيد أول ضمن 90 دقيقة).
- POST `/admin/scheduled-trips/{tripId}/assign` `{ "driverId" }` (حجز يدوي `source=admin`، يتجاوز السوق ويخضع لقيود التداخل) · `/release-reservation` `{ "reason" }` (بلا نقاط).
- GET `/admin/scheduling/stats?from=&to=` → `{ booked, completed, cancelledByPassenger, cancelledLate, driverReleases, confirmationMissed, driverNoShows, rematched, scheduledCompletionRate, scheduledCancellationRate, avgReservationLeadHours }`.
- Audit: `scheduled_ride_rule.create|update|delete`, `scheduled_trip.assign`, `scheduled_trip.release`.

### أكواد الأخطاء (F17)
| الكود | HTTP | ar |
|---|---|---|
| `schedule_window_exceeded` | 422 | لا يمكن الجدولة لأكثر من 7 أيام من الآن |
| `schedule_lead_too_short` | 422 | يجب أن يكون الموعد بعد 30 دقيقة على الأقل |
| `scheduled_limit_reached` | 422 | وصلت للحد الأقصى من الرحلات المجدولة |
| `reservation_taken` | 409 | تم حجز هذه الرحلة من كابتن آخر |
| `reservation_conflict` | 409 | يتعارض الموعد مع رحلة محجوزة لديك |
| `reservation_limit_reached` | 422 | وصلت للحد الأقصى من الحجوزات |
| `reservation_not_confirmable` | 409 | لا يمكن التأكيد الآن |

## F17.5 مؤشرات الأداء
- Scheduled Ride Completion Rate = المجدولة المكتملة ÷ المجدولة التي حلّ موعدها (غير الملغاة من الراكب قبل النافذة المجانية).
- Scheduled Ride Cancellation Rate = المجدولة الملغاة (أي طرف، بما فيها `no_drivers`) ÷ المجدولة المحجوزة في الفترة.
- Driver Commitment Rate = الحجوزات `completed` ÷ الحجوزات (باستثناء `trip_cancelled`).

---

# F17 — المطار

## F17.6 الجداول

| جدول | الأعمدة |
|---|---|
| `airports` | id, city_id, code CHAR(3) UNIQUE (IATA، مثل `RUH`)، name_ar, name_en, lat, lng, geofence JSON (مضلع `[[lat,lng],…]`)، requires_pickup_zone BOOL (true)، default_free_waiting_minutes INT NULL (مثل 15)، default_waiting_per_minute DECIMAL(12,2) NULL, queue_enabled BOOL, is_active, created_at, updated_at |
| `airport_zones` | id, airport_id (FK)، kind (`terminal`/`pickup_zone`/`driver_waiting_area`)، code VARCHAR(30)، terminal_code VARCHAR(10) NULL (`T1`…`T5`)، name_ar, name_en, polygon JSON NULL (إلزامي لـ`driver_waiting_area`)، lat, lng (نقطة الالتقاط/المرجع)، instructions_ar VARCHAR(500) NULL, instructions_en NULL, free_waiting_minutes INT NULL, waiting_per_minute DECIMAL(12,2) NULL, sort_order, is_active, created_at, updated_at — UNIQUE(airport_id, code) |
| `airport_queue_entries` | id, airport_id, driver_id, ride_category_id, status (`waiting`/`offered`/`dispatched`/`left`/`removed`)، entered_at, last_seen_at, offered_trip_id NULL, left_at NULL, left_reason NULL (`exited_area`/`offline`/`trip_assigned`/`rejected_offer`/`admin_removed`)، created_at — INDEX(airport_id, status, entered_at) |

تعديلات `trips`: `airport_id NULL`, `airport_direction` NULL (`pickup`/`dropoff`)، `airport_zone_id NULL` (منطقة الالتقاط)، `terminal_code NULL`, `flight_number VARCHAR(8) NULL`, `waiting_policy JSON NULL` (`{ "freeMinutes": 15, "perMinute": 0.5 }` يُثبَّت عند الإنشاء ويتقدم على قاعدة التسعير في حساب الانتظار).

## F17.7 القواعد
- **الكشف**: عند التسعير والطلب، إن وقع الالتقاط داخل `geofence` مطار نشط → `airport_direction = pickup`؛ وإلا إن وقعت الوجهة داخله → `dropoff`.
- **الالتقاط من المطار**: `airportPickupZoneId` (منطقة `pickup_zone` في نفس المطار) إلزامي إن `requires_pickup_zone` → `422 airport_pickup_zone_required { airportId }`. إحداثيات الالتقاط تُستبدل بـ`lat/lng` المنطقة و`pickup_name` باسمها؛ `terminal_code` من المنطقة. `flightNumber` اختياري بصيغة `^[A-Z0-9]{2}[0-9]{1,4}[A-Z]?$` (يُطبَّع uppercase بلا مسافات) — يُخزَّن فقط للتكامل المستقبلي مع بيانات الرحلات الجوية.
- **التوصيل للمطار**: `airportTerminalCode` اختياري (من مناطق `terminal`).
- **الانتظار**: `waiting_policy.freeMinutes = zone.free_waiting_minutes ?? airport.default_free_waiting_minutes ?? قاعدة التسعير`، ونفس الترتيب لـ`perMinute`. يستخدمها حساب الانتظار (F8/F10) ومراحل `arrived`/`waiting` في F14.
- **فئة `airport`**: مسموحة فقط إن كان الالتقاط أو الوجهة في مطار → وإلا `422 airport_category_not_applicable`. باقي الفئات مسموحة للمطار أيضاً.
- **طابور السائقين** (`queue_enabled`):
  - الدخول التلقائي: عند `PUT /driver/location` لسائق متصل بلا رحلة داخل مضلع `driver_waiting_area` ولا مدخل فعّال له → مدخل `waiting` (`entered_at = now`). أو يدوياً `POST /driver/airport-queue/join` (يتحقق من الموقع).
  - `last_seen_at` يُحدَّث مع كل موقع داخل المنطقة. الخروج: خارج المنطقة أكثر من `Airport:QueueExitGraceSeconds` (180) أو غير متصل بنفس المدة → `left`.
  - المطابقة: لرحلة التقاطها داخل `geofence` المطار، المرشحون أولاً من الطابور بترتيب `entered_at` (FIFO) مع أهلية F9 للفئة، ويُعرض على كل واحد بالتتابع (بدون Score) بمهلة العرض العادية. الرفض/الانتهاء → حسب `Airport:RejectAction`: `move_to_back` (`entered_at = now`، افتراضي) أو `remove`. بعد `Airport:QueueMaxOffers` (5) محاولات أو طابور بلا مؤهلين → المطابقة العادية (F9). القبول → `dispatched`.
  - الموقع في الطابور = عدد المدخلات `waiting` الأقدم لنفس المطار ومجموعة الفئة + 1. `estimatedWaitMinutes = position × متوسط الفاصل بين عمليات الإرسال من الطابور خلال آخر ساعتين` (null إن لا بيانات).

## F17.8 الـAPI
- GET `/catalog/airports` → `[ { "id", "code": "RUH", "name": "مطار الملك خالد الدولي", "lat", "lng", "terminals": [ { "id", "code", "terminalCode": "T1", "name" } ], "pickupZones": [ { "id", "code", "terminalCode", "name", "lat", "lng", "instructions", "freeWaitingMinutes" } ] } ]`
- GET `/passenger/airports/resolve?lat=&lng=` → `{ "airport": { "id", "code", "name" }, "requiresPickupZone": true, "pickupZones": [ … ], "terminals": [ … ] } | null`
- `POST /pricing/quote` و`POST /passenger/trips` يقبلان `airportPickupZoneId`, `airportTerminalCode`, `flightNumber`. `Trip` يضاف إليه: `"airport": { "code": "RUH", "direction": "pickup", "zoneName": "منطقة الالتقاط 3", "terminalCode": "T1", "flightNumber": "SV1020", "freeWaitingMinutes": 15 } | null`؛ و`Offer` يضاف إليه `airport` بنفس الشكل (بدون رقم الرحلة الجوية).
- السائق: GET `/driver/airport-queue` → `{ "inQueue": true, "airport": { "id", "code", "name" }, "position": 7, "total": 23, "enteredAt": "…", "estimatedWaitMinutes": 25 }` أو `{ "inQueue": false, "eligibleAirport": {…} | null }`؛ POST `/driver/airport-queue/join` `{ "lat", "lng" }` → نفس الشكل (`422 not_in_airport_waiting_area`)؛ POST `/driver/airport-queue/leave` → `204`.
- الإدارة (`airport.manage`): GET/POST `/admin/airports`، GET/PUT/DELETE `/admin/airports/{id}`، GET/POST `/admin/airports/{id}/zones`، PUT/DELETE `/admin/airports/{id}/zones/{zoneId}`، GET `/admin/airports/{id}/queue` → `[ { entryId, position, driverName, categoryCode, enteredAt, lastSeenAt, status } ]`، DELETE `/admin/airports/{id}/queue/{entryId}` `{ reason }`. Audit: `airport.create|update|delete`, `airport_zone.create|update|delete`, `airport_queue.remove`.
- أكواد: `airport_pickup_zone_required` (422 "اختر منطقة الالتقاط في المطار")، `airport_category_not_applicable` (422 "فئة المطار متاحة لرحلات المطار فقط")، `not_in_airport_waiting_area` (422 "يجب أن تكون داخل منطقة انتظار المطار").
- SignalR (للسائق): `AirportQueueUpdated({ position, total, estimatedWaitMinutes })` عند تغيّر موقعه في الطابور.

## F17.9 المهام والإعدادات (F17)

| المهمة | الجدولة | العمل |
|---|---|---|
| `ScheduledRideWorker` | كل 30 ث | طلبات التأكيد (الأول/النهائي) المستحقة، انتهاء مهل التأكيد والتحرير، تحويل `scheduled → searching`، كشف عدم حضور السائق وReassign، انتهاء نافذة المفضل |
| `ScheduledReminderJob` | كل دقيقة | إرسال `scheduled_ride_reminders` المستحقة (`pending` و`send_at ≤ now`) عبر `scheduled.reminder` / `scheduled.confirm_request`؛ المتأخرة أكثر من 10 د عن موعدها → `skipped` |
| `AirportQueueJob` | كل 30 ث | إخراج المدخلات المنتهية (`last_seen_at` قديم) وبث المواقع |

| المفتاح | الافتراضي |
|---|---|
| `Airport:QueueExitGraceSeconds` / `Airport:RejectAction` / `Airport:QueueMaxOffers` | 180 / `move_to_back` / 5 |

(إعدادات الجدولة كلها في `scheduled_ride_rules`؛ `Trips:ScheduledLeadMinutes` يُهمل.)

---

# F18 — الدعم

## F18.1 الجداول

| جدول | الأعمدة |
|---|---|
| `help_categories` | id, code UNIQUE, name_ar, name_en, icon VARCHAR(40), audience (`passenger`/`driver`/`all`)، sort_order, is_active, created_at, updated_at |
| `help_articles` | id, category_id (FK)، slug VARCHAR(120) UNIQUE, title_ar, title_en, body_ar MEDIUMTEXT (Markdown)، body_en MEDIUMTEXT, audience (`passenger`/`driver`/`all`)، tags JSON NULL, sort_order, is_published BOOL, published_at NULL, view_count INT, helpful_yes INT, helpful_no INT, updated_by NULL, created_at, updated_at — FULLTEXT(title_ar, title_en, body_ar, body_en) في MySQL (البحث بـ`LIKE` في SQLite للاختبارات) |
| `support_sla_policies` | id, priority UNIQUE (`urgent`/`high`/`normal`/`low`)، first_response_minutes, resolution_minutes, updated_at |
| `support_tickets` | id, ticket_number (UNIQUE `ST-YYYYMMDD-#####`)، requester_user_id, requester_role (`passenger`/`driver`/`corporate_admin`)، type (`trip_issue`/`payment_issue`/`lost_item`/`safety`/`account`/`other`)، trip_id NULL, subject VARCHAR(160), status (`open`/`pending_user`/`in_progress`/`resolved`/`closed`)، priority (`urgent`/`high`/`normal`/`low`)، channel (`app`/`website`/`dashboard`/`phone`)، assigned_to_user_id NULL, assigned_at NULL, first_response_due_at, resolution_due_at, first_response_at NULL, sla_paused_at NULL, sla_paused_seconds INT DEFAULT 0, resolved_at NULL, closed_at NULL, last_message_at, last_message_by (`user`/`agent`/`system`)، unread_by_user INT DEFAULT 0, csat_score TINYINT NULL, csat_comment VARCHAR(500) NULL, safety_case_id NULL, lost_item_report_id NULL, created_at, updated_at — INDEX(status, priority, resolution_due_at)، INDEX(requester_user_id, created_at)، INDEX(assigned_to_user_id, status) |
| `support_messages` | id, ticket_id (FK)، author_user_id NULL, author_role (`user`/`agent`/`system`)، body VARCHAR(4000), is_internal BOOL DEFAULT false (ملاحظة داخلية للوكلاء)، created_at — INDEX(ticket_id, created_at) |
| `support_message_attachments` | id, message_id (FK)، file_id (stored_files)، created_at |
| `canned_responses` | id, code VARCHAR(40) UNIQUE, title VARCHAR(120), body_ar VARCHAR(4000), body_en VARCHAR(4000), ticket_type NULL, is_active, created_by, created_at, updated_at |
| `fare_disputes` | id, ticket_id (FK UNIQUE)، trip_id (UNIQUE — نزاع واحد لكل رحلة)، requester_user_id, reason (`overcharged`/`route_longer`/`waiting_charged`/`cancellation_fee`/`promo_not_applied`/`other`)، charged_amount, requested_refund_amount NULL, status (`open`/`under_review`/`approved`/`partially_approved`/`rejected`)، resolution NULL (`refund_full`/`refund_partial`/`no_refund`)، approved_refund_amount NULL, refund_id NULL (F11)، resolved_by NULL, resolved_at NULL, resolution_note VARCHAR(1000) NULL, created_at, updated_at |

## F18.2 القواعد
- **الأولوية الافتراضية حسب النوع**: `safety` → `urgent`، `payment_issue` → `high`، `trip_issue`/`lost_item` → `normal`، `account`/`other` → `normal`. الوكيل يعدّلها (تُعاد حسابات SLA).
- **SLA**: `first_response_due_at = created_at + first_response_minutes`؛ `resolution_due_at = created_at + resolution_minutes + sla_paused_seconds`. الإيقاف أثناء `pending_user` (`sla_paused_at`)، والاستئناف عند رد المستخدم يضيف المدة إلى `sla_paused_seconds`. `first_response_at` = أول رسالة وكيل غير داخلية.
- **الحالات**:

| الحدث | الانتقال |
|---|---|
| الإنشاء | `open` |
| تعيين وكيل أو أول رد وكيل | `in_progress` |
| الوكيل يطلب معلومات (`status=pending_user`) | `pending_user` |
| رد المستخدم على `pending_user`/`resolved` | `in_progress` (إن كان معيّناً) أو `open` |
| الوكيل يحل | `resolved` (+ طلب تقييم CSAT) |
| بعد `Support:AutoCloseDays` (3) من `resolved` بلا رد، أو الوكيل يغلق | `closed` (نهائي: الرد → `409 ticket_closed`، ويُنشأ تذكرة جديدة) |

- **الإنشاء**: `tripId` اختياري لكن إلزامي للأنواع `trip_issue`, `payment_issue`, `lost_item` ويجب أن يكون المستخدم طرفاً فيه. نوع `safety` → ينشئ أيضاً `safety_cases` (`type=safety_report`, `source=support`، `09`) ويربطها. نوع `lost_item` عبر هذا المسار → ينشئ `lost_item_reports` (`09`) للراكب. والعكس: تقارير المفقودات من `09` تنشئ تذكرة تلقائياً.
- **المرفقات**: رفع مسبق `POST /support/attachments` (jpg/png/pdf ≤ 10MB، نفس قيود F2) → `fileId`؛ حتى 5 مرفقات لكل رسالة (`422 attachment_limit`)؛ `stored_files.owner_user_id` = الرافع؛ `GET /files/{id}` مسموح للمالك والوكلاء.
- **نزاع الأجرة**: ضمن `payment_issue` مع كائن `dispute`: رحلة `completed` (أو ملغاة برسوم) خلال `Support:DisputeWindowDays` (14) وإلا `422 dispute_window_closed`؛ نزاع واحد لكل رحلة (`409 dispute_exists`). `charged_amount` = المدفوع (أو الرسوم). الحل (`support.disputes`): `refund_full` → استرداد `charged_amount`؛ `refund_partial` → `amount` (≤ `charged_amount`)؛ `no_refund`. الاسترداد يُنشأ عبر F11 (`reason_code = fare_dispute`، `dispute_id`، الوجهة `original_method` للبطاقة وإلا `wallet`) ويخضع لقاعدة الأربع عيون (`08`). الحالة `approved`/`partially_approved`/`rejected` + رسالة نظامية في التذكرة + إشعار `support.status`.
- **الإشعارات**: رسالة وكيل غير داخلية → `support.reply` (+ `unread_by_user += 1`)؛ `pending_user`/`resolved`/`closed` → `support.status`. رسالة المستخدم → SignalR للإدارة.
- **CSAT**: بعد `resolved`/`closed` يستطيع المستخدم التقييم مرة واحدة (1–5 + تعليق).

## F18.3 الـAPI

### عام (بدون مصادقة) — للموقع والتطبيق
- GET `/help/categories?audience=passenger|driver` → `[ { "id", "code", "name", "icon", "articlesCount" } ]`
- GET `/help/articles?categoryId=&q=&audience=&page=` → صفحة `{ "id", "slug", "title", "excerpt" (أول 160 حرفاً بلا Markdown), "categoryId", "updatedAt" }` (المنشورة فقط، باللغة حسب `Accept-Language`).
- GET `/help/articles/{slug}` → `{ "id", "slug", "title", "body" (Markdown), "category": { "id", "code", "name" }, "tags", "updatedAt", "related": [ { "slug", "title" } ] }` (يزيد `view_count`).
- POST `/help/articles/{id}/feedback` `{ "helpful": true }` → `204` (حد: مرة لكل IP/مقال/يوم).

### المستخدم `/support` (مصادق — راكب أو سائق)
- POST `/support/attachments` (multipart `file`) → `201 { "fileId", "fileName", "contentType", "sizeBytes" }`.
- POST `/support/tickets`
```json
{ "type": "payment_issue", "tripId": "uuid", "subject": "تم احتساب مبلغ أعلى", "message": "…", "fileIds": ["uuid"],
  "dispute": { "reason": "overcharged", "requestedRefundAmount": 12.00 } }
```
  → `201 TicketDetail`. الأخطاء: `validation_failed`, `dispute_exists`, `dispute_window_closed`, `attachment_limit`.
- GET `/support/tickets?status=open|closed&page=` → صفحة `TicketSummary` `{ "id", "ticketNumber", "type", "subject", "status", "tripNumber", "lastMessageAt", "unread": 1, "createdAt" }` (`status=open` تشمل كل غير `closed`).
- GET `/support/tickets/{id}` → `TicketDetail`
```json
{ "id", "ticketNumber", "type", "subject", "status", "priority", "trip": { "id", "tripNumber", "completedAt" } | null,
  "messages": [ { "id", "authorRole": "agent", "authorName": "فريق دعم ATA", "body", "attachments": [ { "fileId", "fileName", "contentType" } ], "createdAt" } ],
  "dispute": { "reason", "chargedAmount", "requestedRefundAmount", "status", "resolution", "approvedRefundAmount" } | null,
  "canReply": true, "canRate": false, "csatScore": null, "createdAt", "resolvedAt" }
```
  (الرسائل الداخلية لا تظهر؛ فتح التذكرة يصفّر `unread_by_user`.)
- POST `/support/tickets/{id}/messages` `{ "body", "fileIds"?: [] }` → `201 Message`؛ `409 ticket_closed`.
- POST `/support/tickets/{id}/csat` `{ "score": 5, "comment"? }` → `204`؛ `409 conflict` إن لم تُحل أو قُيّمت.

### الإدارة
| المسار | الصلاحية |
|---|---|
| GET `/admin/support/summary` → `{ "open", "unassigned", "pendingUser", "breachingFirstResponse", "breachingResolution", "avgFirstResponseMinutes", "avgResolutionHours", "csatAvg" }` | `support.view` |
| GET `/admin/support/tickets?status=&type=&priority=&assignedTo=me|unassigned|{userId}&sla=breached|due_soon&search=&from=&to=&page=` → `{ id, ticketNumber, type, subject, status, priority, requesterName, requesterRole, tripNumber, assignedToName, firstResponseDueAt, resolutionDueAt, slaState: "ok"|"due_soon"|"breached", lastMessageAt, lastMessageBy, createdAt }` (`due_soon` = خلال 30 دقيقة) | `support.view` |
| GET `/admin/support/tickets/{id}` → التفاصيل + الرسائل الداخلية + بيانات الطالب (الجوال الكامل) + ملخص الرحلة وإيصالها + الحالات المرتبطة | `support.view` |
| POST `/admin/support/tickets` `{ requesterUserId, type, tripId?, subject, message, priority?, channel: "phone" }` | `support.manage` |
| POST `/admin/support/tickets/{id}/messages` `{ body, fileIds?, isInternal, cannedResponseCode? }` | `support.manage` |
| POST `/admin/support/tickets/{id}/assign` `{ userId | null }` · `/status` `{ status: "pending_user|in_progress|resolved|closed", note? }` · `/priority` `{ priority }` · `/type` `{ type }` | `support.manage` |
| POST `/admin/support/disputes/{id}/resolve` `{ "resolution": "refund_full|refund_partial|no_refund", "amount"?: 10.00, "note" }` → النزاع + `refund` | `support.disputes` |
| GET `/admin/support/disputes?status=&page=` | `support.view` |
| GET/POST `/admin/canned-responses`، PUT/DELETE `/admin/canned-responses/{id}` (العناصر النائبة: `{userName}` `{ticketNumber}` `{tripNumber}`) | `support.manage` |
| GET/PUT `/admin/support/sla-policies` | `support.manage` |
| GET/POST `/admin/help/categories`، PUT/DELETE `/admin/help/categories/{id}`؛ GET/POST `/admin/help/articles`، GET/PUT/DELETE `/admin/help/articles/{id}`، POST `/admin/help/articles/{id}/publish` · `/unpublish` | `help.manage` |

Audit: `support_ticket.create|assign|status|priority|type`, `support_dispute.resolve`, `canned_response.create|update|delete`, `support_sla.update`, `help_category.*`, `help_article.create|update|delete|publish|unpublish`. (الرسائل نفسها لا تُكرر في audit.)

### أكواد الأخطاء (F18)
| الكود | HTTP | ar |
|---|---|---|
| `ticket_closed` | 409 | التذكرة مغلقة، أنشئ تذكرة جديدة |
| `dispute_exists` | 409 | يوجد اعتراض سابق على هذه الرحلة |
| `dispute_window_closed` | 422 | انتهت مدة الاعتراض على الأجرة |
| `attachment_limit` | 422 | الحد الأقصى 5 مرفقات |

### SignalR
- للمستخدم: `SupportTicketUpdated({ ticketId, status, lastMessageAt, unread })`.
- لمجموعة `admins`: `SupportTicketUpdated({ ticketId, status, priority, lastMessageBy })` و`SupportTicketCreated(ticketSummary)`.

## F18.4 المهام والإعدادات (F18)

| المهمة | الجدولة | العمل |
|---|---|---|
| `SupportAutoCloseJob` | كل ساعة | `resolved` أقدم من `AutoCloseDays` → `closed` |
| `SupportSlaMonitorJob` | كل 5 د | بث `SupportTicketUpdated` عند تحول `slaState` لتلوين الطابور |

| المفتاح | الافتراضي |
|---|---|
| `Support:AutoCloseDays` / `Support:DisputeWindowDays` | 3 / 14 |
| `Support:MaxAttachmentsPerMessage` | 5 |

## F18.5 مؤشرات الأداء
- Support Resolution Time = متوسط (والوسيط) `resolved_at − created_at − sla_paused_seconds` للتذاكر المحلولة في الفترة.
- First Response Time = متوسط `first_response_at − created_at`. SLA Compliance = نسبة المحلولة قبل `resolution_due_at`. CSAT = متوسط `csat_score`.

---

## Flutter

**feature `scheduled_rides`**:
- entities: `SchedulingRules`, `ScheduledTrip` (Trip + `scheduling`)، `MarketplaceTrip`, `Reservation`.
- usecases: `GetSchedulingRules`, `GetScheduledTrips`, `GetMarketplaceTrips`, `ReserveScheduledTrip`, `ConfirmReservation`, `ReleaseReservation`, `GetMyReservations`.
- cubits: `ScheduleTimeCubit` (منتقي التاريخ/الوقت بحدود `minScheduledAt`/`maxScheduledAt` من القواعد؛ يحل محل حالة "جدولة" المحلية في `HomeCubit`)، `ScheduledTripsCubit` (راكب)، `MarketplaceCubit` (سائق: القائمة + فلتر اليوم + تحديث كل 60 ث + الحجز)، `ReservationsCubit` (سائق: القائمة مع عدّادات مهل التأكيد، التأكيد، التحرير مع حوار يوضح النقاط إن كان متأخراً).
- pages: `/scheduled` (رحلاتي المجدولة، راكب)، `/scheduled/:tripId` (التفاصيل: الموعد، السائق المحجوز، "إلغاء مجاني حتى …"، الإلغاء عبر `CancelFlowCubit`)، `/driver/scheduled` (تبويبان: السوق / حجوزاتي)، `/driver/scheduled/:tripId`. إشعار `scheduled.confirm_request` يفتح التفاصيل بزر "تأكيد" بارز. `RidesCubit` يعرض تبويب "مجدولة".

**feature `airport`**:
- entities: `Airport`, `AirportZone`, `AirportQueueStatus`؛ usecases: `ResolveAirport`, `GetAirports`, `GetAirportQueue`, `JoinAirportQueue`, `LeaveAirportQueue`.
- cubits: `AirportPickupCubit` (عند تغيّر الالتقاط يستدعي `resolve`؛ إن كان مطاراً يعرض ورقة اختيار منطقة الالتقاط مع التعليمات وحقل رقم الرحلة الجوية؛ يزوّد `HomeCubit` بـ`airportPickupZoneId`/`flightNumber`)، `AirportQueueCubit` (سائق: الموقع في الطابور، تحديث من `AirportQueueUpdated` + استطلاع 30 ث).
- widgets/pages: `AirportZoneSheet`، بطاقة "طابور المطار" في نظرة السائق عند وجود مطار قريب، `/driver/airport-queue`.

**feature `support`**:
- entities: `HelpCategory`, `HelpArticle`, `TicketSummary`, `TicketDetail`, `TicketMessage`, `FareDispute`.
- usecases: `GetHelpCategories`, `SearchHelpArticles`, `GetHelpArticle`, `SendArticleFeedback`, `UploadSupportAttachment`, `CreateTicket`, `GetTickets`, `GetTicket`, `ReplyToTicket`, `RateTicket`, `WatchTicket` (Hub + استطلاع 15 ث أثناء فتح التذكرة).
- cubits: `HelpCenterCubit` (الفئات + البحث بـdebounce)، `HelpArticleCubit` (العرض بـ`flutter_markdown` + مفيد/غير مفيد)، `TicketsCubit`، `NewTicketCubit` (النوع، اختيار الرحلة من آخر الرحلات، الموضوع، الرسالة، المرفقات عبر `image_picker`/`file_picker`، حقول النزاع لنوع `payment_issue`)، `TicketDetailCubit` (الرسائل، الرد، المرفقات)، `CsatCubit`.
- pages: `/support` (مركز المساعدة + "تذاكري" + "تواصل معنا")، `/support/articles/:slug`، `/support/tickets`، `/support/tickets/new?type=&tripId=`، `/support/tickets/:id`. المداخل: "اتصل بنا"/"المساعدة والدعم" في حسابي، زر "مشكلة في الرحلة؟" في الإيصال وتفاصيل الرحلة (يفتح `new?type=trip_issue&tripId=`)، و"اعتراض على الأجرة" (`type=payment_issue`). المسارات مشتركة للراكب والسائق.

## الموقع — مركز المساعدة

| المسار | الصفحة |
|---|---|
| `/help` | بحث + فئات (تبويب راكب/سائق عبر `audience`) + أكثر المقالات قراءة؛ رابط في الهيدر والفوتر |
| `/help/:slug` | المقال (Markdown → HTML عبر `react-markdown` مع `rehype-sanitize`)، الفئة، مقالات ذات صلة، "هل كان المقال مفيداً؟"، ودعوة "لم تجد إجابتك؟ افتح تذكرة من تطبيق ATA" مع روابط المتاجر |

SEO: `<title>` ووصف لكل مقال؛ صفحات المساعدة قابلة للفهرسة (بخلاف `/t/:token`).

## لوحة الإدارة

| المسار | الصفحة |
|---|---|
| `/scheduled` | الرحلات المجدولة القادمة (جدول زمني/جدول) مع حالة الحجز ومؤشر الخطر، إسناد يدوي، تحرير؛ بطاقات KPIs من `/admin/scheduling/stats` |
| `/scheduled/rules` | قواعد الجدولة (عامة/مدينة/فئة) بنموذج مجمّع (النافذة، السوق، التأكيدات، التذكيرات، الإلغاء، العقوبات) |
| `/airports` و`/airports/:id` | المطارات: خريطة `geofence` + المناطق (الصالات، مناطق الالتقاط كنقاط، مناطق انتظار السائقين كمضلعات عبر `PolygonEditor`)، سياسة الانتظار، الطابور الحي مع الإزالة |
| `/support` | طابور التذاكر: تبويبات (لي، غير معيّنة، الكل، بانتظار المستخدم)، ألوان SLA، فلاتر، بطاقات الملخص |
| `/support/tickets/:id` | المحادثة (رسائل عامة وملاحظات داخلية بلون مختلف)، إدراج رد جاهز، المرفقات، لوحة جانبية: الطالب، الرحلة والإيصال، الحالة/الأولوية/النوع/التعيين، النزاع مع نموذج الحل، الحالات المرتبطة |
| `/support/disputes` | النزاعات المفتوحة |
| `/support/canned-responses` · `/support/sla` | الردود الجاهزة، سياسات SLA |
| `/help-center` | إدارة الفئات والمقالات (محرر Markdown ثنائي اللغة مع معاينة، نشر/إلغاء نشر) |

## البيانات الأولية (Seed)
- `scheduled_ride_rules`: قاعدة عامة واحدة بالقيم الافتراضية (§F17.2).
- المطار: `RUH` مطار الملك خالد الدولي (24.9576, 46.6988) بمضلع تقريبي، الصالات `T1`–`T5`، منطقتا التقاط لكل صالة (مثل `T1-P1` "منطقة الالتقاط 1 - صالة 1")، منطقة انتظار سائقين واحدة، `default_free_waiting_minutes = 15`، `queue_enabled = true`. (الإحداثيات تقريبية للتطوير وتُراجع قبل الإنتاج.)
- `support_sla_policies`: urgent 15/240، high 60/1440، normal 240/2880، low 1440/4320 (دقائق: أول رد / حل).
- `help_categories`: `trips` (الرحلات)، `payments` (الدفع والمحفظة)، `safety` (السلامة)، `account` (الحساب)، `drivers` (للكباتن، `audience=driver`)؛ مع 2–3 مقالات لكل فئة (مثل "كيف أجدول رحلة؟"، "رسوم الإلغاء"، "كيف أشارك رحلتي؟"، "كيف أسحب أرباحي؟").
- `canned_responses`: `greeting`, `need_more_info`, `refund_approved`, `refund_rejected`, `lost_item_contacted`, `closing`.

## سيناريوهات الاختبار (الخلفية)
الجدولة:
1. `scheduledAt` = الآن + 7 أيام + دقيقة → `422 schedule_window_exceeded` مع `maxScheduledAt`؛ الآن + 7 أيام تماماً → مقبول؛ الآن + 20 د → `422 schedule_lead_too_short`.
2. الرحلة المجدولة لا تمنع طلب رحلة فورية ولا تظهر في `/active`؛ الرابعة المفتوحة → `422 scheduled_limit_reached`.
3. السوق يستبعد الفئة غير المتوافقة، البعيد، المقيّد، والرحلة المحجوزة؛ نافذة المفضل تُظهرها للمفضل فقط.
4. حجزان متزامنان → واحد ينجح (`409 reservation_taken` للآخر)؛ التداخل الزمني → `409 reservation_conflict`؛ الحد 5.
5. الخط الزمني بساعة مزيّفة: طلب التأكيد عند T−60، عدم التأكيد → تحرير بنقاط وعودة للسوق؛ التأكيد النهائي → `driver_assigned`؛ عدم التأكيد النهائي → `searching` + `scheduled.rematched`؛ بلا حجز عند T−10 → `searching`.
6. التأكيد النهائي والسائق غير متصل → `409 reservation_not_confirmable { reason: "offline" }`.
7. عدم حضور السائق حتى T+10 → Reassign إلى `searching` + `no_show_count` + نقاط، دون إنشاء `cancellation_events`.
8. إلغاء الراكب قبل 61 د → مجاني؛ قبل 30 د مع حجز → 10 ر.س + تعويض 5 للسائق المحجوز؛ التذكيرات تُلغى.
9. التذكيرات: تُنشأ فقط للإزاحات المستقبلية، تُرسل مرة واحدة، وتُلغى عند الإلغاء/التحرير.
10. التسعير المجدول يستخدم مضاعف الوقت عند الموعد والطلب `normal` حتى لو كان الطلب الحالي `high`.
11. المفضل يحجز الرحلة المجدولة ويُسنَد نهائياً → خصم المفضل عند الإكمال.

المطار:
12. التقاط داخل المطار بلا منطقة → `422 airport_pickup_zone_required`؛ مع منطقة → الإحداثيات مستبدلة و`terminal_code` مضبوط؛ رقم رحلة جوية غير صالح → `422 validation_failed`.
13. فئة `airport` لرحلة داخل المدينة → `422 airport_category_not_applicable`.
14. سياسة الانتظار من المنطقة تتقدم على قاعدة التسعير في حساب الانتظار ومراحل الإلغاء.
15. الطابور: الدخول التلقائي بالموقع، الخروج بعد مهلة الغياب، العرض FIFO، الرفض ينقل للخلف، بعد 5 رفضات → المطابقة العادية.

الدعم:
16. تذكرة `trip_issue` بلا رحلة أو لرحلة ليست له → `422`/`403`؛ `safety` تنشئ حالة سلامة مرتبطة.
17. حسابات SLA: `first_response_due_at` حسب الأولوية؛ الإيقاف في `pending_user` يمدد `resolution_due_at` بمدة الإيقاف.
18. دورة الحالات كاملة؛ الرد على `closed` → `409 ticket_closed`؛ الإغلاق التلقائي بعد 3 أيام.
19. الرسائل الداخلية لا تظهر للمستخدم؛ رد الوكيل يرسل `support.reply` ويزيد `unread`.
20. النزاع: بعد 15 يوماً → `422 dispute_window_closed`؛ نزاع ثانٍ → `409`؛ `refund_partial` ينشئ استرداد F11 بالمبلغ والوجهة الصحيحة ويخضع للأربع عيون فوق الحد.
21. المرفقات: 6 → `422 attachment_limit`؛ نوع غير مدعوم → `422 unsupported_file_type`؛ الوكيل يستطيع قراءة المرفق.
22. مقالات المساعدة: غير المنشورة لا تظهر في `/help/*`؛ البحث يعيد المطابقات باللغة المطلوبة.

## قرارات تحتاج تأكيد مالك المنتج
1. حالة رحلة جديدة `scheduled`، والرحلة المجدولة لا تمنع طلب رحلة فورية (حتى 3 مجدولة مفتوحة).
2. نافذة الـ7 أيام زمنية دقيقة من لحظة الحجز (وليست حتى نهاية اليوم السابع).
3. الرحلات المجدولة تُسعَّر بطلب `normal` ثابت (بدون تسعير ذروة).
4. إلغاء السائق أو عدم حضوره في رحلة مجدولة يعيد المطابقة بدل إلغاء رحلة الراكب.
5. بدء البحث العادي عند T−10 دقائق إن لم يوجد سائق مؤكد، والتأكيدان عند T−60 وT−15.
6. تفويض البطاقة للرحلة المجدولة عند بدء البحث/التأكيد النهائي وليس عند الحجز.
7. رقم الرحلة الجوية يُخزَّن فقط (لا تكامل مع بيانات الطيران في v1)، وسياسة الانتظار الافتراضية للمطار 15 دقيقة مجانية.
8. إيقاف ساعة SLA أثناء انتظار رد المستخدم.
9. مركز المساعدة على الموقع للقراءة فقط؛ فتح التذاكر من التطبيق.
