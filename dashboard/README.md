# ATA — لوحة الإدارة (dashboard)

واجهة إدارة منصة ATA: تسجيل دخول مؤسسي، لوحة أرقام موجزة، مراجعة طلبات السائقين ومستنداتهم، إدارة الركاب، فئات الرحلات، سجل التدقيق، الرحلات (F8) والخريطة المباشرة، والمناطق وقواعد التسعير والطلب وإعدادات المطابقة (F9/F10).

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
                           leafletDraw.ts (تحميل leaflet-draw + تعريب أدواته)، pricing.ts (أيام الأسبوع، المضلعات، الأوزان، تحويل التواريخ)
  context/                 اللغة، المصادقة، التنبيهات
  hooks/                   useQuery، useDebouncedValue، useApiErrorMessage، useLiveSnapshot (استطلاع كل 5 ث + SignalR)،
                           useCurrentDemand (استطلاع كل 30 ث + حدث DemandChanged عبر SignalR)
  components/              Icon, Button, Card, StatCard, Badge, Table, Pagination, Modal, Toast, DefinitionList,
                           Field (Input/Select/Textarea/Toggle), EmptyState, Spinner, Sidebar, Topbar, MapView (غلاف Leaflet)،
                           ZonesMap (مضلعات المناطق)، PolygonEditor (leaflet-draw + إدخال JSON)…
  layouts/                 RequireAuth (حماية المسارات) + AppLayout (الشريط الجانبي + الشريط العلوي)
  pages/                   Login, Dashboard, Drivers, DriverDetail, Passengers, RideCategories, AuditLogs,
                           Trips, TripDetail, LiveMap, Zones, PricingRules, Demand, MatchingSettings
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

## ملاحظات

- التوكن يُخزَّن في `localStorage` ويُجدَّد تلقائياً عند `401` عبر `POST /auth/refresh`؛ إن فشل التجديد تُمسح الجلسة ويُعاد المستخدم إلى `/login`.
- معاينة المستندات تجلب `GET /files/{id}` مع الترويسة `Authorization` ثم تعرض الملف من `Object URL` (صور و PDF).
- الأرقام والمعرّفات تُعرض دائماً `ltr` (نظام التصميم §2).
- الخرائط: `MapView` يُنشئ خريطة Leaflet داخل `useEffect` ويزيلها في التنظيف (آمن مع Strict Mode)؛ العلامات SVG بألوان نظام التصميم فقط، والبلاطات من OpenStreetMap.
- الخريطة المباشرة تستطلع `GET /admin/live` كل 5 ثوانٍ دائماً، وتتحول إلى دفعات `LiveSnapshot` عند نجاح الاتصال بـ`/hubs/trips` (تعود للاستطلاع تلقائياً عند انقطاعه).
- صفحة الطلب تستطلع `GET /admin/demand/current` كل 30 ثانية دائماً، ويؤدي حدث `DemandChanged` من الـHub إلى إعادة الجلب فوراً.
- `leaflet-draw` يعتمد على `window.L` الذي يُنشئه Leaflet؛ لذلك يُستورد دائماً عبر `lib/leafletDraw.ts` بعد `lib/leaflet.ts`. المضلع يُحفظ مغلقاً (`[[lat,lng],…]` مع تكرار النقطة الأولى) ويُحسب المركز تلقائياً.
- قوائم الإدارة الجديدة (المناطق، القواعد، الإعدادات) تُطلب بصفحة واحدة كبيرة (`pageSize=200`) وتقبل الاستجابة مصفوفةً أو غلافَ صفحات (`unwrapList`).
