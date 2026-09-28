# ATA — لوحة الإدارة (dashboard)

واجهة إدارة منصة ATA: تسجيل دخول مؤسسي، لوحة أرقام موجزة، مراجعة طلبات السائقين ومستنداتهم، إدارة الركاب، فئات الرحلات، سجل التدقيق، الرحلات (F8) والخريطة المباشرة، والمناطق وقواعد التسعير والطلب وإعدادات المطابقة (F9/F10)، والمالية (F11: المدفوعات، الاستردادات، السحوبات، التسويات، المحافظ، الدفتر) والإشعارات (F13: القوالب، الحملات، سجل الإرسال).

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
                           notifications.ts (القنوات والفئات، العناصر النائبة، بيانات المعاينة، أجزاء SMS، تنظيف الجمهور)
  context/                 اللغة، المصادقة، التنبيهات
  hooks/                   useQuery، useDebouncedValue، useApiErrorMessage (+ رسائل أكواد F11/F13)، useUrlState/useUrlSearch (فلاتر متزامنة مع الرابط)، useLiveSnapshot (استطلاع كل 5 ث + SignalR)،
                           useCurrentDemand (استطلاع كل 30 ث + حدث DemandChanged عبر SignalR)
  components/              Icon, Button, Card, StatCard, Badge, Table, Pagination, Modal, Toast, DefinitionList,
                           Field (Input/Select/Textarea/Toggle), EmptyState, Spinner, Sidebar, Topbar, MapView (غلاف Leaflet)،
                           ZonesMap (مضلعات المناطق)، PolygonEditor (leaflet-draw + إدخال JSON)، Tabs، Money، MetaBadge،
                           RefundModal/RefundActions (الأربع عيون)، MarkPaidModal، CampaignFormModal، DriverFinanceCard، TripPaymentCard، DutyToggle…
  layouts/                 RequireAuth (حماية المسارات) + AppLayout (الشريط الجانبي + الشريط العلوي)
  pages/                   Login, Dashboard, Drivers, DriverDetail, Passengers, RideCategories, AuditLogs,
                           Trips, TripDetail, LiveMap, Zones, PricingRules, Demand, MatchingSettings,
                           Payments, PaymentDetail, Refunds, Payouts, PayoutBatches, PayoutBatchDetail, Settlements,
                           SettlementBatchDetail, Wallets, WalletDetail, Ledger, NotificationTemplates, Campaigns,
                           CampaignDetail, NotificationDeliveries
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

اختصارات تُحوَّل: `/notification-templates`، `/campaigns`، `/notification-deliveries`، `/notifications`. وفي `/drivers/:id` (المعتمد/الموقوف) بطاقة «المالية» (الرصيد، دين النقد مقابل الحد 500، آخر السحوبات)، وفي `/trips/:id` قسم «الدفع» (الدفعة، الإيصال، استرداد)، وفي `/` بطاقات المالية (المحصّل اليوم، السحوبات والاستردادات المعلّقة، وGMV اليوم إن أعادها الملخص)، وفي الشريط العلوي مفتاح «مناوب» (`/admin/me/duty`) يظهر فقط لمن يستجيب له الخادم.

## ملاحظات

- التوكن يُخزَّن في `localStorage` ويُجدَّد تلقائياً عند `401` عبر `POST /auth/refresh`؛ إن فشل التجديد تُمسح الجلسة ويُعاد المستخدم إلى `/login`.
- معاينة المستندات تجلب `GET /files/{id}` مع الترويسة `Authorization` ثم تعرض الملف من `Object URL` (صور و PDF).
- الأرقام والمعرّفات تُعرض دائماً `ltr` (نظام التصميم §2).
- الخرائط: `MapView` يُنشئ خريطة Leaflet داخل `useEffect` ويزيلها في التنظيف (آمن مع Strict Mode)؛ العلامات SVG بألوان نظام التصميم فقط، والبلاطات من OpenStreetMap.
- الخريطة المباشرة تستطلع `GET /admin/live` كل 5 ثوانٍ دائماً، وتتحول إلى دفعات `LiveSnapshot` عند نجاح الاتصال بـ`/hubs/trips` (تعود للاستطلاع تلقائياً عند انقطاعه).
- صفحة الطلب تستطلع `GET /admin/demand/current` كل 30 ثانية دائماً، ويؤدي حدث `DemandChanged` من الـHub إلى إعادة الجلب فوراً.
- `leaflet-draw` يعتمد على `window.L` الذي يُنشئه Leaflet؛ لذلك يُستورد دائماً عبر `lib/leafletDraw.ts` بعد `lib/leaflet.ts`. المضلع يُحفظ مغلقاً (`[[lat,lng],…]` مع تكرار النقطة الأولى) ويُحسب المركز تلقائياً.
- قوائم الإدارة الجديدة (المناطق، القواعد، الإعدادات) تُطلب بصفحة واحدة كبيرة (`pageSize=200`) وتقبل الاستجابة مصفوفةً أو غلافَ صفحات (`unwrapList`).
- المالية: حد الاعتماد التلقائي للاسترداد (50) وحد دين النقد (500) قيم افتراضية من `docs/08 §F11.8` تُستخدم للتلميحات فقط؛ الخادم هو المرجع (`409 four_eyes_required`). ملفات CSV تُنزَّل عبر `api.download` (Bearer) مع قراءة `Content-Disposition` إن كشفه الخادم في CORS (`Access-Control-Expose-Headers`)، وإلا يُستخدم رقم الدفعة اسماً للملف.
