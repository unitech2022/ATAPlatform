# F9 المطابقة + F10 التسعير والمناطق والطلب

يستبدلان `SimpleMatcher` و`FlatPricing` من F8 بمحركين قائمين على قواعد قابلة للإدارة (بدون AI). الطبقات: MySQL → API (وحدتا `Matching` و`Pricing`) → Flutter (تقدير السعر، اقتراح السعر، مؤشر الطلب) → لوحة الإدارة (المناطق، قواعد التسعير، مستويات الطلب، إعدادات المطابقة).

## F10 — المناطق Zones

| جدول | الأعمدة |
|---|---|
| `zones` | id, city_id, code (UNIQUE), name_ar, name_en, polygon JSON (`[[lat,lng],…]` مغلق)، center_lat, center_lng, priority INT (الأعلى يفوز عند التداخل)، is_active, operating_hours JSON (`[{day:0-6, from:"06:00", to:"23:59"}]` أو null = دائماً)، created_at, updated_at |
| `zone_category_settings` | id, zone_id, ride_category_id, is_enabled, surge_cap DECIMAL(4,2) (افتراضي 2.5) — UNIQUE(zone_id, ride_category_id) |

نقطة بلا منطقة → المنطقة الافتراضية للمدينة (`city_default`). خوارزمية Point-in-Polygon (ray casting) في `ZoneResolver` مع كاش في الذاكرة يُبطَل عند التعديل.

## F10 — قواعد التسعير Pricing Rules

| جدول | الأعمدة |
|---|---|
| `pricing_rules` | id, ride_category_id, zone_id NULL (null = المدينة كلها)، name, base_fare, per_km, per_minute, booking_fee, service_fee_percent, min_fare, waiting_per_minute, free_waiting_minutes, cancellation_fee (يُستخدم في F14)، driver_share_percent, effective_from DATETIME, effective_to NULL, priority INT, is_active — INDEX(ride_category_id, zone_id, is_active) |
| `pricing_time_multipliers` | id, pricing_rule_id, day_of_week TINYINT NULL (null = كل الأيام)، from_time TIME, to_time TIME, multiplier DECIMAL(4,2), label (`night`, `peak_morning`…) |
| `demand_levels` | id, code (`normal`/`moderate`/`high`/`very_high`, UNIQUE), name_ar, name_en, multiplier DECIMAL(4,2) (1.00/1.20/1.50/1.90 افتراضي)، color, sort_order |
| `demand_rules` | id, zone_id NULL, ride_category_id NULL, metric (`requests_per_driver`)، window_minutes (10)، threshold_moderate, threshold_high, threshold_very_high, is_active |
| `demand_overrides` | id, zone_id, ride_category_id NULL, demand_level_id, reason, starts_at, ends_at, created_by — تفعيل يدوي من الإدارة |
| `demand_snapshots` | id, zone_id, ride_category_id NULL, computed_at, requests_count, online_drivers, ratio, demand_level_code — INDEX(zone_id, computed_at) |
| `fare_quotes` | id, passenger_id, ride_category_id, pickup_zone_id, dropoff_zone_id, distance_m, duration_s, breakdown JSON, demand_level_code, total, offer_min, offer_max, expires_at (5 دقائق)، used_trip_id NULL, created_at |

### المعادلة
```
subtotal  = base_fare + per_km × km + per_minute × min
subtotal  = max(subtotal, min_fare)
timeMult  = أعلى multiplier منطبق من pricing_time_multipliers (أو 1.0)
demand    = multiplier مستوى الطلب الحالي للمنطقة/الفئة (override > snapshot > normal)، مقيّد بـ surge_cap
fare      = subtotal × timeMult × demand + booking_fee
fare     += fare × service_fee_percent / 100
fare     -= discount (F15/F16)
total     = round(fare, 0.5 ر.س)        // تقريب لأقرب نصف ريال
driverNet = (subtotal × timeMult × demand) × driver_share_percent / 100
```
اختيار القاعدة: القاعدة النشطة والسارية بأعلى `priority` للفئة ومنطقة الالتقاط، ثم قاعدة المدينة (zone_id null).

### Offer Your Price (F10)
`offer_min = total × Pricing:OfferMinPercent (70%)`، `offer_max = total × Pricing:OfferMaxPercent (130%)`. الراكب يرسل `offeredPrice` ضمن النطاق، وإلا `422 offer_out_of_range` مع الحدين. يظهر للسائق السعر وصافي أرباحه (`offeredPrice × driver_share_percent`).

### الطلب Demand
خدمة خلفية كل دقيقة: لكل منطقة (وفئة إن وُجدت قاعدة) تحسب `requests_count` (رحلات `requested/searching` خلال النافذة) ÷ `online_drivers` المتاحين → تحدد المستوى بالعتبات → تحفظ `demand_snapshots` وتُبث `DemandChanged` للإدارة. الكاش في الذاكرة للقراءة.

### API التسعير
- POST `/pricing/quote` (راكب) `{ pickup:{lat,lng}, dropoff:{lat,lng}, stops:[…], rideCategoryId?, bookingType, scheduledAt? }` → `{ quoteId, expiresAt, distanceMeters, durationSeconds, pickupZone:{id,name}, demand:{code,name,multiplier,color}, categories:[{ rideCategoryId, code, name, etaMinutes, total, driverNetEarnings, offerMin, offerMax, breakdown:{ baseFare, distanceFare, timeFare, minFareApplied, timeMultiplier, demandMultiplier, bookingFee, serviceFee, discount } }] }` — يحل محل `/passenger/trips/estimate` (يبقى كـ alias).
- POST `/passenger/trips` يقبل `quoteId` (اختياري؛ إن وُجد يُثبَّت السعر من العرض ما لم ينتهِ).
- GET `/pricing/demand?lat=&lng=` (راكب/سائق) → مستوى الطلب الحالي للموقع (لعرض مؤشر "الطلب مرتفع الآن" في التطبيق).
- الإدارة: CRUD `/admin/zones` (مع `zoneCategorySettings`), `/admin/pricing-rules` (+ `timeMultipliers`), `/admin/demand-levels` (تعديل المضاعِفات فقط)، `/admin/demand-rules`, `/admin/demand-overrides`, GET `/admin/demand/current` (لكل منطقة)، POST `/admin/pricing/simulate` (نفس مدخلات quote + `at` لتجربة القواعد). كل تعديل → audit_logs.

## F9 — محرك المطابقة

| جدول | الأعمدة |
|---|---|
| `matching_settings` | id, zone_id NULL, ride_category_id NULL, radius_meters (5000), max_radius_meters (12000), radius_step_meters (2500), offer_timeout_seconds (20), search_timeout_seconds (120), max_candidates (8), weights JSON (`{distance:0.35, eta:0.20, rating:0.15, acceptance:0.10, cancellation:0.10, tier:0.05, favorite:0.05}`)، allow_category_upgrade BOOL, prefer_favorite_driver BOOL, is_active — UNIQUE(zone_id, ride_category_id) |
| `matching_attempts` | id, trip_id, round INT, radius_meters, candidates_count, started_at, finished_at, outcome (`assigned`/`exhausted`/`timeout`/`cancelled`) |
| `matching_candidates` | id, attempt_id, driver_id, distance_m, eta_s, score DECIMAL(6,4), rank INT, offered BOOL, response (`accepted`/`rejected`/`expired`/null) — INDEX(attempt_id, rank) |

### خط الأنابيب
1. **البحث الجغرافي**: صندوق حدود ثم Haversine ضمن `radius_meters`، يتوسع بـ `radius_step` حتى `max_radius` عند نفاد المرشحين.
2. **الأهلية**: متصل، معتمد، لا رحلة حالية، موقع حديث (≤ `Matching:LocationMaxAgeSeconds`)، فئة المركبة مطابقة أو أعلى إن مسموح، الجنس إن طُلبت سائقة، المنطقة تسمح بالفئة، وثائق غير منتهية، السائق غير محظور مؤقتاً (F14)، لم يرفض نفس الرحلة سابقاً.
3. **Score** (0..1، الأعلى أفضل): `Σ weight_i × norm_i` حيث `norm_distance = 1 − d/max_radius`, `norm_eta = 1 − eta/900`, `norm_rating = (rating−3)/2`, `norm_acceptance = acceptance_rate`, `norm_cancellation = 1 − cancellation_rate`, `norm_tier = {bronze:.25,silver:.5,gold:.75,platinum:1}`, `norm_favorite = 1 إن كان مفضلاً (F16) وإلا 0`. معدلات القبول/الإلغاء من آخر 30 يوماً (`drivers.acceptance_count/rejection_count` + إلغاءات).
4. **الترتيب والعرض**: أفضل `max_candidates`، عرض تسلسلي بمهلة `offer_timeout_seconds`؛ عند الرفض/الانتهاء → التالي؛ عند نفاد الجولة → جولة جديدة بنصف قطر أوسع حتى `search_timeout_seconds` → `no_drivers`.
5. **السجل**: كل جولة في `matching_attempts` والمرشحون في `matching_candidates` (لشاشة "لماذا لم يُعيَّن سائق" في اللوحة).

### API المطابقة
- الإدارة: CRUD `/admin/matching-settings`, GET `/admin/trips/{id}/matching` (الجولات والمرشحون مع الدرجات)، GET `/admin/matching/stats?from=&to=` (متوسط زمن التعيين، معدل no_drivers، معدل القبول).
- السائق: GET `/driver/offers/active` يعيد إضافةً `score`؟ لا — يعيد `expiresAt` و`round` فقط.

## Flutter
- feature `pricing`: `QuoteCubit` (يستدعي `/pricing/quote` عند تغيّر الوجهة/المحطات/الفئة بـ debounce 600ms، يعرض breakdown في bottom sheet "تفاصيل السعر")، شريحة "اقتراح سعر" مع slider بين `offerMin` و`offerMax` وخانة إدخال، مؤشر الطلب (شارة بلون المستوى "الطلب مرتفع الآن ×1.5").
- `TripRequestCubit` يرسل `quoteId` و`offeredPrice`.
- السائق: شاشة العرض تُظهر `passengerPrice` و`driverNetEarnings` و"اقتراح من الراكب" عند `pricingMode=offer`.

## لوحة الإدارة
- صفحات: المناطق (خريطة Leaflet مع رسم/تحرير المضلع عبر `leaflet-draw` أو إدخال إحداثيات، إعدادات الفئات لكل منطقة، ساعات التشغيل)، قواعد التسعير (جدول + نموذج + مضاعِفات الوقت + معاينة/محاكاة)، الطلب (خريطة حرارية بسيطة للمناطق بلون المستوى + Overrides يدوية)، إعدادات المطابقة، وفي تفاصيل الرحلة تبويب "المطابقة" بالجولات والمرشحين.
