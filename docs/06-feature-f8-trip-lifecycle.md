# F8 — دورة حياة الرحلة (Trip Lifecycle)

تُنفَّذ كاملة عبر: MySQL → API (وحدة `Trips`) → Flutter (feature `trip` للراكب والسائق) → لوحة الإدارة (الرحلات + Live Map). المطابقة (F9) والتسعير (F10) يُستبدلان بمكوّنين مبدئيين قابلين للاستبدال: `SimpleMatcher` (أقرب سائق متصل من نفس الفئة) و`FlatPricing` (سعر تقديري من الفئة).

## حالات الرحلة

`requested → searching → driver_assigned → driver_en_route → driver_arrived → waiting → pin_verified → in_trip → completed | cancelled | no_drivers`

| الانتقال | من يقوم به | الشرط |
|---|---|---|
| requested → searching | النظام | بعد إنشاء الرحلة مباشرة |
| searching → driver_assigned | النظام (قبول السائق للعرض) | عرض نشط غير منتهٍ |
| searching → no_drivers | النظام | انتهاء المهلة الكلية (Matching:SearchTimeoutSeconds = 120) |
| driver_assigned → driver_en_route | السائق | — |
| driver_en_route → driver_arrived | السائق | مسافة ≤ 300م من نقطة الالتقاط (يُسجَّل تحذير فقط إن تجاوزت) |
| driver_arrived → waiting | النظام | فوراً؛ يبدأ عدّاد الانتظار المجاني (Trips:FreeWaitingMinutes = 3) |
| waiting → pin_verified | السائق | إدخال PIN الراكب (4 أرقام) صحيح؛ 5 محاولات |
| pin_verified → in_trip | السائق | "بدء الرحلة" |
| in_trip → completed | السائق | "إنهاء الرحلة"؛ يُحسب الأجرة النهائية وتُنشأ حركة المحفظة/الدفع (نقدي: تسجيل فقط) |
| أي حالة قبل in_trip → cancelled | الراكب أو السائق أو الإدارة | يُسجَّل `cancellation_events` (الرسوم تُفعّل في F14) |

كل انتقال يُسجَّل في `trip_events` (النوع، الطرف، الوقت، الإحداثيات، بيانات إضافية JSON).

## الجداول (MySQL)

| جدول | الأعمدة |
|---|---|
| `trips` | id, trip_number (UNIQUE `T-YYYYMMDD-#####`), passenger_id, driver_id NULL, vehicle_id NULL, ride_category_id, status, booking_type (`now`/`scheduled`), scheduled_at NULL, pickup_name, pickup_address, pickup_lat, pickup_lng, dropoff_name, dropoff_address, dropoff_lat, dropoff_lng, prefer_female_driver, payment_method (`cash`/`wallet`/`card`), pricing_mode (`fixed`/`saver`/`offer`), offered_price NULL, estimated_distance_m, estimated_duration_s, estimated_fare, final_distance_m, final_duration_s, final_fare, waiting_seconds, pin_code_hash, pin_attempts, cancelled_by (`passenger`/`driver`/`system`/`admin`) NULL, cancellation_reason NULL, rider_note, requested_at, assigned_at, arrived_at, started_at, completed_at, cancelled_at, created_at, updated_at — INDEX(passenger_id, created_at), INDEX(driver_id, created_at), INDEX(status) |
| `trip_stops` | id, trip_id, sequence TINYINT, name, address, lat, lng, arrived_at NULL — UNIQUE(trip_id, sequence) |
| `trip_offers` | id, trip_id, driver_id, status (`sent`/`accepted`/`rejected`/`expired`), driver_net_earnings, distance_to_pickup_m, eta_seconds, sent_at, responded_at, expires_at — INDEX(driver_id, status) |
| `trip_events` | id, trip_id, type, actor (`passenger`/`driver`/`system`/`admin`), actor_user_id NULL, lat NULL, lng NULL, data JSON, created_at — INDEX(trip_id, created_at) |
| `driver_locations` | driver_id (PK), lat, lng, heading, speed, accuracy, is_online, current_trip_id NULL, updated_at — INDEX(is_online, updated_at) |
| `driver_location_history` | id, driver_id, trip_id NULL, lat, lng, recorded_at — INDEX(trip_id, recorded_at) (تُقلَّم وفق سياسة الاحتفاظ) |

`drivers` تضاف إليها: `current_trip_id`, `gender` (موجود)، و`acceptance_count`, `rejection_count` (للـKPIs).

## الـAPI

### الراكب `/passenger/trips`
- POST `/passenger/trips/estimate` `{ pickup:{lat,lng,name,address}, dropoff:{…}, stops:[{…}], rideCategoryId, bookingType, scheduledAt? }` → `{ distanceMeters, durationSeconds, categories:[{ rideCategoryId, code, name, etaMinutes, estimatedFare, driverNetEarnings }] }`
- POST `/passenger/trips` `{ …estimate fields…, rideCategoryId, paymentMethod, preferFemaleDriver, pricingMode:"fixed"|"offer", offeredPrice?, riderNote? }` → `201 Trip` (status `searching`); `409` إن كان لدى الراكب رحلة نشطة.
- GET `/passenger/trips/active` → `Trip | null`
- GET `/passenger/trips/{id}` → `Trip` (مع `driver`, `vehicle`, `stops`, `events`, `pin` عند `driver_assigned+`)
- POST `/passenger/trips/{id}/cancel` `{ reasonCode, note? }` → `Trip`
- GET `/passenger/trips?status=` (القائمة، موجودة من F1 وتُصبح فعلية)

### السائق `/driver`
- PUT `/driver/location` `{ lat, lng, heading?, speed?, accuracy? }` → `204` (كل 3–5 ثوانٍ عند الاتصال؛ يُحدّث `driver_locations` ويُبث للراكب عبر SignalR أثناء الرحلة)
- GET `/driver/offers/active` → `Offer | null` `{ id, tripId, pickup, dropoff, stops, distanceToPickupMeters, etaSeconds, tripDistanceMeters, passengerPrice, driverNetEarnings, expiresAt, passenger:{ firstName, ratingAvg } }`
- POST `/driver/offers/{id}/accept` → `Trip` ؛ `409 offer_expired`
- POST `/driver/offers/{id}/reject` `{ reasonCode? }` → `204`
- GET `/driver/trips/active` → `Trip | null`
- POST `/driver/trips/{id}/en-route` | `/arrived` | `/verify-pin` `{ pin }` | `/start` | `/complete` `{ finalLat, finalLng }` | `/cancel` `{ reasonCode, note? }` → `Trip`
- GET `/driver/trips?status=` القائمة الفعلية

### الإدارة `/admin`
- GET `/admin/trips?status=&from=&to=&search=&page=` ، GET `/admin/trips/{id}` (مع الأحداث والمسار)
- POST `/admin/trips/{id}/cancel` `{ reason }`
- GET `/admin/live` → `{ drivers:[{ driverId, name, lat, lng, isOnline, status:"idle"|"on_trip", categoryCode }], activeTrips:[{ id, status, pickup, dropoff, driverId }], searchingTrips:[…] }`

### SignalR Hub `/hubs/trips` (JWT عبر `access_token`)
أحداث للراكب: `TripUpdated(trip)`, `DriverLocation({tripId, lat, lng, heading, etaSeconds})`. للسائق: `OfferReceived(offer)`, `OfferExpired(offerId)`, `TripUpdated(trip)`. للإدارة (مجموعة `admins`): `LiveSnapshot` كل 5 ثوانٍ.

### كائن `Trip` (الاستجابة)
```json
{ "id","tripNumber","status","bookingType","scheduledAt","rideCategory":{"id","code","name"},
  "pickup":{"name","address","lat","lng"},"dropoff":{…},"stops":[…],
  "paymentMethod","pricingMode","offeredPrice","estimatedFare","finalFare","estimatedDistanceMeters","estimatedDurationSeconds",
  "driver":{"id","fullName","ratingAvg","photoFileId","phoneMasked","gender"}|null,
  "vehicle":{"make","model","color","plateNumber"}|null,
  "pin":"4821"|null, "waitingSeconds", "cancelledBy","cancellationReason",
  "timeline":{"requestedAt","assignedAt","arrivedAt","startedAt","completedAt","cancelledAt"},
  "events":[{"type","actor","createdAt"}] }
```

## المطابقة المبدئية `SimpleMatcher` (تُستبدل في F9)
1. السائقون المتصلون، المعتمدون، بدون رحلة حالية، فئة مركبتهم = الفئة المطلوبة (أو أعلى إن `Matching:AllowUpgrade`)، جنس السائق أنثى إن `preferFemaleDriver`.
2. ضمن نصف قطر `Matching:RadiusMeters` (5000) مرتّبين بالمسافة (Haversine).
3. عرض لكل سائق بالتتابع بمهلة `Matching:OfferTimeoutSeconds` (20). رفض أو انتهاء → التالي. لا أحد → `no_drivers`.
4. يعمل كـ `BackgroundService` يفحص الرحلات `searching` كل ثانيتين (Redis Lock لاحقاً).

## التسعير المبدئي `FlatPricing` (يُستبدل في F10)
`fare = baseFare + perKm × km + perMin × min + bookingFee`، من أعمدة تُضاف إلى `ride_categories`: `base_fare, per_km, per_minute, booking_fee, min_fare, driver_share_percent` (افتراضي 80%). المسافة/الزمن بـ Haversine × 1.3 وسرعة 30 كم/س حتى تكامل Maps Provider.

## Flutter — feature `trip`
- `data/`: `TripRemoteDataSource` (REST) + `TripRealtimeDataSource` (signalr_netcore) ; `domain/`: `Trip`, `Offer` entities, usecases (`EstimateTrip`, `RequestTrip`, `CancelTrip`, `WatchActiveTrip`, `AcceptOffer`, `RejectOffer`, `AdvanceTrip`, `VerifyPin`, `SendLocation`).
- `presentation/cubit/`: `TripRequestCubit` (يحل محل الطلب المحلي في `passenger_home`)، `ActiveTripCubit` (الراكب: searching → assigned → … مع بطاقة السائق وETA وPIN ومشاركة)، `DriverOfferCubit` (شاشة العرض مع عدّاد 20 ث)، `DriverTripCubit` (الأزرار حسب الحالة)، `LocationStreamCubit` (geolocator → PUT /driver/location).
- لا `setState`؛ الخريطة `google_maps_flutter` مع مفاتيح عبر `--dart-define`.

## لوحة الإدارة
- صفحة الرحلات (فلاتر، جدول، تفاصيل مع الجدول الزمني والأحداث)، صفحة Live Map (Leaflet + OpenStreetMap مع تحديث من `/admin/live` أو SignalR).

## ملاحظات التنفيذ الفعلي (الخلفية — مُسلَّم)

- أعمدة إضافية: `trips.pin_code_protected` (رمز PIN محمي بـ Data Protection ليُعاد للراكب) و`trips.driver_earnings` (يُثبَّت عند الإكمال). `drivers.current_trip_id` بدون FK لتجنب دورة مفاتيح.
- أكواد أخطاء: `trip_active_exists` (409)، `offer_expired` (409)، `pin_invalid` (400 مع `attemptsLeft`)، `pin_locked` (429).
- الاستعلامات `/…/active` تعيد `200` مع `null` عند عدم وجود عنصر نشط.
- الدفع عند الإكمال: محفظة → قيد `trip_payment` للراكب و`trip_earning` للسائق مقابل `trip_revenue`؛ نقدي → `trip_earning` مقابل `cash_collected`. رصيد غير كافٍ أو بطاقة (لا بوابة بعد) → تحويل تلقائي إلى نقدي مع حدث `payment_fallback_cash`.
- إعدادات: `Trips:FreeWaitingMinutes`, `Trips:ScheduledLeadMinutes`, `Matching:{Enabled,RadiusMeters,OfferTimeoutSeconds,SearchTimeoutSeconds,AllowUpgrade,LocationMaxAgeSeconds,PollIntervalSeconds}`, `Realtime:LiveSnapshotEnabled`.
- CORS يسمح بـ credentials لعمل SignalR من لوحة الإدارة.
