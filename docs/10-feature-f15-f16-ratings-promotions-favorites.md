# F15 التقييم والعروض والمستويات والحوافز + F16 السائق المفضل

الطبقات: MySQL → API (وحدات `Ratings`, `Promotions`, `Incentives`, `Favorites` + تعديلات على `Pricing` و`Trips` و`Matching`) → Flutter (features `rating`, `promotions`, `driver_rewards`, `favorite_drivers` + توسعة `passenger_home` و`trip`) → لوحة الإدارة (التقييمات والبلاغات، العروض، المستويات، الحوافز، قواعد خصم المفضل). لا صفحات للموقع.

يعتمد على: `06`، `07` (المعادلة وخط المطابقة)، `08` (الدفتر §F11.3 — حسابات `discount_promotion`, `discount_favorite_driver`, `incentives`؛ الإشعارات §F13.2؛ الروابط §F13.7)، `09` (`IReliabilityService.IncentiveMultiplier`). الصلاحيات من `12` §F20.

---

## 1. محرك الخصومات الموحّد (F15 + F16)

`IDiscountEngine` يُستدعى في ثلاثة مواضع: التسعير (`/pricing/quote`)، إنشاء الرحلة (حجز الكود)، الإكمال (التطبيق الفعلي على الأجرة النهائية).

```
base  = subtotal × timeMult × demand + booking_fee + service_fee      // "fare" في معادلة F10 قبل الخصم والتقريب
promo = حسب نوع العرض:
          percent          → base × value / 100، بحد أقصى max_discount
          fixed            → min(value, base)
          free_booking_fee → booking_fee
fav   = base × discount_percent / 100، بحد أقصى max_discount_amount     // فقط إن كان السائق المفضل هو المُسنَد (F16)
if promo و fav كلاهما موجود:
    if promotion.is_stackable و rule.stackable_with_promotions:  discounts = [promo, fav]
    else: discounts = [الأكبر]  (التعادل → fav، ويُحرَّر حجز الكود بسبب not_stacked)
discount = min(Σ discounts, base − Promotions:MinPayableFare)            // الافتراضي 0
total    = round(base − discount, 0.5)
driverNet لا يتأثر (يُحسب قبل الخصم — `08` §F11.3)
```

- لا خصومات عند `pricingMode = offer` (السعر يقترحه الراكب) ولا عند `paymentMethod = corporate` (F19). طلب كود في هذه الحالات → `422 promo_not_eligible { reason: "pricing_mode" | "payment_method" }`.
- النتيجة تُكتب في `breakdown.discount` و`breakdown.discounts[]` (الشكل في `08` §F11.6) وفي `trips.discount_total` و`trips.fare_breakdown`.
- الأثر المالي عند الإكمال: قيد `trip_discount` لكل مصدر (`discount_promotion` أو `discount_favorite_driver` ← `trip_revenue`/`cash_collected`).

---

# F15 — التقييم

## F15.1 الجداول

| جدول | الأعمدة |
|---|---|
| `ratings` | id, trip_id (FK)، rater_user_id, rater_role (`passenger`/`driver`)، ratee_user_id, ratee_role, stars TINYINT (1–5)، tags JSON (مصفوفة أكواد)، comment VARCHAR(500) NULL, status (`visible`/`hidden`)، hidden_by NULL, hidden_reason NULL, created_at, updated_at — UNIQUE(trip_id, rater_role)، INDEX(ratee_user_id, ratee_role, created_at) |
| `rating_tags` | id, code UNIQUE, target_role (`driver`/`passenger` = من يُقيَّم)، name_ar, name_en, sort_order, is_active |
| `rating_flags` | id, user_id, role, type (`low_rating`/`low_average`/`abusive_comment`)، rating_id NULL, value DECIMAL(4,2) NULL, status (`open`/`dismissed`/`actioned`)، action NULL (`warned`/`suspension_review`/`none`)، reviewed_by NULL, reviewed_at NULL, note NULL, created_at — INDEX(status, created_at) |

## F15.2 القواعد
- التقييم متاح لطرفي رحلة `completed` خلال `Ratings:WindowHours` (72) من `completed_at` → وإلا `422 rating_window_closed`. مرة واحدة لكل طرف → `409 rating_exists`. لا تعديل بعد الإرسال.
- الوسوم: فئات من `rating_tags` للدور المُقيَّم. للسائق: `driving`, `cleanliness`, `behaviour`, `navigation`, `vehicle_condition`. للراكب: `punctuality`, `behaviour`, `cleanliness`. المعنى: مع نجوم ≤ 3 "ما الذي لم يعجبك"، ومع ≥ 4 "ما الذي أعجبك" (يُشتق من النجوم). وسم غير معروف → `422 validation_failed { tags: "invalid" }`.
- **المتوسط**: متوسط مرجّح لآخر `Ratings:WindowSize` (500) تقييم `visible` للمستخدم في الدور، الأحدث أولاً: `w_i = 1 − (i / N) × (1 − Ratings:MinWeight)` حيث `i` = الترتيب من 0 و`N` = عدد التقييمات في النافذة و`MinWeight` = 0.5. `rating_avg = round(Σ w_i × stars_i / Σ w_i, 2)`؛ بلا تقييمات = 5.00. `rating_count` = إجمالي التقييمات الظاهرة (كل الوقت). يُحدَّث `drivers.rating_avg/rating_count` أو `passengers.rating_avg/rating_count` في نفس المعاملة، وعند إخفاء تقييم.
- **البلاغات**: نجوم ≤ `Ratings:LowRatingThreshold` (2) → `rating_flags (low_rating)`؛ متوسط سائق < `Ratings:DriverMinAverage` (4.30) مع `rating_count ≥ Ratings:MinCountForAverageFlag` (50) → `low_average` (واحد مفتوح لكل سائق)؛ تعليق يطابق قائمة كلمات `Ratings:AbusiveWords` → `abusive_comment` ويُخفى التعليق تلقائياً (`status` يبقى `visible` للنجوم).
- **الخصوصية**: المُقيَّم يرى المتوسط والتوزيع والوسوم الأكثر تكراراً وآخر 20 تعليقاً بلا اسم ولا رقم رحلة ولا تاريخ دقيق (الأسبوع فقط).
- تذكير: `rating.reminder` بعد `Ratings:ReminderAfterMinutes` (30) من الإكمال إن لم يُقيِّم (مرة واحدة).

## F15.3 الـAPI
- GET `/catalog/rating-tags?target=driver|passenger` → `[ { "code", "name" } ]`
- POST `/passenger/trips/{id}/rating` و`/driver/trips/{id}/rating` `{ "stars": 5, "tags": ["driving", "cleanliness"], "comment"?: "…" }` → `201 { "id", "tripId", "stars", "tags", "comment", "createdAt" }`
- GET `/passenger/ratings/pending` و`/driver/ratings/pending` → `[ { "tripId", "tripNumber", "counterpartName", "completedAt", "rateUntil" } ]`
- GET `/driver/ratings/summary` (وللراكب `/passenger/ratings/summary`) → `{ "ratingAvg": 4.87, "ratingCount": 312, "distribution": { "1": 2, "2": 3, "3": 10, "4": 40, "5": 257 }, "topTags": [ { "code", "name", "count", "positive": true } ], "recentComments": [ { "stars", "comment", "week": "2026-W39" } ] }`
- `Trip` يضاف إليه: `"myRating": { "stars", "tags" } | null, "canRate": true, "rateUntil": "…"`.
- الإدارة (`ratings.manage`): GET `/admin/ratings?raterRole=&stars=&flagged=&userId=&from=&to=&page=` → `{ id, tripNumber, raterName, raterRole, rateeName, stars, tags, comment, status, createdAt }`؛ POST `/admin/ratings/{id}/hide` `{ reason }` · `/unhide`؛ GET `/admin/rating-flags?status=&type=&page=`؛ POST `/admin/rating-flags/{id}/review` `{ "action": "dismiss" | "warn" | "suspension_review", "note" }` (`suspension_review` ينشئ ملاحظة ويحوّل السائق لطابور المراجعة في `/drivers/:id`؛ لا يعلّق تلقائياً). Audit: `rating.hide|unhide`, `rating_flag.review`.
- أكواد: `rating_window_closed` (422 "انتهت مدة التقييم")، `rating_exists` (409 "تم تقييم هذه الرحلة مسبقاً").

---

# F15 — العروض (Promo Codes)

## F15.4 الجداول

| جدول | الأعمدة |
|---|---|
| `promotions` | id, code VARCHAR(20) UNIQUE (أحرف لاتينية كبيرة وأرقام، 4–20، يُطبَّع إلى uppercase)، name_ar, name_en, description_ar NULL, description_en NULL, type (`percent`/`fixed`/`free_booking_fee`)، value DECIMAL(12,2) (نسبة 1–100 أو مبلغ)، max_discount NULL, min_fare NULL, valid_from, valid_to, total_usage_limit NULL, per_user_limit INT DEFAULT 1, usage_count INT DEFAULT 0 (محجوز + مطبّق)، budget_amount NULL, spent_amount DECIMAL(12,2) DEFAULT 0, first_trip_only BOOL, new_users_only BOOL, new_user_days INT DEFAULT 30, city_id NULL, ride_category_ids JSON NULL, zone_ids JSON NULL (منطقة الالتقاط)، payment_methods JSON NULL (`["card","wallet"]`)، booking_types JSON NULL, is_stackable BOOL, is_public BOOL (يظهر في قائمة العروض)، is_active, created_by, created_at, updated_at |
| `promotion_redemptions` | id, promotion_id (FK)، passenger_id, trip_id UNIQUE (كود واحد لكل رحلة)، status (`reserved`/`applied`/`released`)، reserved_amount (تقديري)، discount_amount (المطبّق)، reserved_at, applied_at NULL, released_at NULL, release_reason NULL (`trip_cancelled`/`no_drivers`/`not_stacked`/`payment_failed`/`not_eligible_at_completion`/`admin`)، created_at — INDEX(promotion_id, status), INDEX(passenger_id, promotion_id) |

## F15.5 القواعد
- **التحقق** (بالترتيب، أول فشل يُعاد): موجود ونشط (`404 promo_not_found`) → ضمن `valid_from..valid_to` (`422 promo_expired`) → `usage_count < total_usage_limit` و`spent_amount + reserved_amount ≤ budget_amount` (`422 promo_usage_limit_reached`) → عدد حجوزات الراكب (`reserved`+`applied`) < `per_user_limit` (`422 promo_usage_limit_reached { scope: "user" }`) → الأهلية (`422 promo_not_eligible` مع `details.reason` ∈ `first_trip_only`, `new_users_only`, `city`, `category`, `zone`, `payment_method`, `booking_type`, `min_fare`, `pricing_mode`).
  - `first_trip_only`: لا رحلات `completed` للراكب ولا حجز آخر نشط لعرض رحلة أولى. `new_users_only`: `users.created_at ≥ now − new_user_days`. `min_fare`: `base` ≥ `min_fare`.
- **الحجز** عند `POST /passenger/trips` مع `promoCode`: تحقق كامل + `UPDATE promotions SET usage_count = usage_count + 1 WHERE id = ? AND (total_usage_limit IS NULL OR usage_count < total_usage_limit)` (فشل = نفاد) + صف `promotion_redemptions (reserved)` في معاملة إنشاء الرحلة. فشل التحقق → لا تُنشأ الرحلة.
- **التطبيق** عند الإكمال: إعادة الحساب على الأجرة النهائية (قد يختلف المبلغ)؛ إن لم تعد الرحلة مؤهلة (`min_fare`) → `released (not_eligible_at_completion)`؛ وإلا `applied` + `discount_amount` + `spent_amount += discount_amount`.
- **التحرير**: إلغاء / `no_drivers` / فشل الدفع قبل البدء / عدم الجمع → `released` + `usage_count − 1`.
- التعطيل لا يؤثر على الحجوزات القائمة. الكود لا يُعاد استخدامه بعد الحذف (الحذف منطقي بـ`is_active=false`).

## F15.6 الـAPI
- GET `/passenger/promotions` → العروض `is_public` النشطة المؤهلة مبدئياً للراكب: `[ { "code", "name", "description", "type", "value", "maxDiscount", "minFare", "validTo", "firstTripOnly", "rideCategoryCodes": ["economy"] | null, "paymentMethods": null } ]`
- POST `/passenger/promotions/validate` `{ "code": "ATA10", "quoteId"?: "uuid", "rideCategoryId"?: "uuid", "paymentMethod"?: "card", "bookingType"?: "now" }` → `200 { "valid": true, "promotion": { "code", "name", "type", "value", "maxDiscount", "isStackable" }, "discountAmount": 5.00, "totalBefore": 51.00, "totalAfter": 46.00 }` (مع `quoteId`؛ بدونه `discountAmount` = null) أو أحد أكواد الخطأ أعلاه.
- POST `/pricing/quote` يقبل `promoCode` و`favoriteDriverId` (اختياريان): كل فئة يضاف إلى `breakdown` `discounts[]`، ويضاف للاستجابة `promotion: { code, valid, reason }` (كود غير صالح لا يفشل التسعير بل يعيد `valid:false` و`reason`).
- POST `/passenger/trips` يقبل `promoCode`. `Trip` يضاف إليه `"promotion": { "code": "ATA10", "status": "reserved", "discountAmount": null } | null`.
- الإدارة (`promotions.manage`):
  - GET `/admin/promotions?status=active|scheduled|expired|inactive&search=&page=` → `{ id, code, nameAr, type, value, validFrom, validTo, usageCount, totalUsageLimit, spentAmount, budgetAmount, isActive }`
  - POST `/admin/promotions`، GET/PUT `/admin/promotions/{id}` (كل أعمدة `promotions` بصيغة camelCase عدا العدادات؛ `code` و`type` غير قابلين للتعديل بعد أول حجز → `409 conflict`)، POST `/admin/promotions/{id}/deactivate`.
  - GET `/admin/promotions/{id}/redemptions?status=&page=` → `{ id, passengerName, phoneMasked, tripNumber, status, reservedAmount, discountAmount, reservedAt, appliedAt, releaseReason }`
  - GET `/admin/promotions/{id}/stats` → `{ reserved, applied, released, totalDiscount, uniqueUsers, firstTripConversions }`
  - Audit: `promotion.create|update|deactivate`.
- أكواد: `promo_not_found` (404 "كود الخصم غير صحيح")، `promo_expired` (422 "انتهت صلاحية كود الخصم")، `promo_not_eligible` (422 "كود الخصم لا ينطبق على هذه الرحلة")، `promo_usage_limit_reached` (422 "تم استنفاد كود الخصم").

---

# F15 — مستويات السائق

## F15.7 الجداول والقواعد

| جدول | الأعمدة |
|---|---|
| `driver_tier_rules` | id, tier UNIQUE (`bronze`/`silver`/`gold`/`platinum`)، min_completed_trips INT, min_rating_avg DECIMAL(3,2), min_acceptance_rate DECIMAL(5,4), max_cancellation_rate DECIMAL(5,4), commission_discount_percent DECIMAL(5,2), matching_norm DECIMAL(3,2) (قيمة `norm_tier` في F9)، benefits_ar VARCHAR(500), benefits_en, sort_order, updated_at |
| `driver_tier_history` | id, driver_id, from_tier, to_tier, metrics JSON (`{ completedTrips, ratingAvg, acceptanceRate, cancellationRate }`)، reason (`weekly_recalc`/`admin`)، computed_at |

- إعادة الحساب أسبوعياً (`DriverTierRecalcJob` الأحد 03:00 الرياض) على آخر `Tiers:PeriodDays` (28) يوماً: `completedTrips` من `trips`، `ratingAvg` = `drivers.rating_avg`، المعدلات من `reliability_profiles` (`09`). المستوى = أعلى مستوى تتحقق كل شروطه، وإلا `bronze`. `drivers.tier` + سجل التاريخ + `driver.tier_changed` عند التغيير.
- **خصم العمولة**: `effective_driver_share = driver_share_percent + (100 − driver_share_percent) × commission_discount_percent / 100`. مثال: حصة 80% وخصم 10% → 82%. يُطبق في `driverNetEarnings` للعرض (Offer) وعند الإكمال (`trips.driver_earnings`). `fare_quotes` تبقى بلا هوية سائق.
- **وزن المطابقة**: `norm_tier` في F9 = `driver_tier_rules.matching_norm` بدل الخريطة الثابتة.

API:
- GET `/driver/tier` → `{ "tier": "silver", "nextTier": "gold" | null, "periodDays": 28, "metrics": { "completedTrips": 96, "ratingAvg": 4.82, "acceptanceRate": 0.88, "cancellationRate": 0.04 }, "nextRequirements": { "minCompletedTrips": 150, "minRatingAvg": 4.85, "minAcceptanceRate": 0.90, "maxCancellationRate": 0.03 } | null, "benefits": { "commissionDiscountPercent": 5, "text": "…" }, "recalculatesAt": "2026-10-04T00:00:00Z" }`
- الإدارة (`incentives.manage`): GET `/admin/driver-tier-rules`، PUT `/admin/driver-tier-rules/{id}`، POST `/admin/driver-tiers/recalculate` (`202`)، GET `/admin/drivers/{id}/tier-history`، POST `/admin/drivers/{id}/tier` `{ tier, reason }` (تعديل يدوي حتى إعادة الحساب التالية). Audit: `driver_tier_rule.update`, `driver.tier_set`.

---

# F15 — حوافز السائقين

## F15.8 الجداول

| جدول | الأعمدة |
|---|---|
| `driver_incentives` | id, name_ar, name_en, description_ar NULL, description_en NULL, type (`daily`/`weekly`/`zone_quest`/`one_time`)، city_id, zone_ids JSON NULL, ride_category_ids JSON NULL, target_trips INT, reward_amount DECIMAL(12,2), min_trip_fare NULL, starts_at, ends_at, days_of_week JSON NULL (0–6)، daily_from TIME NULL, daily_to TIME NULL (بتوقيت الرياض)، min_tier NULL, min_rating NULL, requires_opt_in BOOL, max_participants NULL, budget_amount NULL, spent_amount DECIMAL(12,2) DEFAULT 0, notify_on_publish BOOL, is_active, created_by, created_at, updated_at |
| `driver_incentive_progress` | id, incentive_id, driver_id, period_start, period_end, opted_in_at NULL, completed_trips INT, status (`in_progress`/`achieved`/`paid`/`expired`/`voided`)، achieved_at NULL, incentive_multiplier DECIMAL(4,2) NULL, reward_amount NULL (المصروف فعلاً)، paid_at NULL, wallet_transaction_id NULL, voided_reason NULL, created_at, updated_at — UNIQUE(incentive_id, driver_id, period_start) |
| `driver_incentive_trips` | id, progress_id, trip_id, counted_at — UNIQUE(progress_id, trip_id) |

## F15.9 القواعد
- **الفترات**: `daily` = كل يوم محلي بين `starts_at` و`ends_at`؛ `weekly` = كل أسبوع (الأحد–السبت)؛ `zone_quest` و`one_time` = فترة واحدة `[starts_at, ends_at)`. داخل الفترة تُقيَّد الرحلات بـ`days_of_week` و`daily_from..daily_to` إن وُجدت.
- **الاحتساب** عند إكمال رحلة: لكل حافز نشط مطابق (مدينة السائق، `completed_at` داخل الفترة والنافذة، منطقة الالتقاط ضمن `zone_ids`، الفئة ضمن `ride_category_ids`، `final_fare ≥ min_trip_fare`) وسائق مؤهل (`min_tier`, `min_rating`, مشترك إن `requires_opt_in`، و`max_participants` لم يكتمل) → upsert للتقدم + صف `driver_incentive_trips` + `completed_trips += 1`. عند بلوغ `target_trips` → `achieved`.
- الرحلة المستردة كلياً (F11) أو المُلغاة إدارياً بعد الإكمال تُطرح من التقدم إن لم يُصرف بعد.
- **الصرف** (`IncentivePayoutJob` كل ساعة): تقدم `achieved` و`period_end + Incentives:PayoutDelayHours` (2) ≤ الآن → `multiplier = IReliabilityService.IncentiveMultiplier(driver)` (`09`)، `reward = round(reward_amount × multiplier, 2)`؛ إن تجاوز الميزانية → `voided (budget_exhausted)`؛ وإلا حركة `incentive` لمحفظة السائق من حساب `incentives` (مفتاح `incentive:{progressId}`) + `paid` + `spent_amount += reward` + إشعار `incentive.achieved`.
- انتهاء الفترة بلا تحقيق → `expired`.
- النشر: حافز جديد نشط مع `notify_on_publish` → `incentive.new` للسائقين المؤهلين في المدينة.

## F15.10 الـAPI
- GET `/driver/incentives?status=active|upcoming|completed` → `[ { "id", "name", "description", "type", "targetTrips", "rewardAmount", "periodStart", "periodEnd", "window": { "daysOfWeek": [4, 5], "from": "16:00", "to": "22:00" } | null, "zones": [ { "id", "name" } ] | null, "rideCategoryCodes": null, "requiresOptIn": false, "optedIn": true, "progress": { "completedTrips": 7, "status": "in_progress", "rewardAmount": null, "paidAt": null } | null } ]`
- GET `/driver/incentives/{id}` → نفس الشكل + `zonesPolygons` (لعرض الخريطة).
- POST `/driver/incentives/{id}/opt-in` → `200`؛ `409 incentive_opt_in_closed` (غير نشط أو اكتمل العدد).
- الإدارة (`incentives.manage`): GET/POST `/admin/incentives`، GET/PUT `/admin/incentives/{id}` (كل أعمدة `driver_incentives`)، POST `/admin/incentives/{id}/deactivate`، GET `/admin/incentives/{id}/progress?status=&page=` → `{ id, driverName, periodStart, completedTrips, status, rewardAmount, incentiveMultiplier, paidAt }`، POST `/admin/incentive-progress/{id}/void` `{ reason }` (قبل الصرف فقط). Audit: `incentive.create|update|deactivate`, `incentive_progress.void`.
- أكواد: `incentive_opt_in_closed` (409 "الاشتراك في هذا الحافز غير متاح").

---

# F16 — السائق المفضل

## F16.1 الجداول

| جدول | الأعمدة |
|---|---|
| `favorite_drivers` | id, passenger_id (FK)، driver_id (FK)، source_trip_id NULL, created_at — UNIQUE(passenger_id, driver_id)، INDEX(driver_id) |
| `favorite_driver_discount_rules` | id, name, discount_percent DECIMAL(5,2) (مثل 5 أو 10)، max_discount_amount DECIMAL(12,2)، min_fare NULL, stackable_with_promotions BOOL, valid_from, valid_to NULL, ride_category_ids JSON NULL, zone_ids JSON NULL, booking_types JSON NULL (`["now","scheduled"]`)، priority INT, is_active, created_by, created_at, updated_at |

تعديلات:
- `trips`: `favorite_driver_id CHAR(36) NULL`, `favorite_status` NULL (`requested`/`accepted`/`unavailable`/`rejected`/`expired`)، `favorite_discount_rule_id CHAR(36) NULL`.
- `matching_attempts`: `mode` (`favorite`/`normal`، افتراضي `normal`). جولة المفضل = `round 0`.

## F16.2 القواعد
- **الإضافة**: يجب وجود رحلة `completed` واحدة على الأقل بين الراكب والسائق (وإلا `422 favorite_not_eligible`)، من شاشة نهاية الرحلة/التقييم أو من سجل الرحلات (`tripId`). الحد `Favorites:MaxPerPassenger` (20) → `422 favorites_limit`. مكرر → `409 favorite_exists`.
- **المفضلون المتاحون** (`/available`): من مفضلي الراكب مَن يحقق أهلية F9 (متصل، معتمد، بلا رحلة، موقع حديث، فئة المركبة مطابقة لـ`rideCategoryId` أو أعلى إن مسموح، وثائق سارية، غير مقيّد في `09`، الجنس إن طلبت الراكبة سائقة، ليس عليه دين نقد فوق الحد) وضمن `Favorites:AvailabilityRadiusMeters` (افتراضياً = `matching_settings.radius_meters`). `etaMinutes` = المسافة ÷ 30 كم/س (نفس تقدير F9). لا يُعاد الموقع الدقيق.
- **الطلب** مع `favoriteDriverId` (يجب أن يكون ضمن مفضلي الراكب، وإلا `422 validation_failed { favoriteDriverId: "not_favorite" }`):
  0. الجولة الحصرية تعمل فقط إن كان `matching_settings.prefer_favorite_driver = true` (F9، افتراضي true) للمنطقة/الفئة؛ وإلا يُعامل الطلب كطلب عادي (`favorite_status = unavailable`) مع بقاء وزن `favorite` في الدرجة.
  1. إن كان مؤهلاً الآن → `favorite_status = requested` وجولة `mode=favorite` (round 0) بعرض **حصري** لهذا السائق بمهلة `Favorites:ExclusiveOfferTimeoutSeconds` (30). `Offer.isFavoriteRequest = true`.
  2. غير مؤهل عند الطلب → `unavailable` ومطابقة عادية فوراً.
  3. رفض/انتهاء → `rejected`/`expired` ثم مطابقة عادية (السائق مستبعد لأنه رفض نفس الرحلة) + `TripUpdated` + push `trip.favorite_fallback`.
  4. مهلة البحث الكلية (`search_timeout_seconds`) تبدأ بعد انتهاء الجولة الحصرية.
- **الخصم**: يُطبق فقط عند `favorite_status = accepted`. القاعدة: أعلى `priority` بين القواعد النشطة السارية المطابقة للفئة ومنطقة الالتقاط ونوع الحجز و`min_fare`؛ يُثبَّت `favorite_discount_rule_id` عند القبول، ويُحسب المبلغ عند الإكمال بمحرك الخصومات (§1). المصدر في الإيصال `favorite_driver`، والقيد `discount_favorite_driver`.
- وزن `favorite` في درجة F9 (`norm_favorite`) يبقى للجولات العادية (يرفع أي سائق مفضل للراكب دون حصرية أو خصم).
- **الرحلات المجدولة**: أولوية حجز حصرية للمفضل قبل فتح السوق — التفاصيل في `11` §F17.
- حذف سائق من المفضلة لا يؤثر على رحلة جارية.

## F16.3 الـAPI

### الراكب
- GET `/passenger/favorite-drivers` → `[ FavoriteDriver ]`
```json
{ "driverId": "uuid", "firstName": "محمد", "photoUrl": "/api/v1/passenger/favorite-drivers/{driverId}/photo", "ratingAvg": 4.93,
  "vehicle": { "make": "Toyota", "model": "Camry", "color": "أبيض" }, "rideCategoryCode": "economy",
  "tripsTogether": 6, "lastTripAt": "…", "createdAt": "…" }
```
- POST `/passenger/favorite-drivers` `{ "driverId"?: "uuid", "tripId"?: "uuid" }` (أحدهما) → `201 FavoriteDriver`.
- DELETE `/passenger/favorite-drivers/{driverId}` → `204`.
- GET `/passenger/favorite-drivers/{driverId}/photo` → صورة السائق (للمفضل فقط).
- GET `/passenger/favorite-drivers/available?lat=&lng=&rideCategoryId=` → `[ { "driverId", "firstName", "photoUrl", "ratingAvg", "vehicle": {…}, "etaMinutes": 4, "discount": { "percent": 10, "maxAmount": 15.00, "stackableWithPromotions": false } | null } ]`
- `POST /passenger/trips` يقبل `favoriteDriverId`. `Trip` يضاف إليه: `"favorite": { "driverId", "driverName", "status": "requested", "discountApplied": false } | null`.
- `POST /pricing/quote` مع `favoriteDriverId` → الخصم محسوب بافتراض القبول، مع `"favoriteDiscountConditional": true` في الاستجابة.

### السائق
- `Offer` يضاف إليه `isFavoriteRequest` (bool) و`exclusive` (bool).
- GET `/driver/favorites/count` → `{ "count": 38 }` (عدد الركاب الذين أضافوه).

### الإدارة (`favorites.manage`)
- GET/POST `/admin/favorite-discount-rules`، GET/PUT/DELETE `/admin/favorite-discount-rules/{id}` (الحقول: `name, discountPercent, maxDiscountAmount, minFare, stackableWithPromotions, validFrom, validTo, rideCategoryIds, zoneIds, bookingTypes, priority, isActive`؛ `discountPercent` بين 1 و50).
- GET `/admin/favorites/stats?from=&to=&cityId=` → `{ favoriteRequests, accepted, fallback, favoriteBookingRate, discountUsageCount, discountTotal, topDrivers: [ { driverId, name, favoritesCount, favoriteTrips } ] }`.
- Audit: `favorite_discount_rule.create|update|delete`.

أكواد: `favorite_not_eligible` (422 "يمكنك إضافة الكابتن بعد إكمال رحلة معه")، `favorite_exists` (409)، `favorites_limit` (422).

## SignalR
- `TripUpdated` يحمل `favorite` و`promotion`.
- للسائق: `OfferReceived` يحمل `isFavoriteRequest`.

## المهام الخلفية والإعدادات

| المهمة | الجدولة | العمل |
|---|---|---|
| `RatingReminderJob` | كل 5 د | `rating.reminder` بعد 30 د من الإكمال لمن لم يُقيِّم |
| `LowAverageFlagJob` | يومياً 04:00 | بلاغات `low_average` |
| `DriverTierRecalcJob` | الأحد 03:00 | §F15.7 |
| `IncentivePeriodJob` | كل 15 د | إنهاء الفترات (`expired`) وفتح فترات الحوافز المتكررة للمشتركين |
| `IncentivePayoutJob` | كل ساعة | §F15.9 |

| المفتاح | الافتراضي |
|---|---|
| `Ratings:WindowHours` / `Ratings:WindowSize` / `Ratings:MinWeight` | 72 / 500 / 0.5 |
| `Ratings:LowRatingThreshold` / `Ratings:DriverMinAverage` / `Ratings:MinCountForAverageFlag` | 2 / 4.30 / 50 |
| `Ratings:ReminderAfterMinutes` / `Ratings:AbusiveWords` | 30 / `[]` |
| `Promotions:MinPayableFare` | 0 |
| `Tiers:PeriodDays` | 28 |
| `Incentives:PayoutDelayHours` | 2 |
| `Favorites:MaxPerPassenger` / `Favorites:ExclusiveOfferTimeoutSeconds` / `Favorites:AvailabilityRadiusMeters` | 20 / 30 / 5000 |

## Flutter

**feature `rating`**:
- entities: `RatingTag`, `PendingRating`, `RatingSummary`؛ usecases: `GetRatingTags`, `SubmitRating`, `GetPendingRatings`, `GetRatingSummary`.
- cubits: `RatingCubit` (النجوم، الوسوم حسب النجوم، التعليق، الإرسال، خيار "إضافة للمفضلة" للراكب يستدعي `AddFavoriteDriver`)، `PendingRatingCubit` (app-wide: بعد الإكمال أو عند التشغيل يعرض ورقة التقييم للرحلة المعلقة مرة واحدة)، `RatingSummaryCubit`.
- pages: `/rate/:tripId` (راكب وسائق — يُفتح تلقائياً من شاشة نهاية الرحلة)، `/driver/ratings` (ملخص السائق).

**feature `promotions`**:
- entities: `Promotion`, `PromoValidation`؛ usecases: `GetPromotions`, `ValidatePromoCode`.
- cubits: `PromotionsCubit` (القائمة)، `PromoCodeCubit` (الإدخال، التحقق عبر `quoteId` الحالي، الحالة: فارغ/جاري/صالح بمبلغ الخصم/خطأ مترجم حسب `reason`، الإزالة).
- pages: `/promotions` (قائمة العروض + نسخ/تطبيق الكود). في ورقة الرئيسية صف "كود الخصم" يفتح ورقة إدخال؛ `HomeCubit`/`QuoteCubit` يرسلان `promoCode` إلى `/pricing/quote`، و`TripRequestCubit` إلى `POST /passenger/trips`، ويعرض السعر قبل/بعد الخصم.

**feature `driver_rewards`** (للسائق):
- entities: `DriverTierInfo`, `Incentive`, `IncentiveProgress`؛ usecases: `GetDriverTier`, `GetIncentives`, `GetIncentive`, `OptInIncentive`.
- cubits: `DriverTierCubit`، `IncentivesCubit` (تبويبات نشطة/قادمة/منتهية)، `IncentiveDetailCubit` (مع خريطة المناطق).
- pages: `/driver/tier`، `/driver/incentives`، `/driver/incentives/:id`. بطاقة "المستوى" وبطاقة "أقرب حافز" في نظرة السائق العامة (`DriverOverviewCubit` يستدعي use cases هذه الميزة).

**feature `favorite_drivers`**:
- entities: `FavoriteDriver`, `AvailableFavorite`, `FavoriteDiscount`؛ usecases: `GetFavoriteDrivers`, `AddFavoriteDriver`, `RemoveFavoriteDriver`, `GetAvailableFavorites`.
- cubits: `FavoriteDriversCubit` (القائمة والحذف)، `AvailableFavoritesCubit` (يُحدَّث عند تغيّر نقطة الالتقاط/الفئة بـdebounce 600ms وكل 30 ث أثناء فتح الورقة؛ الاختيار يضبط `favoriteDriverId` في `HomeCubit` ويعيد التسعير).
- pages: `/account/favorite-drivers`. في ورقة الرئيسية شريط أفقي "كباتنك المفضلون المتاحون" (صورة، اسم، تقييم، ETA، شارة الخصم). `ActiveTripCubit` يعرض حالة المفضل ("بانتظار رد {name}…" ثم "لم يتمكن {name}، نبحث عن كابتن آخر").

## لوحة الإدارة

| المسار | الصفحة |
|---|---|
| `/ratings` | التقييمات بالفلاتر + إخفاء/إظهار |
| `/ratings/flags` | بلاغات التقييم ومراجعتها |
| `/promotions` و`/promotions/:id` | العروض: جدول بالحالة والاستخدام، نموذج كامل (القيود كقوائم متعددة الاختيار للفئات والمناطق وطرق الدفع)، الإحصاءات والحجوزات |
| `/driver-tiers` | قواعد المستويات الأربعة (جدول قابل للتحرير) + زر إعادة الحساب + توزيع السائقين على المستويات |
| `/incentives` و`/incentives/:id` | الحوافز: نموذج (النوع، الفترة، الأيام والساعات، المناطق على خريطة، الفئات، الهدف والمكافأة، الأهلية، الميزانية)، جدول تقدم السائقين، الإلغاء |
| `/favorites` | قواعد خصم المفضل + إحصاءات (معدل الحجز بالمفضل، الاستخدام، أكثر السائقين تفضيلاً) |

وفي `/drivers/:id`: المستوى وتاريخه، ملخص التقييم، الحوافز. وفي `/trips/:id`: التقييمان، العرض المستخدم، حالة المفضل، الخصومات.

## البيانات الأولية (Seed)
- `rating_tags`: للسائق `driving` (القيادة)، `cleanliness` (النظافة)، `behaviour` (التعامل)، `navigation` (معرفة الطريق)، `vehicle_condition` (حالة المركبة)؛ للراكب `punctuality` (الالتزام بالوقت)، `behaviour` (التعامل)، `cleanliness` (النظافة).
- `driver_tier_rules`:

| tier | رحلات (28 يوماً) | تقييم ≥ | قبول ≥ | إلغاء ≤ | خصم العمولة | matching_norm |
|---|---|---|---|---|---|---|
| bronze | 0 | 0 | 0 | 1.00 | 0% | 0.25 |
| silver | 60 | 4.70 | 0.80 | 0.08 | 5% | 0.50 |
| gold | 150 | 4.80 | 0.85 | 0.05 | 10% | 0.75 |
| platinum | 250 | 4.90 | 0.90 | 0.03 | 15% | 1.00 |

- عرض تجريبي: `WELCOME` (percent 20، حد 15 ر.س، `first_trip_only`، `per_user_limit` 1، عام) و`ATA10` (fixed 10، `min_fare` 30، `per_user_limit` 3، غير قابل للجمع).
- قاعدة خصم مفضل: "خصم الكابتن المفضل" 10%، حد 10 ر.س، `stackable_with_promotions = false`، كل الفئات.
- حافز تجريبي: "10 رحلات مساء الخميس والجمعة" (`weekly`، `days_of_week [4,5]`, 16:00–23:59، الهدف 10، المكافأة 75).

## سيناريوهات الاختبار (الخلفية)
التقييم:
1. التقييم بعد 73 س → `422 rating_window_closed`؛ مرتين → `409`؛ رحلة غير مكتملة أو ليس طرفاً → `409`/`403`.
2. المتوسط المرجّح: مجموعة معروفة من التقييمات تعطي القيمة المتوقعة؛ النافذة 500 تتجاهل الأقدم؛ الإخفاء يعيد الحساب.
3. نجمتان → بلاغ `low_rating`؛ متوسط 4.2 مع 60 تقييماً → `low_average` واحد فقط.
4. الملخص للمُقيَّم لا يحتوي أسماء أو أرقام رحلات.

العروض والخصومات:
5. ترتيب رسائل التحقق؛ كل `reason` لـ`promo_not_eligible`.
6. التزامن: حجزان متزامنان لكود `total_usage_limit = 1` → واحد ينجح والآخر `promo_usage_limit_reached`.
7. الإلغاء و`no_drivers` يحرران الحجز ويُنقصان `usage_count`.
8. الإكمال بأجرة أقل من `min_fare` → `released (not_eligible_at_completion)` بلا خصم.
9. `percent` مع `max_discount`، `fixed` أكبر من الأجرة → الأجرة 0 (مع `MinPayableFare`)، `free_booking_fee` = رسوم الحجز.
10. الجمع: عرض غير قابل للجمع + مفضل → الأكبر فقط ويُحرَّر الكود عند فوز المفضل؛ كلاهما قابل → المجموع.
11. `pricingMode=offer` أو `corporate` مع كود → `422 promo_not_eligible`.
12. نصيب السائق لا يتغير بالخصم، وقيود `trip_discount` متوازنة.

المستويات والحوافز:
13. إعادة الحساب تعطي المستوى الصحيح لحدود الشروط (على الحد تماماً = يتحقق)؛ خصم العمولة 10% يعطي 82% من 80%.
14. الحافز: رحلات خارج النافذة/المنطقة/الفئة لا تُحتسب؛ نفس الرحلة لا تُحتسب مرتين؛ الصرف بعد التأخير فقط؛ `incentives_reduced` يصرف 50%؛ تجاوز الميزانية → `voided`.
15. استرداد كامل قبل الصرف يطرح الرحلة من التقدم.

المفضل:
16. الإضافة بلا رحلة مشتركة → `422 favorite_not_eligible`؛ الحد 20.
17. `/available` يستبعد غير المتصل، المقيّد، المشغول، خارج النصف القطري، الفئة المختلفة.
18. طلب مع مفضل متاح → جولة `favorite` بعرض حصري 30 ث؛ القبول → `accepted` وخصم عند الإكمال بقيد `discount_favorite_driver`؛ الرفض/الانتهاء → مطابقة عادية بدون خصم وبدون إعادة عرض على نفس السائق.
19. المفضل غير متاح عند الطلب → `unavailable` ومطابقة فورية.
20. مهلة البحث الكلية تبدأ بعد الجولة الحصرية.

## قرارات تحتاج تأكيد مالك المنتج
1. لا خصومات مع "اقترح سعرك" ولا مع دفع الشركات.
2. عند عدم الجمع يُطبق الخصم الأكبر تلقائياً (لصالح الراكب) ويُحرَّر الكود.
3. المتوسط المرجّح يعطي الأحدث وزناً 1 والأقدم 0.5 ضمن آخر 500 تقييم.
4. خصم عمولة المستوى نسبة من العمولة (وليس نقاطاً مئوية) — المثال 80% → 82% عند 10%.
5. الحوافز تُصرف بعد نهاية الفترة بساعتين (لا فوراً عند التحقيق) لإتاحة كشف الاحتيال.
6. مهلة العرض الحصري للمفضل 30 ثانية (أطول من العرض العادي 20 ث).
7. المُقيَّم يرى التعليقات بلا هوية أصحابها وبدقة أسبوع.
