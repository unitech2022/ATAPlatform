# ATA — لوحة الإدارة (dashboard)

واجهة إدارة منصة ATA للخطوة 1: تسجيل دخول مؤسسي، لوحة أرقام موجزة، مراجعة طلبات السائقين ومستنداتهم، إدارة الركاب، فئات الرحلات، وسجل التدقيق.

- React 19 + Vite + TypeScript
- Tailwind CSS v4 (`@theme` بنفس Tokens نظام التصميم في `docs/01-design-system.md`)
- react-router v7
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

## البنية

```
src/
  main.tsx, App.tsx        نقطة الدخول والمسارات
  index.css                Tailwind v4 + tokens + الخط IBM Plex Sans Arabic
  i18n.ts                  قاموس عربي/إنجليزي (العربية افتراضياً)
  nav.ts                   عناصر الشريط الجانبي وعناوين الصفحات
  lib/                     api.ts (Bearer + تجديد التوكن + أخطاء مُهيكلة)، admin.ts (نقاط /admin)،
                           types.ts، session.ts، format.ts، status.ts، transitions.ts
  context/                 اللغة، المصادقة، التنبيهات
  hooks/                   useQuery، useDebouncedValue، useApiErrorMessage
  components/              Icon, Button, Card, StatCard, Badge, Table, Pagination, Modal, Toast,
                           Field (Input/Select/Textarea/Toggle), EmptyState, Spinner, Sidebar, Topbar…
  layouts/                 RequireAuth (حماية المسارات) + AppLayout (الشريط الجانبي + الشريط العلوي)
  pages/                   Login, Dashboard, Drivers, DriverDetail, Passengers, RideCategories, AuditLogs
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

## ملاحظات

- التوكن يُخزَّن في `localStorage` ويُجدَّد تلقائياً عند `401` عبر `POST /auth/refresh`؛ إن فشل التجديد تُمسح الجلسة ويُعاد المستخدم إلى `/login`.
- معاينة المستندات تجلب `GET /files/{id}` مع الترويسة `Authorization` ثم تعرض الملف من `Object URL` (صور و PDF).
- الأرقام والمعرّفات تُعرض دائماً `ltr` (نظام التصميم §2).
