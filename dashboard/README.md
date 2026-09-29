# ATA — لوحة الإدارة (dashboard)

واجهة إدارة منصة ATA: تسجيل دخول مؤسسي، لوحة أرقام موجزة، مراجعة طلبات السائقين ومستنداتهم، إدارة الركاب، فئات الرحلات، سجل التدقيق، الرحلات (F8) والخريطة المباشرة، والمناطق وقواعد التسعير والطلب وإعدادات المطابقة (F9/F10)، والمالية (F11: المدفوعات، الاستردادات، السحوبات، التسويات، المحافظ، الدفتر) والإشعارات (F13: القوالب، الحملات، سجل الإرسال)، والسلامة (F12: مركز الحالات مع تنبيه SOS حيّ، التنبيهات الآلية، المفقودات، قراءة محادثة الرحلة المُدقَّقة) والإلغاء والموثوقية (F14: الأسباب، القواعد مع المحاكي، مراجعة الأعذار، سجل الإلغاءات ومؤشراته، ملفات الموثوقية وسلّم العتبات)، والتقييم والتسويق (F15: التقييمات وبلاغاتها، العروض وأكواد الخصم وحجوزاتها، مستويات السائقين، حوافز السائقين).

- React 19 + Vite + TypeScript
- Tailwind CSS v4 (`@theme` بنفس Tokens نظام التصميم في `docs/01-design-system.md`)
- react-router v7
- Leaflet + OpenStreetMap (خريطة المسار والخريطة المباشرة وخرائط المناطق)
- leaflet-draw (رسم/تحرير مضلعات المناطق في صفحة المناطق)
- @microsoft/signalr (اختياري: `LiveSnapshot` من `/hubs/trips` مع الرجوع إلى الاستطلاع الدوري)
- oxlint

## التشغيل

```bash
cp .env.example .env      # عدّل VITE_API_BASE_URL إن لزم
npm install
npm run dev               # http://localhost:5173
```

| الأمر | الوصف |
|---|---|
| `npm run dev` | خادم التطوير |
| `npm run build` | فحص الأنواع + بناء الإنتاج في `dist/` |
| `npm run lint` | oxlint |
| `npm run preview` | معاينة البناء |

### بيانات الدخول في التطوير

`admin` / `Admin@12345` (حساب البذور في الخلفية).

## البيئة

| المتغير | الافتراضي | الوصف |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5000/api/v1` | عنوان الـAPI بدون شرطة مائلة في النهاية |
| `VITE_HUB_URL` | `<أصل الـAPI>/hubs/trips` | عنوان SignalR Hub للخريطة المباشرة (اختياري) |

## البنية

```
src/
  main.tsx, App.tsx        نقطة الدخول والمسارات
  index.css                Tailwind v4 + tokens + الخط IBM Plex Sans Arabic
  i18n.ts                  قاموس عربي/إنجليزي (العربية افتراضياً)
  nav.ts                   عناصر الشريط الجانبي وعناوين الصفحات
  lib/                     api.ts (Bearer + تجديد التوكن + أخطاء مُهيكلة)، admin.ts (نقاط /admin)،
                           types.ts، session.ts، format.ts، status.ts، transitions.ts،
                           trips.ts (مجموعات الحالات/الحالات النهائية)، leaflet.ts (أيقونات العلامات + أنماط المضلعات + إصلاح الأيقونة الافتراضية)،
                           leafletDraw.ts (تحميل leaflet-draw + تعريب أدواته)، pricing.ts (أيام الأسبوع، المضلعات، الأوزان، تحويل التواريخ)،
                           finance.ts (قوائم الحالات والأسباب، تسميات الحسابات والحركات، saveBlob، parseAmount)،
                           notifications.ts (القنوات والفئات، العناصر النائبة، بيانات المعاينة، أجزاء SMS، تنظيف الجمهور)،
                           safety.ts (أنواع/أولويات/حالات F12، ترتيب الطابور، المؤقتات، المسار المخطط، روابط tel/الخرائط)،
                           cancellation.ts (المراحل والفاعلون وأنواع الرسوم والمستويات، formatRate، التحقق من القاعدة كما في §F14.3)،
                           rewards.ts (F15: قوائم الحالات والأنواع، حالة العرض/الحافز من الصلاحية، تحقق نماذج العرض/المستوى/الحافز، ملخص التقييمات، formatAvg/formatRatio)
  context/                 اللغة، المصادقة، التنبيهات
  hooks/                   useQuery، useDebouncedValue، useApiErrorMessage (+ رسائل أكواد F11/F13)، useUrlState/useUrlSearch (فلاتر متزامنة مع الرابط)، useLiveSnapshot (استطلاع كل 5 ث + SignalR)،
                           useCurrentDemand (استطلاع كل 30 ث + حدث DemandChanged عبر SignalR)،
                           useSafetyFeed (SafetyCaseOpened/Updated وSafetyAlertRaised لمجموعة admins + استطلاع احتياطي كل 10 ث)، useNow (مؤقتات حيّة)، useRatingTags (أسماء وسوم التقييم من `/catalog/rating-tags`)
  components/              Icon, Button, Card, StatCard, Badge, Table, Pagination, Modal, Toast, DefinitionList,
                           Field (Input/Select/Textarea/Toggle), EmptyState, Spinner, Sidebar, Topbar, MapView (غلاف Leaflet)،
                           ZonesMap (مضلعات المناطق)، PolygonEditor (leaflet-draw + إدخال JSON)، Tabs، Money، MetaBadge،
                           RefundModal/RefundActions (الأربع عيون)، MarkPaidModal، CampaignFormModal، DriverFinanceCard، TripPaymentCard، DutyToggle،
                           SosBanner، SafetyCaseCreateModal، TripMessagesPanel، TripSafetyCard، TripCancellationCard، ExcuseReviewModal،
                           CancellationKpis، ReliabilityCard، ChipGroup، FormSection، CityField، ZonePicker (شرائح + خريطة)، Stars، UsageBar،
                           PromotionFormModal، IncentiveFormModal، DriverTierCard، RatingSummaryCard، DriverIncentivesCard، TripRewardsCard…
  layouts/                 RequireAuth (حماية المسارات) + AppLayout (الشريط الجانبي + الشريط العلوي)
  pages/                   Login, Dashboard, Drivers, DriverDetail, Passengers, RideCategories, AuditLogs,
                           Trips, TripDetail, LiveMap, Zones, PricingRules, Demand, MatchingSettings,
                           Payments, PaymentDetail, Refunds, Payouts, PayoutBatches, PayoutBatchDetail, Settlements,
                           SettlementBatchDetail, Wallets, WalletDetail, Ledger, NotificationTemplates, Campaigns,
                           CampaignDetail, NotificationDeliveries, SafetyCases, SafetyCaseDetail, SafetyAlerts, LostItems,
                           CancellationReasons, CancellationRules, CancellationExcuses, CancellationEvents, Reliability, ReliabilityProfile,
                           Ratings, RatingFlags, Promotions, PromotionDetail, DriverTiers, Incentives, IncentiveDetail
```

## المسارات

| المسار | الوصف |
|---|---|
| `/login` | تسجيل الدخول (`POST /auth/admin/login`) |
| `/` | لوحة التحكم: بطاقات الإحصاء + طلبات بانتظار المراجعة + روابط سريعة |
| `/drivers` | طلبات السائقين: فلتر الحالة، بحث، جدول مُصفّح |
| `/drivers/:id` | تفاصيل الطلب: الإجراءات، الملف، المركبة، المستندات مع المعاينة، سجل الحالات |
| `/passengers` | الركاب: بحث، تعليق/إعادة تفعيل |
| `/ride-categories` | فئات الرحلات: إضافة/تعديل/حذف |
| `/audit-logs` | سجل التدقيق مع فلاتر وعرض JSON قبل/بعد |
| `/trips` | الرحلات: تبويبات الحالة (نشطة / مكتملة / ملغاة)، نطاق تاريخ، بحث، جدول مُصفّح (`GET /admin/trips`) |
| `/trips/:id` | تفاصيل الرحلة: الراكب، السائق والمركبة، المسار مع خريطة، الجدول الزمني، الأحداث، قسم «المطابقة» (الجولات والمرشحون من `GET /admin/trips/{id}/matching`)، إلغاء بسبب (`POST /admin/trips/{id}/cancel`) |
| `/live` | الخريطة المباشرة: السائقون (متاح/في رحلة/غير متصل) والرحلات الجارية، تحديث كل 5 ثوانٍ من `GET /admin/live` أو `LiveSnapshot` عبر SignalR |
| `/zones` | المناطق (F10): جدول + خريطة بكل المضلعات، نموذج إنشاء/تعديل مع رسم المضلع (leaflet-draw) أو لصق الإحداثيات، ساعات التشغيل، إعدادات الفئات (تفعيل + حد المضاعف)، حذف (`/admin/zones`) |
| `/pricing-rules` | قواعد التسعير (F10): فلاتر (الفئة/المنطقة/الحالة)، نموذج كامل مع مضاعِفات الوقت وتواريخ السريان (`/admin/pricing-rules`)، ومحاكاة السعر بنقرتين على الخريطة (`POST /admin/pricing/simulate`) |
| `/demand` | الطلب (F10): شبكة الطلب الحالي لكل منطقة بلون المستوى + خريطة (`GET /admin/demand/current` كل 30 ث أو `DemandChanged`)، مضاعِفات المستويات (`PUT /admin/demand-levels/{id}`)، قواعد الطلب، التفعيل اليدوي (`/admin/demand-overrides`) |
| `/matching-settings` | إعدادات المطابقة (F9): إحصاءات بفلتر تاريخ (`GET /admin/matching/stats`)، جدول + نموذج بالأوزان السبعة (مجموعها 1.00) (`/admin/matching-settings`) |
| `/payments` | المدفوعات (F11): تبويبات الحالة، فلاتر الغرض/الطريقة/التاريخ، بحث بالجوال/رقم الرحلة/مرجع البوابة (`GET /admin/payments`) |
| `/payments/:id` | تفاصيل الدفعة: المبالغ، الجدول الزمني، مراجع البوابة، الاستردادات، قيود الدفتر (مع فحص التوازن)، أحداث الـWebhook مع الحمولة؛ زر «استرداد» (المبلغ/السبب/الوجهة) وشريط «بانتظار موافقة ثانية» مع اعتماد/رفض (معطّل لمنشئ الطلب — الأربع عيون) |
| `/refunds` | طابور الاستردادات (افتراضياً «بانتظار الموافقة»، `?status=all` للكل): اعتماد/رفض بسبب/إعادة محاولة (`/admin/refunds/{id}/approve|reject|retry`) |
| `/payouts` | طلبات السحب: تبويبات الحالة، اعتماد (تأكيد)/رفض (سبب)/تأكيد الدفع (مرجع بنكي)، تحديد متعدد → «اعتماد المحدد» (طلب لكل سحب) أو «إنشاء دفعة تحويل»، فلتر `?driverId=` |
| `/payout-batches` · `/payout-batches/:id` | دفعات التحويل: تصدير CSV (blob مع التوكن)، تأكيد دفع الدفعة برقم المرجع |
| `/settlements` · `/settlements/:batchId` | دفعات التسوية + توليد لفترة (الأسبوع الماضي افتراضياً، مدينة اختيارية)؛ التفاصيل: الإجماليات، جدول السائقين (فلتر الاتجاه + بحث، صف قابل للتوسيع بكامل البيان وفحص معادلة الرصيد)، تصدير CSV، اعتماد نهائي/إعادة توليد، استطلاع كل 3 ث أثناء `generating` |
| `/wallets` · `/wallets/:id` | المحافظ: بحث، نوع، «الأرصدة السالبة فقط»؛ التفاصيل: الرصيد ودين النقد مقابل الحد، الحركات، تعديل يدوي (اتجاه/مبلغ/سبب، مُدقَّق)، تجميد/إلغاء تجميد بسبب |
| `/ledger` | أرصدة حسابات الدفتر لفترة (`GET /admin/ledger/balances`) |
| `/notifications/templates` | القوالب (F13): جدول الأحداث من `/admin/notification-events` مع شارة لكل قناة (مفعّل/موقوف/غير موجود)؛ المحرر: عربي/إنجليزي جنباً إلى جنب، إدراج العناصر النائبة بالنقر في موضع المؤشر، تحذير العناصر غير المعرّفة، عدّاد الأحرف وأجزاء SMS، معاينة مباشرة ببيانات تجريبية قابلة للتعديل، تفعيل/إيقاف، إرسال تجريبي |
| `/notifications/campaigns` · `/notifications/campaigns/:id` | الحملات: قائمة بتبويبات الحالة؛ نموذج (الاسم، الفئة، القنوات، الجمهور: الأدوار/المدن/اللغة/الجنس/المستويات/النشاط/مستخدمون محددون + «معاينة العدد»، البدء من قالب أو نص مخصص، الرابط العميق، مسودة/جدولة/إرسال الآن بتأكيد)؛ التفاصيل: الإحصاءات والتقدم (استطلاع أثناء الإرسال)، تعديل/جدولة/إرسال/إلغاء/حذف |
| `/notifications/deliveries` | سجل الإرسال: فلاتر الحدث/المستخدم/القناة/الحالة/التاريخ/`?campaignId=`، الخطأ والحمولة، إعادة المحاولة |

| `/safety` | مركز السلامة (F12): بطاقات الملخص (`GET /admin/safety/summary`)، طابور الحالات (`GET /admin/safety/cases`) مرتّب: غير المحلولة ← الأولوية ← الأقدم، مع مؤقت عمر حيّ، تبويبات الحالة وفلاتر الأولوية/النوع/المسؤول (أنا/غير مُسندة)/التاريخ/البحث؛ شريط «حالة طوارئ جديدة» نابض بصرياً بلا صوت عند `SafetyCaseOpened` (critical/SOS) عبر SignalR، ومع انقطاع الـHub استطلاع الحالات المفتوحة كل 10 ث ومقارنتها؛ إنشاء حالة يدوية |
| `/safety/cases/:id` | تفاصيل الحالة: خريطة (المسار المخطط أو قطع مستقيمة، الالتقاط/الوجهة، موقع البلاغ، آخر موقع للمُبلِّغ، موقع السائق من `LiveSnapshot`)، الرحلة والأطراف بأزرار `tel:` وتسجيل محاولة اتصال، جهات المُبلِّغ الموثوقة (عرض مُدقَّق أثناء الحالة المفتوحة)، التنبيهات، الجدول الزمني والملاحظات (داخلية/مرسلة للمُبلِّغ)، المرفقات، إسناد إليّ، بدء المعالجة، تصعيد مع الجهة، حل برمز الحل، زر الطوارئ 911، محادثة الرحلة (تُحمَّل بطلب صريح لأن القراءة مُدقَّقة) |
| `/safety/alerts` | التنبيهات الآلية (`GET /admin/safety/alerts`): تبويبات الحالة، فلتر النوع/التاريخ، مقاييس التنبيه ومهلة رد الراكب الحيّة، «تم الاطلاع» (`POST …/dismiss` بملاحظة) و«تحويل إلى حالة» (`POST /admin/safety/cases`)، تحديث حيّ عند `SafetyAlertRaised` |
| `/lost-items` | المفقودات (`GET /admin/lost-items`): تبويبات الحالة، بحث، رد السائق، تحديث الحالة بملاحظة (`PATCH /admin/lost-items/{id}`) |
| `/cancellation/events` | سجل الإلغاءات (`GET /admin/cancellations`) بفلاتر الفاعل/المرحلة/المخطئ/حالة الرسوم/العذر/التاريخ/البحث + بطاقات KPIs (`GET /admin/cancellations/stats`) لنفس الفترة |
| `/cancellation/excuses` | طابور مراجعة الأعذار (افتراضياً «بانتظار المراجعة»): العمر ومؤشر SLA (48 س)، الرسوم المعنية، اعتماد/رفض بملاحظة مع توضيح الأثر (إعفاء/استرداد أو تحصيل ونقاط) |
| `/cancellation/reasons` | أسباب الإلغاء: فلتر الفاعل، إضافة/تعديل (ثنائي اللغة، المراحل أو «كل المراحل»، قابل للإعذار/طارئ/يتطلب ملاحظة/قابل للاختيار، الترتيب، التفعيل)، حذف (أو تعطيل إن كان مستخدماً) |
| `/cancellation/rules` | قواعد الإلغاء: فلاتر الفاعل/المرحلة/نوع الحجز، نموذج (المرحلة، نوع الحجز، الفئة، المنطقة، النافذة المجانية، نوع الرسوم ومبلغها/نسبتها وحداها، تعويض السائق، النقاط، الأولوية) مع قيود §F14.3، ومحاكي الرسوم (`POST /admin/cancellation-rules/simulate`) |
| `/reliability` | ملفات الموثوقية (`GET /admin/reliability-profiles`): الدور، فلتر المستوى، بحث؛ تبويب `?tab=thresholds` لتحرير سلّم العتبات لكل دور (`PUT /admin/reliability-thresholds/{id}`) مع تنبيه عند عدم تصاعد السلّم |
| `/reliability/:userId?role=` | ملف الموثوقية: المؤشرات المتحركة، المستوى التالي والأثر، أحداث 60 يوماً، التعديلات، تعديل يدوي (نقاط/مستوى حتى تاريخ/رفع التقييد بسبب) |
| `/ratings` | التقييمات (F15، `GET /admin/ratings`): تبويبات الاتجاه (راكب ← سائق / سائق ← راكب)، فلاتر النجوم/الوسم/البلاغ/الحالة/التاريخ/`?userId=`، بحث، نافذة التفاصيل (الوسوم بمعنى «أعجبه/لم يعجبه» حسب النجوم، التعليق، سبب الإخفاء)، إخفاء بسبب (`POST …/hide`) واستعادة (`POST …/unhide`) مُدقَّقان |
| `/ratings/flags` | بلاغات التقييم (`GET /admin/rating-flags`): تبويبات الحالة (افتراضياً «مفتوح»)، فلتر النوع (تقييم منخفض/متوسط منخفض/تعليق مسيء)، مراجعة بإجراء تجاهل/تحذير/إحالة لمراجعة الإيقاف مع ملاحظة (`POST /admin/rating-flags/{id}/review`) |
| `/promotions` | العروض وأكواد الخصم (`GET /admin/promotions`): تبويبات الحالة (نشط/مجدول/منتهٍ/معطّل)، بحث، الاستخدام مقابل الحد والصرف مقابل الميزانية، إنشاء (نموذج كامل: الكود، النوع والقيمة وحدها، الحد الأدنى للأجرة، الصلاحية، الحد الإجمالي ولكل راكب، الميزانية، الرحلة الأولى/المستخدمون الجدد، المدينة، الفئات والمناطق (مع خريطة) وطرق الدفع وأنواع الحجز كاختيار متعدد، قابل للجمع، عام، نشط)، تعطيل (`POST …/deactivate`) وتفعيل (إعادة حفظ بـ`isActive=true`) |
| `/promotions/:id` | تفاصيل العرض: الاستخدام والميزانية، الإحصاءات (`GET …/stats`)، القيود، تعديل (الكود والنوع مقفلان بعد أول حجز — `409`)، جدول الحجوزات (`GET …/redemptions`) بتبويبات الحالة وسبب التحرير ومجاميع الصفحة وإجمالي الخصم |
| `/driver-tiers` | مستويات السائقين (`/admin/driver-tier-rules`): سلّم المستويات الأربعة قابل للتحرير (الشروط: الرحلات، التقييم، القبول، الإلغاء؛ المزايا: خصم العمولة مع مثال الحصة الفعلية، قيمة المطابقة، نص المزايا) مع تنبيه عند سهولة مستوى أعلى، زر إعادة الحساب (`POST /admin/driver-tiers/recalculate` → `202`)، توزيع السائقين إن أعاد الخادم `driversCount` |
| `/incentives` | حوافز السائقين (`GET /admin/incentives`): تبويبات الحالة (جارٍ/قادم/منتهٍ/معطّل) بعدّادات، بحث، الهدف والمكافأة، الأهلية، الميزانية، المشاركون؛ إنشاء (النوع، المدينة، الفترة، أيام الأسبوع والساعات بتوقيت الرياض، المناطق على الخريطة، الفئات، الهدف والمكافأة وأدنى أجرة، أدنى مستوى/تقييم، الاشتراك، الحد الأقصى للمشاركين، الميزانية، إشعار النشر)، تعطيل |
| `/incentives/:id` | تفاصيل الحافز: التعريف وخريطة المناطق، تقدم السائقين (`GET …/progress`) بتبويبات الحالة، معامل الموثوقية («مخفّض» عند < 1)، المكافأة المصروفة وتاريخها، إلغاء التقدم قبل الصرف بسبب (`POST /admin/incentive-progress/{id}/void`) |

اختصارات تُحوَّل: `/notification-templates`، `/campaigns`، `/notification-deliveries`، `/notifications`، `/safety/cases` → `/safety`، `/safety/lost-items` → `/lost-items`، `/cancellation` و`/cancellations` → `/cancellation/events`، `/reliability-thresholds`، `/rating-flags` → `/ratings/flags`، `/promo-codes` → `/promotions`، `/tiers` → `/driver-tiers`. وفي `/drivers/:id` (المعتمد/الموقوف) بطاقة «المالية» (الرصيد، دين النقد مقابل الحد 500، آخر السحوبات)، وفي `/trips/:id` قسم «الدفع» (الدفعة، الإيصال، استرداد)، وفي `/` بطاقات المالية (المحصّل اليوم، السحوبات والاستردادات المعلّقة، وGMV اليوم إن أعادها الملخص)، وفي الشريط العلوي مفتاح «مناوب» (`/admin/me/duty`) يظهر فقط لمن يستجيب له الخادم.

F12/F14 في الصفحات القائمة: في `/trips/:id` قسم «الإلغاء» (المرحلة، الفاعل، المخطئ، السبب، الرسوم وحالتها، التعويض، النقاط، العذر مع اعتماد/رفض)، قسم «السلامة» (الحالات، التنبيهات، روابط المشاركة)، «محادثة الرحلة» للقراءة، ونافذة الإلغاء الإداري تقبل `atFault` و`chargeFee`؛ في `/drivers/:id` وفي صف كل راكب في `/passengers` بطاقة «الموثوقية» (المستوى، معدل الإلغاء والموثوقية، النقاط، التقييد حتى)؛ وفي `/` بطاقة «السلامة الآن» وبطاقة مؤشرات الإلغاء لآخر 7 أيام (تختفيان عند `403`).

F15 في الصفحات القائمة: في `/drivers` عمودا «المستوى» و«التقييم» (عند تصفية المعتمدين/الموقوفين أو إن أعادتها الصفوف)؛ في `/drivers/:id` شارة المستوى والتقييم في الرأس، وبطاقات «المستوى» (التاريخ مع المؤشرات من `GET /admin/drivers/{id}/tier-history` + تعديل يدوي بسبب `POST /admin/drivers/{id}/tier`)، «ملخص التقييم» (التوزيع، الوسوم +/−، أحدث التعليقات)، «الحوافز»؛ في `/passengers` عمود التقييم وملخص التقييم في الصف الموسّع؛ في `/trips/:id` قسم «التقييم والخصومات» (كود الخصم وحالة حجزه ومبلغه، سطور `discounts[]`، التقييمان)؛ وفي `/` بطاقتا «العروض النشطة» و«استخدامات الأكواد اليوم» (تختفيان عند الخطأ).

## ملاحظات

- التوكن يُخزَّن في `localStorage` ويُجدَّد تلقائياً عند `401` عبر `POST /auth/refresh`؛ إن فشل التجديد تُمسح الجلسة ويُعاد المستخدم إلى `/login`.
- معاينة المستندات تجلب `GET /files/{id}` مع الترويسة `Authorization` ثم تعرض الملف من `Object URL` (صور و PDF).
- الأرقام والمعرّفات تُعرض دائماً `ltr` (نظام التصميم §2).
- الخرائط: `MapView` يُنشئ خريطة Leaflet داخل `useEffect` ويزيلها في التنظيف (آمن مع Strict Mode)؛ العلامات SVG بألوان نظام التصميم فقط، والبلاطات من OpenStreetMap.
- الخريطة المباشرة تستطلع `GET /admin/live` كل 5 ثوانٍ دائماً، وتتحول إلى دفعات `LiveSnapshot` عند نجاح الاتصال بـ`/hubs/trips` (تعود للاستطلاع تلقائياً عند انقطاعه).
- صفحة الطلب تستطلع `GET /admin/demand/current` كل 30 ثانية دائماً، ويؤدي حدث `DemandChanged` من الـHub إلى إعادة الجلب فوراً.
- `leaflet-draw` يعتمد على `window.L` الذي يُنشئه Leaflet؛ لذلك يُستورد دائماً عبر `lib/leafletDraw.ts` بعد `lib/leaflet.ts`. المضلع يُحفظ مغلقاً (`[[lat,lng],…]` مع تكرار النقطة الأولى) ويُحسب المركز تلقائياً.
- قوائم الإدارة الجديدة (المناطق، القواعد، الإعدادات) تُطلب بصفحة واحدة كبيرة (`pageSize=200`) وتقبل الاستجابة مصفوفةً أو غلافَ صفحات (`unwrapList`).
- السلامة: لا تنبيه صوتي (تنبيه بصري نابض يحترم `prefers-reduced-motion`). قراءة محادثة الرحلة وجهات الاتصال الموثوقة لا تُطلب إلا بنقرة لأنها مُسجَّلة في `audit_logs`. رقم الطوارئ `911` قيمة `Safety:EmergencyNumber` الافتراضية (لا يكشفها الـAPI).
- افتراضات F12/F14 غير المنصوص عليها في `docs/09`: `GET /admin/trips/{id}/shares` (بنفس شكل `/safety/trips/{id}/shares`؛ `404` → «غير متاح»)، معامل `tripId` في `/admin/safety/alerts` (مع تصفية محلية)، أسماء حقول `/admin/cancellations/stats` (`passengerCancellationRate`، `driverCancellationRate`، `cancellationFeeRevenue`|`feeRevenue`، …) و`trip`/`liveLocation` في تفاصيل الحالة؛ كلها اختيارية في الأنواع.
- المالية: حد الاعتماد التلقائي للاسترداد (50) وحد دين النقد (500) قيم افتراضية من `docs/08 §F11.8` تُستخدم للتلميحات فقط؛ الخادم هو المرجع (`409 four_eyes_required`). ملفات CSV تُنزَّل عبر `api.download` (Bearer) مع قراءة `Content-Disposition` إن كشفه الخادم في CORS (`Access-Control-Expose-Headers`)، وإلا يُستخدم رقم الدفعة اسماً للملف.
- افتراضات F15 غير المنصوص عليها في `docs/10`: `GET /admin/promotion-redemptions?from=&to=` لعدّاد «استخدامات الأكواد اليوم» (تختفي البطاقة عند `404`)، `GET /admin/drivers/{id}/incentives` لبطاقة حوافز السائق (تختفي عند الخطأ)، `Trip.ratings[]` و`Trip.promotion.promotionId` في تفاصيل الرحلة (وإلا يُبحث في `/admin/ratings?search=<رقم الرحلة>` للرحلات المكتملة)، `userId` في `/admin/ratings` يطابق أي طرف (مع `raterRole` للطرف المقابل = التقييمات المستلمة)، معاملات `tag`/`status`/`search` في `/admin/ratings` (تُعاد تصفية الحالة والوسم محلياً)، وحقول عرض اختيارية (`flagged`، `commentHidden`، `hiddenReason`، `userName`/`driverId`/`rating` في البلاغ، `driversCount` في قواعد المستويات، `participantsCount`/`achievedCount`/`paidCount` في الحافز، `tier`/`ratingAvg`/`ratingCount` في قوائم السائقين والركاب). لا نقطة تفعيل للعرض في العقد، فالتفعيل `PUT` كامل بـ`isActive=true`. ملخص التقييم في بطاقات السائق/الراكب يُحسب محلياً من آخر 100 تقييم (لا يوجد ملخص إداري في العقد)، والمتوسط المعروض هو المخزّن المرجّح إن أُعيد.
