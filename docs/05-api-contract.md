# عقد الـAPI — الخطوة 1

- الأساس: `/api/v1`، JSON (`camelCase`)، UTF-8.
- الرؤوس: `Authorization: Bearer <accessToken>`، `Accept-Language: ar|en` (افتراضي ar)، `X-Device-Id` (اختياري)، `Idempotency-Key` (للعمليات المالية).
- التوقيت ISO-8601 UTC (`2026-09-28T12:00:00Z`). المبالغ أرقام عشرية بالريال السعودي.
- التصفح: `?page=1&pageSize=20` → `{ "items": [], "page": 1, "pageSize": 20, "total": 0 }`.
- الأخطاء (HTTP 400/401/403/404/409/422/429/500):

```json
{ "error": { "code": "otp_invalid", "message": "رمز التحقق غير صحيح", "details": { "attemptsLeft": 3 } } }
```

أكواد شائعة: `validation_failed`, `unauthorized`, `forbidden`, `not_found`, `conflict`, `rate_limited`, `otp_invalid`, `otp_expired`, `otp_locked`, `phone_invalid`, `account_suspended`, `driver_not_approved`, `file_too_large`, `unsupported_file_type`, `insufficient_balance`.

أرقام الجوال: تُقبل `05XXXXXXXX` أو `5XXXXXXXX` أو `+9665XXXXXXXX` وتُطبَّع إلى E.164 `+9665XXXXXXXX`.

---

## 1. المصادقة `/auth`

### POST `/auth/otp/request`
```json
{ "phoneNumber": "0512345678", "role": "passenger" | "driver", "language": "ar" }
```
→ `200`
```json
{ "requestId": "uuid", "phoneNumber": "+966512345678", "expiresInSeconds": 300, "resendAfterSeconds": 60, "devCode": "1234" }
```
`devCode` يظهر فقط عندما `Otp:DevMode=true`. الحد: 3 طلبات/10 دقائق لكل رقم → `429 rate_limited` مع `details.retryAfterSeconds`.

### POST `/auth/otp/verify`
```json
{ "requestId": "uuid", "phoneNumber": "0512345678", "code": "1234", "role": "passenger" | "driver",
  "device": { "deviceId": "string", "platform": "android|ios|web", "deviceName": "string", "appVersion": "1.0.0" } }
```
→ `200` `AuthResponse`:
```json
{
  "accessToken": "jwt", "accessTokenExpiresIn": 3600, "refreshToken": "opaque", "refreshTokenExpiresAt": "...",
  "isNewUser": true,
  "user": { "id": "uuid", "phoneNumber": "+966512345678", "fullName": null, "language": "ar", "gender": "unknown",
            "roles": ["passenger"], "termsAcceptedAt": null, "createdAt": "..." },
  "driver": { "applicationNumber": "ATA-28419", "applicationStatus": "draft" } | null
}
```
- إن لم يكن للمستخدم الدور المطلوب يُضاف تلقائياً (راكب: يُنشأ `passengers`؛ سائق: يُنشأ `drivers` بحالة `draft` ورقم طلب).
- الأخطاء: `otp_invalid` (مع `attemptsLeft`)، `otp_expired`، `otp_locked`، `account_suspended`.

### POST `/auth/refresh` `{ "refreshToken": "..." }` → `AuthResponse` (تدوير التوكن؛ القديم يُلغى).
### POST `/auth/logout` `{ "refreshToken": "..." }` → `204`.

### POST `/auth/admin/login` `{ "username": "admin", "password": "..." }` → `AuthResponse` مع `user.roles: ["admin"]` و`permissions: ["*"]`.

JWT claims: `sub` (userId), `phone`, `roles` (مصفوفة), `name`, `lang`, `perm` (للإدارة). مدة الوصول 60 دقيقة، التجديد 30 يوماً.

---

## 2. الحساب `/me` (مصادق)

- GET `/me` → `{ "user": {...}, "passenger": { "ratingAvg": 5, "ratingCount": 0, "preferFemaleDriver": false, "defaultPaymentMethod": "cash", "memberSince": "2026" } | null, "driver": { "applicationNumber", "applicationStatus", "isOnline", "tier" } | null }`
- PATCH `/me` `{ "fullName"?: "…", "language"?: "ar|en", "gender"?: "male|female", "acceptTerms"?: true }` → المستخدم المحدّث.
- GET `/me/notification-preferences` → `{ "trips": true, "wallet": true, "safety": true, "offers": true }`
- PUT `/me/notification-preferences` نفس الشكل → `200`.
- PUT `/me/devices` `{ "deviceId", "platform", "deviceName", "pushToken"?, "appVersion"? }` → `204`.
- DELETE `/me` → `202` (تعطيل الحساب وجدولة الحذف؛ إلغاء كل التوكنات).

---

## 3. الفهارس `/catalog` (عام)

- GET `/catalog/ride-categories` → `[ { "id", "code": "economy", "name": "اقتصادي", "description": "سيارة مريحة", "icon": "car", "seats": 4, "maxStops": 2, "sortOrder": 2, "estimate": { "etaMinutes": 2, "price": 38 } | null } ]` (الاسم/الوصف حسب `Accept-Language`؛ `estimate` ثابت تجريبي في الخطوة 1 ويُحسب في الخطوة 2).
- GET `/catalog/document-types` → `[ { "id", "code", "name", "appliesTo": "driver|vehicle", "isRequired": true, "requiresExpiry": true } ]`
- GET `/catalog/cities` → `[ { "id", "code": "riyadh", "name": "الرياض" } ]`

---

## 4. الراكب `/passenger` (دور passenger)

- GET `/passenger/trips?status=all|active|completed|cancelled&page=` → صفحة من `TripSummary` `{ "id", "destinationName", "pickupName", "scheduledAt"|null, "completedAt"|null, "status", "fare", "categoryName" }` (فارغة في الخطوة 1).
- GET `/passenger/saved-places` → `[ { "id", "label": "home|work|other", "name", "address", "latitude", "longitude" } ]`
- PUT `/passenger/saved-places/{label}` `{ "name", "address", "latitude", "longitude" }` → `200`
- DELETE `/passenger/saved-places/{label}` → `204`
- PATCH `/passenger/preferences` `{ "preferFemaleDriver"?: bool, "defaultPaymentMethod"?: "cash|wallet|card" }` → `200`

---

## 5. المحفظة `/wallet` (مصادق؛ محفظة الراكب أو السائق حسب الدور النشط في `?kind=passenger|driver`، الافتراضي حسب الدور)

- GET `/wallet` → `{ "id", "kind", "currency": "SAR", "balance": 125.00, "paymentMethods": [ { "type": "wallet", "label": "محفظة ATA", "isDefault": true }, { "type": "cash", "label": "الدفع نقداً" } ] }`
- GET `/wallet/transactions?page=` → صفحة من `{ "id", "type", "direction", "amount", "balanceAfter", "description", "createdAt" }`
- POST `/wallet/topups` (Idempotency-Key مطلوب) `{ "amount": 100, "method": "sandbox" }` → `201 { "transactionId", "balance": 225.00 }` — في الخطوة 1 يُقبل فقط `method: "sandbox"` عندما `Payments:SandboxEnabled=true`؛ غير ذلك `422 validation_failed`. الحد الأدنى 10 والأقصى 5000.

---

## 6. السائق `/driver` (دور driver)

- GET `/driver/application` →
```json
{ "applicationNumber": "ATA-28419", "status": "draft|submitted|under_review|approved|rejected|suspended", "rejectionReason": null,
  "submittedAt": null, "approvedAt": null,
  "profile": { "fullName", "nationalId", "dateOfBirth", "cityId", "gender", "iban" },
  "vehicle": { "id", "make", "model", "year", "color", "plateNumber", "seats", "rideCategoryId" } | null,
  "documents": [ { "id", "documentTypeId", "documentTypeCode", "documentTypeName", "status", "expiresAt", "reviewNote", "fileId", "fileName", "uploadedAt" } ],
  "requiredDocuments": [ { "documentTypeId", "code", "name", "appliesTo", "isRequired", "requiresExpiry", "uploaded": false } ],
  "steps": { "profileComplete": false, "vehicleComplete": false, "documentsComplete": false, "canSubmit": false } }
```
- PUT `/driver/application/profile` `{ "fullName", "nationalId" (10 أرقام), "dateOfBirth": "1990-01-01", "cityId", "gender": "male|female", "iban"?: "SA…" }` → `200` (مسموح فقط في `draft`/`rejected`).
- PUT `/driver/application/vehicle` `{ "make", "model", "year", "color", "plateNumber", "seats", "rideCategoryId" }` → `200`.
- POST `/driver/documents` `multipart/form-data`: `documentTypeId`, `file` (pdf/jpg/jpeg/png ≤ 10MB), `expiresAt?` → `201` عنصر مستند. رفع نوع موجود يستبدل السابق إن كان `pending`/`rejected`.
- DELETE `/driver/documents/{id}` → `204` (فقط `pending`).
- POST `/driver/application/submit` → `200 { "status": "submitted" }`؛ `422` إن كانت الخطوات غير مكتملة مع `details.missing: ["profile","vehicle","documents:insurance"]`.
- GET `/driver/status` → `{ "isOnline": false, "canGoOnline": false, "reason": "driver_not_approved" | null }`
- PUT `/driver/status` `{ "isOnline": true, "latitude"?, "longitude"? }` → `200` نفس الشكل؛ `403 driver_not_approved` إن لم يُعتمد.
- GET `/driver/earnings/summary` → `{ "today": { "earnings": 0, "trips": 0, "onlineHours": 0 }, "week": { "earnings": 0, "target": 2500 }, "ratingAvg": 5 }` (أصفار في الخطوة 1؛ ساعات الاتصال تُحسب من `driver_status_logs`).
- GET `/driver/trips?page=` → صفحة فارغة (الخطوة 2).

---

## 7. الإشعارات `/notifications` (مصادق)

- GET `/notifications?page=` → صفحة من `{ "id", "type", "title", "body", "data", "readAt", "createdAt" }` + `unreadCount`.
- POST `/notifications/read` `{ "ids": ["…"] | null }` (null = الكل) → `204`.

---

## 8. الملفات `/files`

- GET `/files/{id}` → المحتوى (يتطلب أن يكون المالك أو إداري). `Content-Disposition: inline`.

---

## 9. الإدارة `/admin` (دور admin/operations)

- GET `/admin/dashboard/summary` → `{ "pendingDriverApplications", "approvedDrivers", "onlineDrivers", "passengers", "tripsToday": 0, "usersToday" }`
- GET `/admin/drivers?status=&search=&page=` → صفحة من `{ "id", "applicationNumber", "fullName", "phoneNumber", "status", "cityName", "vehicle": "تويوتا كامري 2023 · أ ب ج 2841" | null, "submittedAt", "documentsPending": 2 }`
- GET `/admin/drivers/{id}` → نفس بنية `/driver/application` + `user` + `statusHistory` (من audit).
- POST `/admin/drivers/{id}/review` `{ "action": "start_review" }` → `under_review`.
- POST `/admin/drivers/{id}/approve` → `approved` (يتطلب كل المستندات المطلوبة `verified`؛ وإلا `422`). يرسل إشعاراً للسائق.
- POST `/admin/drivers/{id}/reject` `{ "reason" }` → `rejected`.
- POST `/admin/drivers/{id}/suspend` `{ "reason" }` / POST `/admin/drivers/{id}/reinstate` → `suspended`/`approved`.
- POST `/admin/documents/{id}/verify` `{ "status": "verified|rejected", "note"? }` → المستند المحدّث.
- GET `/admin/passengers?search=&page=` → صفحة من `{ "id", "fullName", "phoneNumber", "status", "createdAt", "tripsCount": 0 }`
- POST `/admin/users/{userId}/suspend` `{ "reason" }` / `/reinstate` → `200`.
- GET/POST `/admin/ride-categories`, PUT/DELETE `/admin/ride-categories/{id}` — الحقول: `code, nameAr, nameEn, descriptionAr, descriptionEn, icon, seats, maxStops, sortOrder, isActive`.
- GET `/admin/audit-logs?entityType=&entityId=&page=`.

كل إجراء إداري يُسجَّل في `audit_logs`.

---

## 10. النظام

- GET `/health` → `{ "status": "Healthy" }` (يفحص MySQL).
- GET `/openapi/v1.json`، وفي التطوير واجهة `/docs`.
