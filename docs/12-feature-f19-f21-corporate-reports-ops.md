# F19 الشركات + F20 التقارير والصلاحيات + F21 الجاهزية التشغيلية

الطبقات:
- **F19**: MySQL → API (وحدة `Corporate`) → Flutter (feature `corporate`) → الموقع (بوابة الشركات `/business`) → لوحة الإدارة (حسابات الشركات والفواتير).
- **F20**: MySQL → API (وحدتا `Rbac` و`Reporting` + توسعة `Identity`) → لوحة الإدارة (المستخدمون، الأدوار، MFA، الجلسات، التقارير). لا Flutter.
- **F21**: البنية التحتية عبر كل المشاريع (الخلفية، Docker، nginx، الوثائق) + صفحة الخصوصية في Flutter.

يعتمد على: `05`، `06`، `07`، `08` (الدفتر §F11.3 — `corporate_receivable:{id}`، الإشعارات §F13.2)، `09`، `10`، `11`. هذا المستند هو **المرجع الموحّد** لكتالوج الصلاحيات (§F20.2)، وتعريفات مؤشرات الأداء (§F20.6)، ومرجع متغيرات البيئة (§F21.10).

---

# F19 — حسابات الشركات

## F19.1 الجداول

| جدول | الأعمدة |
|---|---|
| `corporate_accounts` | id, account_number (UNIQUE `CA-#####`)، legal_name_ar, legal_name_en, display_name, cr_number VARCHAR(10) UNIQUE (السجل التجاري، 10 أرقام)، vat_number VARCHAR(15) NULL (15 رقماً يبدأ وينتهي بـ3)، billing_email VARCHAR(254), billing_address JSON (العنوان الوطني: `{ buildingNumber, street, district, city, postalCode, additionalNumber, countryCode: "SA" }`)، city_id, contact_name, contact_phone (E.164)، status (`pending`/`active`/`suspended`/`closed`)، credit_limit DECIMAL(12,2), billing_cycle (`monthly`)، payment_terms_days INT (30)، default_policy_id NULL, notes VARCHAR(1000) NULL, created_by, created_at, updated_at |
| `corporate_users` | id, corporate_account_id (FK)، user_id NULL (يُربط عند القبول)، phone_number (E.164)، full_name NULL, role (`corporate_admin`/`employee`)، employee_number VARCHAR(40) NULL, department VARCHAR(80) NULL, cost_center_id NULL, policy_id NULL (NULL = السياسة الافتراضية)، monthly_budget DECIMAL(12,2) NULL (يتقدم على السياسة)، status (`invited`/`active`/`disabled`)، invited_by, activated_at NULL, disabled_at NULL, created_at, updated_at — UNIQUE(corporate_account_id, phone_number)، INDEX(user_id, status) |
| `corporate_invitations` | id, corporate_account_id, corporate_user_id (FK)، phone_number, token_hash CHAR(64) UNIQUE (SHA-256)، expires_at (بعد `Corporate:InvitationDays` = 7)، sent_at, accepted_at NULL, declined_at NULL, revoked_at NULL, created_at |
| `corporate_cost_centers` | id, corporate_account_id, code VARCHAR(30), name VARCHAR(120), is_active, created_at, updated_at — UNIQUE(corporate_account_id, code) |
| `corporate_policies` | id, corporate_account_id, name, is_default BOOL, allowed_ride_category_ids JSON NULL, allowed_days JSON NULL (0–6)، time_windows JSON NULL (`[{ "from": "07:00", "to": "22:00" }]` بتوقيت الرياض لوقت الالتقاط)، allowed_zone_ids JSON NULL, zone_match (`pickup_and_dropoff`/`pickup_or_dropoff`)، max_fare_per_trip DECIMAL(12,2) NULL, monthly_budget_per_employee DECIMAL(12,2) NULL, require_purpose BOOL, require_cost_center BOOL, allow_scheduled BOOL DEFAULT true, allow_guest_booking BOOL DEFAULT true, is_active, created_at, updated_at |
| `corporate_adjustments` | id, corporate_account_id, amount DECIMAL(12,2) (موقّع، شامل الضريبة)، description VARCHAR(255), invoice_id NULL (يُملأ عند الفوترة)، created_by, created_at |
| `corporate_invoices` | id, invoice_number (UNIQUE `INV-YYYYMM-#####`)، corporate_account_id, period_start DATE, period_end DATE (شامل، أيام الرياض)، issue_date DATE, due_date DATE, currency (`SAR`)، trips_count, subtotal_excl_vat, vat_rate DECIMAL(5,2) (15.00)، vat_amount, total_incl_vat, status (`draft`/`issued`/`paid`/`overdue`/`void`)، seller_snapshot JSON (اسم المنصة القانوني، الرقم الضريبي، العنوان)، buyer_snapshot JSON (من الحساب وقت الإصدار)، pdf_file_id NULL, issued_by NULL, issued_at NULL, paid_at NULL, paid_amount NULL, payment_reference NULL, void_reason NULL, created_at, updated_at — UNIQUE(corporate_account_id, period_start) (عدا `void`) |
| `corporate_invoice_lines` | id, invoice_id (FK)، line_type (`trip`/`cancellation_fee`/`adjustment`)، trip_id NULL, trip_number NULL, trip_date DATETIME(6) NULL, employee_name NULL, employee_number NULL, department NULL, cost_center_code NULL, guest_name NULL, purpose NULL, pickup_name NULL, dropoff_name NULL, description VARCHAR(255), amount_excl_vat, vat_amount, amount_incl_vat, created_at — INDEX(invoice_id) |
| `corporate_api_keys` | id, corporate_account_id, name, key_prefix CHAR(8), key_hash CHAR(64), scopes JSON (`["bookings:read","bookings:write","reports:read"]`)، last_used_at NULL, expires_at NULL, revoked_at NULL, created_by, created_at — **للمستقبل**: لا يوجد مخطط مصادقة يستهلكها في v1 |

تعديلات:
- `trips`: `payment_method` يقبل `corporate`؛ أعمدة `corporate_account_id NULL`, `corporate_user_id NULL` (الموظف المستفيد أو NULL للضيف)، `booked_by_user_id NULL`, `is_guest BOOL DEFAULT false`, `guest_name VARCHAR(80) NULL`, `guest_phone NULL`, `trip_purpose VARCHAR(200) NULL`, `cost_center_id NULL` — INDEX(corporate_account_id, completed_at).
- `PaymentMethodKind` يضاف إليه `Corporate` (JSON `corporate`). لا يمكن أن يكون `passengers.default_payment_method`.
- `user_roles.role = corporate_admin` يُمنح عند تفعيل مسؤول شركة.

## F19.2 القواعد

### العضوية والدعوات
- الحساب ينشئه مسؤول المنصة (`corporate.manage`) بحالة `pending` ثم يدعو أول مسؤول شركة؛ التفعيل (`active`) يتطلب مسؤولاً واحداً مفعّلاً على الأقل.
- الدعوة (من المنصة أو مسؤول الشركة): صف `corporate_users (invited)` + `corporate_invitations` + إشعار `corporate.invitation` (SMS إلى الرقم، وinapp/push إن كان المستخدم موجوداً). رابط SMS: `{Corporate:PortalBaseUrl}/business/join/{token}`.
- القبول: الموظف في التطبيق (راكب) يرى الدعوات المطابقة لرقمه الموثّق ويقبلها → `active`، `user_id`، `activated_at`. مسؤول الشركة يقبل ضمنياً عند أول دخول ناجح للبوابة بنفس الرقم. الدعوة المنتهية → `410 invitation_expired`.
- المستخدم عضو فعّال في حساب شركة واحد فقط → `409 corporate_member_elsewhere`.
- التعطيل (`disabled`) يمنع الحجز فوراً؛ الرحلات الجارية تكمل.

### تقييم السياسة (`CorporatePolicyEvaluator`)
المدخلات: العضو، الفئة، الالتقاط/الوجهة (المناطق عبر `ZoneResolver`)، وقت الالتقاط المحلي، الأجرة التقديرية، نوع الحجز، الغرض، مركز التكلفة.
```
violations = []
category ∉ allowed_ride_category_ids                         → { rule: "category" }
day(pickupAt) ∉ allowed_days                                → { rule: "day" }
time(pickupAt) خارج كل time_windows                          → { rule: "time_window" }
zones: pickup_and_dropoff → كلاهما ضمن allowed_zone_ids؛ pickup_or_dropoff → أحدهما  → { rule: "zone" }
estimatedFare > max_fare_per_trip                           → { rule: "max_fare", limit }
bookingType = scheduled و !allow_scheduled                  → { rule: "scheduled" }
require_purpose و purpose فارغ                              → { rule: "purpose_required" }
require_cost_center و costCenterId فارغ/غير نشط             → { rule: "cost_center_required" }
budget = corporate_users.monthly_budget ?? policy.monthly_budget_per_employee
spent  = Σ final_fare لرحلات الموظف المكتملة هذا الشهر (الرياض) + Σ estimated_fare لرحلاته الشركاتية الجارية/المجدولة
budget != null و spent + estimatedFare > budget             → corporate_budget_exceeded { remaining }
الحساب: unbilled + unpaid_invoices + estimatedFare > credit_limit → corporate_credit_limit_exceeded
```
- المخالفات تعيد `422 corporate_policy_violation { violations: [ { rule, limit?, allowed? } ] }`. الميزانية والحد الائتماني لهما أكوادهما.
- الإكمال بأجرة نهائية تتجاوز `max_fare_per_trip` أو الميزانية مسموح (يُسجَّل حدث رحلة `corporate_policy_exceeded`).
- لا عروض ولا خصم مفضل مع `corporate` (`10` §1). رسوم الإلغاء تُفوتر على الشركة (`09`).

### الحجز
- **موظف لنفسه** (التطبيق): `POST /passenger/trips` بـ`paymentMethod: "corporate"`, `tripPurpose`, `costCenterId` → الرحلة `corporate_user_id` = العضو، `booked_by_user_id` = المستخدم.
- **مسؤول الشركة لموظف** (البوابة): `passenger_id` = ملف الراكب للموظف (يتلقى إشعارات الرحلة في تطبيقه)، `booked_by_user_id` = المسؤول.
- **مسؤول الشركة لضيف** (البوابة، `allow_guest_booking`): `passenger_id` = ملف راكب المسؤول (يُنشأ تلقائياً إن لم يوجد)، `is_guest = true`, `guest_name`, `guest_phone`. يُرسل SMS `corporate.guest_trip` للضيف فيه رابط التتبع (F12) وPIN الرحلة. السائق يرى اسم الضيف الأول (`Offer.passenger.firstName`) والاتصال المقنّع يوجَّه لرقم الضيف. قاعدة `trip_active_exists` لا تنطبق على حجوزات الضيوف (المسؤول يستطيع حجز عدة رحلات ضيوف متزامنة حتى `Corporate:MaxActiveGuestTripsPerAdmin` = 10).
- الأثر المالي عند الإكمال: قيد `trip_corporate_charge` (`corporate_receivable:{accountId}` ← `trip_revenue`) + `trip_earning` للسائق.

### الفواتير الشهرية
- `CorporateInvoiceJob` يوم `Corporate:InvoiceDayOfMonth` (1) الساعة 04:00 بتوقيت الرياض للشهر السابق، لكل حساب له حركات:
  - أسطر `trip` لكل رحلة شركة `completed` في الشهر، وأسطر `cancellation_fee` لرسوم الإلغاء المفوترة، وأسطر `adjustment` لكل `corporate_adjustments` بلا فاتورة.
  - لكل سطر (الأسعار شاملة الضريبة): `amount_excl_vat = round(amount_incl_vat / 1.15, 2)`، `vat_amount = amount_incl_vat − amount_excl_vat`. مجاميع الفاتورة = مجموع الأسطر.
  - `issue_date` = يوم التوليد، `due_date = issue_date + payment_terms_days`.
  - `Corporate:AutoIssueInvoices=true` → `issued` مباشرة مع PDF؛ وإلا `draft` للمراجعة.
- **PDF**: عبر QuestPDF (دعم RTL العربي)، ثنائي اللغة، يحتوي: بيانات البائع والمشتري (الاسم، الرقم الضريبي، العنوان الوطني)، رقم وتاريخ الفاتورة، الفترة، جدول الأسطر، المجاميع والضريبة، ورمز QR بترميز TLV (اسم البائع، الرقم الضريبي، الطابع الزمني، الإجمالي، الضريبة). يُخزَّن في `stored_files` (`pdf_file_id`).
- الإشعار: `corporate.invoice_issued` لمسؤولي الشركة + بريد إلى `billing_email` عبر `IEmailSender` (`LoggingEmailSender` افتراضياً؛ `Email:Provider`).
- `CorporateInvoiceOverdueJob` يومياً: `issued` و`due_date < today` → `overdue`. إن `Corporate:SuspendAfterOverdueDays` > 0 وتجاوز التأخير ذلك → الحساب `suspended` (+ audit، actor system).
- التحصيل: `mark-paid` → قيد `corporate_invoice_payment` (`platform_cash` ← `corporate_receivable:{id}`) بالمبلغ المدفوع؛ جزئي → الحالة تبقى `issued`/`overdue` مع `paid_amount`.
- `void` للفواتير غير المدفوعة فقط؛ تتحرر أسطرها للتوليد مجدداً.

## F19.3 المصادقة لبوابة الشركات
- `POST /auth/otp/request` يقبل `role: "corporate_admin"` (يُرسل الرمز دائماً دون كشف العضوية).
- `POST /auth/otp/verify` بـ`role: "corporate_admin"`: يتطلب `corporate_users` بدور `corporate_admin` وحالة `invited`/`active` لحساب `active`، وإلا `403 corporate_not_member`. النتيجة `AuthResponse` مع `user.roles` تحتوي `corporate_admin`، ومطالبة JWT جديدة `corp` = `corporate_account_id`، و`session_kind = corporate`.
- سياسة جديدة `Policies.CorporateAdmin` (الدور + `corp`). كل استعلامات `/corporate/*` مقيدة بـ`corp`.

## F19.4 الـAPI

### الراكب (الموظف)
- GET `/passenger/corporate` →
```json
{ "membership": { "corporateUserId", "accountId", "companyName": "شركة المثال", "role": "employee", "employeeNumber", "department", "status": "active" },
  "policy": { "name", "allowedRideCategoryCodes": ["economy", "comfort"] | null, "timeWindows": [ … ] | null, "allowedDays": null,
              "maxFarePerTrip": 150.00, "requirePurpose": true, "requireCostCenter": false, "allowScheduled": true },
  "budget": { "monthly": 1500.00, "spent": 420.50, "remaining": 1079.50 } | null,
  "costCenters": [ { "id", "code", "name" } ] } | null
```
- GET `/passenger/corporate/invitations` → `[ { "id", "companyName", "role", "expiresAt" } ]`؛ POST `/passenger/corporate/invitations/{id}/accept` · `/decline` → `200`؛ الأخطاء: `invitation_expired`, `corporate_member_elsewhere`.
- `POST /pricing/quote` يقبل `paymentMethod: "corporate"` (+`tripPurpose`, `costCenterId`) ويضيف `corporate: { "allowed": false, "violations": [ … ], "remainingBudget": 79.50 }`.
- `POST /passenger/trips` يقبل `paymentMethod: "corporate"`, `tripPurpose`, `costCenterId`؛ الأخطاء: `corporate_not_member`, `corporate_account_inactive`, `corporate_policy_violation`, `corporate_budget_exceeded`, `corporate_credit_limit_exceeded`.
- `Trip` يضاف إليه: `"corporate": { "companyName", "purpose", "costCenter": "IT-01" | null, "isGuest": false, "guestName": null } | null`.

### مسؤول الشركة `/corporate` (`Policies.CorporateAdmin`)
| المسار | الوصف |
|---|---|
| GET `/corporate/account` · PUT `/corporate/account` `{ billingEmail, billingAddress, contactName, contactPhone }` | بيانات الحساب (الحقول القانونية للقراءة) |
| GET `/corporate/dashboard` → `{ monthToDate: { trips, spend }, activeEmployees, invitedEmployees, budgetUtilizationPercent, creditLimit, creditUsed, openInvoices: { count, amount }, recentTrips: [ … ] }` | |
| GET `/corporate/employees?status=&department=&costCenterId=&search=&page=` → `{ id, fullName, phoneNumber, role, employeeNumber, department, costCenter, policyName, monthlyBudget, spentThisMonth, status, activatedAt }` | |
| POST `/corporate/employees` `{ phoneNumber, fullName, role, employeeNumber?, department?, costCenterId?, policyId?, monthlyBudget? }` → `201` (دعوة) | |
| POST `/corporate/employees/import` (multipart CSV: `phone_number,full_name,employee_number,department,cost_center_code,monthly_budget,role`) → `{ created, skipped: [ { row, reason } ] }` | |
| PUT `/corporate/employees/{id}` · POST `/{id}/disable` · `/enable` · `/resend-invitation` · DELETE (المدعوون فقط → إلغاء الدعوة) | لا يمكن تعطيل آخر مسؤول (`409 conflict`) |
| GET/POST `/corporate/policies`، PUT/DELETE `/corporate/policies/{id}`، POST `/corporate/policies/{id}/default` | الحقول = أعمدة `corporate_policies` |
| GET/POST `/corporate/cost-centers`، PUT/DELETE `/corporate/cost-centers/{id}` | |
| POST `/corporate/bookings/quote` (مدخلات `/pricing/quote` + `employeeId?` + `purpose`, `costCenterId`) → التسعير + تقييم السياسة | |
| POST `/corporate/bookings` `{ employeeId? , guest?: { name, phoneNumber }, pickup, dropoff, stops, rideCategoryId, bookingType, scheduledAt?, quoteId?, tripPurpose, costCenterId?, riderNote? }` → `201 Trip` (أحد `employeeId`/`guest`) | |
| GET `/corporate/bookings?status=&from=&to=&employeeId=&isGuest=&page=`؛ GET `/corporate/bookings/{tripId}` (حالة حيّة، السائق والمركبة، الإيصال عند الانتهاء)؛ POST `/corporate/bookings/{tripId}/cancel` `{ reasonCode, note? }` (معاينة: `/cancel/preview`) | كل رحلات الشركة |
| GET `/corporate/invoices?status=&page=`؛ GET `/corporate/invoices/{id}` (+ `lines` مُصفّحة)؛ GET `/corporate/invoices/{id}/pdf`؛ GET `/corporate/invoices/{id}/export?format=csv` | |
| GET `/corporate/reports/summary?from=&to=&groupBy=employee|department|cost_center|month|category` → `{ rows: [ { key, label, trips, amount, avgFare } ], totals: { trips, amount } }` | |
| GET `/corporate/reports/trips?from=&to=&employeeId=&department=&costCenterId=&page=`؛ GET `/corporate/reports/trips/export?format=csv&…` (UTF-8 BOM: `trip_number, date, employee, employee_number, department, cost_center, guest, purpose, category, pickup, dropoff, distance_km, amount_incl_vat, vat, status`) | |
| GET/POST/DELETE `/corporate/api-keys` (فقط إن `Corporate:ApiKeysEnabled=true`؛ وإلا `404`؛ الإنشاء يعيد المفتاح كاملاً مرة واحدة `ata_live_<prefix><secret>`) | للمستقبل |

### الإدارة (`corporate.manage`)
- GET/POST `/admin/corporate/accounts`، GET/PUT `/admin/corporate/accounts/{id}`، POST `/admin/corporate/accounts/{id}/activate` · `/suspend {reason}` · `/close {reason}`.
- POST `/admin/corporate/accounts/{id}/admins` `{ phoneNumber, fullName }` (دعوة مسؤول)، GET `/admin/corporate/accounts/{id}/employees?page=`.
- POST `/admin/corporate/accounts/{id}/adjustments` `{ amount, description }`.
- GET `/admin/corporate/invoices?accountId=&status=&from=&to=&page=`، POST `/admin/corporate/accounts/{id}/invoices/generate` `{ periodStart: "2026-09-01" }`، POST `/admin/corporate/invoices/{id}/issue` · `/mark-paid { amount, reference, paidAt? }` · `/void { reason }`، GET `/admin/corporate/invoices/{id}/pdf`.
- GET `/admin/corporate/receivables` → `[ { accountId, name, creditLimit, unbilled, unpaidInvoices, overdueAmount } ]` (يتطلب أيضاً `payments.view`).
- Audit: `corporate_account.create|update|activate|suspend|close`, `corporate_user.invite|update|disable|enable`, `corporate_policy.*`, `corporate_invoice.generate|issue|mark_paid|void`, `corporate_adjustment.create`؛ وإجراءات مسؤول الشركة تُسجَّل أيضاً في `audit_logs` بـ`actor_role = corporate_admin`.

### أكواد الأخطاء (F19)
| الكود | HTTP | ar |
|---|---|---|
| `corporate_not_member` | 403 | لست عضواً في حساب شركة |
| `corporate_account_inactive` | 403 | حساب الشركة غير نشط |
| `corporate_member_elsewhere` | 409 | الرقم مرتبط بحساب شركة آخر |
| `corporate_policy_violation` | 422 | الرحلة تخالف سياسة شركتك |
| `corporate_budget_exceeded` | 422 | تجاوزت الميزانية الشهرية المتاحة |
| `corporate_credit_limit_exceeded` | 422 | تجاوز حساب الشركة الحد الائتماني |
| `invitation_expired` | 410 | انتهت صلاحية الدعوة |

## F19.5 Flutter — feature `corporate`
- entities: `CorporateMembership`, `CorporatePolicySummary`, `CorporateBudget`, `CostCenter`, `CorporateInvitation`.
- usecases: `GetCorporateMembership`, `GetCorporateInvitations`, `AcceptCorporateInvitation`, `DeclineCorporateInvitation`.
- cubits: `CorporateMembershipCubit` (app-wide للراكب: يُحمَّل بعد الدخول)، `CorporateInvitationsCubit`، `CorporatePaymentCubit` (داخل ورقة الرئيسية عند اختيار "حساب الشركة": الغرض، مركز التكلفة، عرض المخالفات والميزانية المتبقية من استجابة التسعير؛ يزوّد `HomeCubit`/`TripRequestCubit` بـ`tripPurpose`, `costCenterId`).
- pages: `/account/corporate` (العضوية، السياسة، الميزانية، الدعوات). خيار الدفع "حساب الشركة" يظهر فقط عند `membership.status = active`، ويعطَّل مع سبب عند `allowed = false`.

## F19.6 الموقع — بوابة الشركات `/business`

| المسار | الصفحة |
|---|---|
| `/business` | صفحة تعريفية (المزايا، السياسات، الفواتير الشهرية) + "دخول الشركات" + نموذج "تواصل للمبيعات" (mailto/واتساب — بلا API) |
| `/business/login` | الجوال + OTP (مكونات `Keypad`/`OtpBoxes` القائمة) بـ`role: corporate_admin` |
| `/business/join/:token` | صفحة الدعوة: للموظف تعليمات تحميل التطبيق والدخول بنفس الرقم؛ للمسؤول زر الدخول للبوابة |
| `/business/app` | لوحة الشركة (`/corporate/dashboard`) |
| `/business/app/employees` و`/business/app/employees/:id` | الموظفون: جدول، دعوة، استيراد CSV مع تقرير الأخطاء، تعديل، تعطيل |
| `/business/app/policies` | السياسات (نموذج الفئات، الأيام، النوافذ الزمنية، المناطق من `/catalog`+قائمة المناطق، الحدود، المتطلبات) |
| `/business/app/cost-centers` | مراكز التكلفة |
| `/business/app/bookings` · `/business/app/bookings/new` · `/business/app/bookings/:tripId` | الحجوزات: القائمة، حجز جديد (موظف أو ضيف، خريطة Leaflet لاختيار النقاط + بحث، التسعير وفحص السياسة، الجدولة ضمن 7 أيام)، التفاصيل الحيّة والإلغاء |
| `/business/app/invoices` · `/business/app/invoices/:id` | الفواتير وتنزيل PDF وCSV |
| `/business/app/reports` | التقارير (تجميع حسب الموظف/القسم/مركز التكلفة/الشهر مع رسم بياني) + تصدير CSV |
| `/business/app/settings` | بيانات الحساب ومفاتيح API (إن مفعّلة) |

`RequireCorporateAuth` يحمي `/business/app/*`؛ الجلسة في `localStorage` (`ata-business-session`) منفصلة عن جلسة بوابة السائق.

## F19.7 لوحة الإدارة
| المسار | الصفحة |
|---|---|
| `/corporate` | حسابات الشركات: جدول بالحالة والاستهلاك والمستحقات، إنشاء حساب |
| `/corporate/:id` | التفاصيل: البيانات القانونية، المسؤولون والموظفون، السياسات (للقراءة)، الرحلات، الفواتير، التعديلات، الإجراءات (تفعيل/تعليق/إغلاق، دعوة مسؤول) |
| `/corporate/invoices` | كل الفواتير: توليد يدوي، إصدار، تسجيل دفعة، إلغاء، PDF |

---

# F20 — الصلاحيات والتقارير

## F20.1 الجداول

| جدول | الأعمدة |
|---|---|
| `roles` | id, code VARCHAR(50) UNIQUE, name_ar, name_en, description VARCHAR(255) NULL, is_system BOOL (لا تُحذف ولا يُغيّر كودها)، created_at, updated_at |
| `permissions` | id, code VARCHAR(60) UNIQUE, module VARCHAR(30), name_ar, name_en, description NULL, sort_order — تُزامَن من الكود عند الإقلاع (للقراءة فقط) |
| `role_permissions` | id, role_id (FK)، permission_id (FK) — UNIQUE(role_id, permission_id) |
| `admin_account_roles` | id, admin_account_id (FK)، role_id (FK)، created_at — UNIQUE(admin_account_id, role_id) |
| `admin_recovery_codes` | id, admin_account_id, code_hash VARCHAR(255), used_at NULL, created_at |
| `report_snapshots` | id, snapshot_date DATE (يوم الرياض)، scope_key VARCHAR(160) (`all` · `city:{id}` · `zone:{id}` · `cat:{id}` · `city:{id}|cat:{id}` · `zone:{id}|cat:{id}`)، city_id NULL, zone_id NULL, ride_category_id NULL, metric_code VARCHAR(60), value DECIMAL(18,4), numerator DECIMAL(18,4) NULL, denominator DECIMAL(18,4) NULL, computed_at — UNIQUE(snapshot_date, scope_key, metric_code)، INDEX(metric_code, snapshot_date) |

تعديلات:
- `admin_accounts` (+): `failed_login_count INT`, `locked_until NULL`, `must_change_password BOOL`, `password_changed_at NULL`, `mfa_enrolled_at NULL`, `mfa_last_step BIGINT NULL` (منع إعادة استخدام رمز TOTP)، `mfa_failed_count INT`. العمود `permissions JSON` القائم يُهمل (يبقى للتوافق فقط؛ المصدر = الأدوار).
- `refresh_tokens` (+): `session_kind` (`app`/`admin`/`corporate`)، `user_agent VARCHAR(255) NULL`, `last_used_at NULL`, `absolute_expires_at NULL`.

## F20.2 كتالوج الصلاحيات (مرجع موحّد)

| الكود | الوحدة | يغطي |
|---|---|---|
| `dashboard.view` | dashboard | `/admin/dashboard/summary` |
| `drivers.view` | drivers | قوائم وتفاصيل السائقين |
| `drivers.review` | drivers | بدء المراجعة، الاعتماد، الرفض، التعليق، الإعادة، التحقق من المستندات |
| `passengers.view` | passengers | قائمة الركاب |
| `users.suspend` | users | تعليق/إعادة المستخدمين |
| `catalog.manage` | catalog | كتابة فئات الرحلات وأنواع المستندات |
| `trips.view` | trips | الرحلات، التفاصيل، المطابقة، سجل الإلغاءات |
| `trips.cancel` | trips | إلغاء رحلة إدارياً |
| `live.view` | trips | الخريطة المباشرة |
| `pricing.view` | pricing | قراءة المناطق والتسعير والطلب والمطابقة |
| `pricing.edit` | pricing | كتابة المناطق وقواعد التسعير والطلب والتجاوزات والمحاكاة |
| `matching.edit` | matching | كتابة إعدادات المطابقة |
| `payments.view` | payments | الدفعات، الاستردادات، السحوبات، المحافظ، الدفتر |
| `payments.refund` | payments | إنشاء استرداد |
| `payments.refund_approve` | payments | اعتماد/رفض/إعادة الاسترداد |
| `payouts.approve` | payments | اعتماد السحوبات ودفعات التحويل |
| `settlements.manage` | payments | التسويات |
| `wallets.adjust` | payments | التعديل اليدوي وتجميد المحافظ |
| `notifications.view` | notifications | الكتالوج، القوالب (قراءة)، سجل الإرسال |
| `notifications.manage` | notifications | تعديل القوالب، الحملات، إعادة الإرسال |
| `notifications.sms_broadcast` | notifications | حملات SMS |
| `safety.manage` | safety | حالات السلامة، التنبيهات، المفقودات، المناوبة |
| `cancellation.manage` | cancellation | الأسباب، القواعد، العتبات |
| `cancellation.review` | cancellation | مراجعة الأعذار |
| `reliability.manage` | cancellation | ملفات الموثوقية والتعديل اليدوي |
| `ratings.manage` | ratings | التقييمات والبلاغات |
| `promotions.manage` | promotions | العروض |
| `incentives.manage` | incentives | الحوافز ومستويات السائقين |
| `favorites.manage` | favorites | قواعد خصم المفضل |
| `scheduling.manage` | scheduling | قواعد الجدولة والرحلات المجدولة |
| `airport.manage` | airport | المطارات والطابور |
| `support.view` | support | قراءة التذاكر والنزاعات |
| `support.manage` | support | الرد والتعيين والحالات والردود الجاهزة وSLA |
| `support.disputes` | support | حل نزاعات الأجرة |
| `help.manage` | support | مقالات المساعدة |
| `corporate.manage` | corporate | حسابات الشركات والفواتير |
| `reports.view` | reports | مؤشرات الأداء والإحصاءات |
| `reports.export` | reports | تصدير CSV وإعادة بناء اللقطات |
| `admin.users.manage` | admin | مستخدمو الإدارة |
| `admin.roles.manage` | admin | الأدوار وصلاحياتها |
| `audit.view` | admin | سجل التدقيق |

القيمة `*` تعني كل الصلاحيات (دور `super_admin`).

ربط النقاط القائمة (F1–F10): قراءة `/admin/drivers*` → `drivers.view`، كتابتها و`/admin/documents/{id}/verify` → `drivers.review`؛ `/admin/passengers` → `passengers.view`؛ `/admin/users/{id}/suspend|reinstate` → `users.suspend`؛ كتابة `/admin/ride-categories` → `catalog.manage` (القراءة لأي إداري)؛ `/admin/audit-logs` → `audit.view`؛ `/admin/trips*` قراءة → `trips.view`، `/cancel` → `trips.cancel`؛ `/admin/live` → `live.view`؛ `/admin/zones|pricing-rules|demand-*|pricing/simulate|demand/current` قراءة → `pricing.view` وكتابة → `pricing.edit`؛ `/admin/matching-settings` قراءة → `pricing.view` وكتابة → `matching.edit`؛ `/admin/matching/stats` → `reports.view`.

## F20.3 قواعد التفويض
- كل نقاط `/admin/*` تبقى تحت `Policies.Admin` (الدوران `admin`/`operations`) + مرشح `RequirePermission("<code>")` لكل نقطة. نقص الصلاحية → `403 forbidden` مع `details { permission }`.
- JWT للإدارة: `roles: ["admin"]` لكل حساب إداري، و`perm` = اتحاد صلاحيات أدواره (أو `["*"]`). تغيير أدوار مستخدم أو تعطيله يلغي كل `refresh_tokens` له (يعيد الدخول)؛ الوصول الحالي ينتهي خلال `Admin:AccessTokenMinutes`.
- لوحة الإدارة تستخدم `GET /admin/me` لإخفاء عناصر الشريط الجانبي والأزرار غير المسموحة (والخلفية هي المرجع).
- حماية: لا يمكن للمستخدم تعطيل نفسه أو إزالة دور `super_admin` عن آخر حامل له (`409 conflict { reason: "last_super_admin" }`)؛ الأدوار `is_system` لا تُحذف.

## F20.4 MFA (TOTP) والجلسات
- TOTP وفق RFC 6238: SHA-1، 6 أرقام، 30 ث، نافذة ±1 خطوة؛ سر 20 بايت Base32 مشفّر بـData Protection في `admin_accounts.mfa_secret`؛ رفض الخطوة ≤ `mfa_last_step` (منع الإعادة). `otpauth://totp/ATA%20Admin:{username}?secret={secret}&issuer=ATA%20Admin&digits=6&period=30`.
- رموز الاسترداد: 10 رموز بصيغة `xxxx-xxxx` (base32 صغيرة)، تُخزَّن مجزأة (`PasswordHasher`)، استخدام واحد، تُعرض مرة واحدة.
- الإلزام: `Admin:MfaRequired` (`true` افتراضياً، و`false` في `appsettings.Development.json` لحساب البذور).
- `mfaToken`: رمز Data Protection موقّع قصير العمر (`Admin:MfaTokenMinutes` = 5) يحمل `adminAccountId` والمرحلة (`verify`/`enroll`).
- القفل: `Admin:MaxFailedLogins` (5) كلمات مرور خاطئة → `locked_until = now + Admin:LockoutMinutes` (15) → `429 account_locked { retryAfterSeconds }`؛ 5 رموز MFA خاطئة لنفس `mfaToken` → `429 mfa_locked` (يلزم الدخول من جديد).
- سياسة كلمة المرور: ≥ 12 حرفاً، حرف كبير وصغير ورقم ورمز (`422 password_policy_violation { rules: [...] }`). كلمة مؤقتة → `must_change_password = true` ولا يُسمح إلا بـ`/admin/me/password` حتى التغيير (`403 password_change_required`).
- الجلسات الإدارية: وصول `Admin:AccessTokenMinutes` (15)، حد مطلق `Admin:SessionAbsoluteHours` (12)، خمول `Admin:SessionIdleMinutes` (30) — تجديد بعد خمول أطول أو بعد الحد المطلق → `401 unauthorized`.

## F20.5 الـAPI — الهوية والصلاحيات

### تسجيل دخول الإدارة (تعديل §1 في `05`)
- POST `/auth/admin/login` `{ "username", "password" }` → أحد:
  - `AuthResponse` (+ `"mustChangePassword": false`) إن لم تكن MFA مطلوبة/مفعّلة.
  - `200 { "mfaRequired": true, "mfaToken": "…", "methods": ["totp", "recovery_code"] }`
  - `200 { "mfaEnrollmentRequired": true, "mfaToken": "…" }`
  - `401 invalid_credentials` · `429 account_locked` · `403 account_suspended` (معطّل).
- POST `/auth/admin/mfa/verify` `{ "mfaToken", "code"?: "123456", "recoveryCode"?: "abcd-efgh" }` → `AuthResponse` (+ `"recoveryCodesRemaining": 9`)؛ `400 mfa_invalid { attemptsLeft }`، `429 mfa_locked`، `401 unauthorized` (رمز منتهٍ).
- POST `/auth/admin/mfa/enroll` `{ "mfaToken" }` → `{ "secret": "JBSW…", "otpauthUri": "otpauth://…" }`
- POST `/auth/admin/mfa/enroll/confirm` `{ "mfaToken", "code" }` → `{ "recoveryCodes": ["…"], "auth": AuthResponse }`

### الإدارة الذاتية `/admin/me` (أي إداري)
- GET `/admin/me` → `{ "adminAccountId", "userId", "username", "fullName", "roles": [ { "id", "code", "name" } ], "permissions": ["trips.view", …] | ["*"], "mfaEnabled": true, "mustChangePassword": false, "onDuty": false }`
- POST `/admin/me/password` `{ "currentPassword", "newPassword" }` → `204` (يلغي الجلسات الأخرى).
- POST `/admin/me/mfa/recovery-codes` `{ "code" }` → `{ "recoveryCodes": [...] }` (يستبدل القديمة).
- GET `/admin/me/sessions` → `[ { "id", "userAgent", "ipAddress", "createdAt", "lastUsedAt", "current": true } ]`؛ DELETE `/admin/me/sessions/{id}` → `204`؛ POST `/admin/me/sessions/revoke-others` → `204`.
- GET/PUT `/admin/me/duty` (معرّف في `08`).

### المستخدمون والأدوار
| المسار | الصلاحية |
|---|---|
| GET `/admin/admin-users?search=&roleId=&isActive=&page=` → `{ id, userId, username, fullName, phoneNumber, roles, isActive, mfaEnabled, lastLoginAt, onDuty, lockedUntil }` | `admin.users.manage` |
| POST `/admin/admin-users` `{ username, fullName, phoneNumber, roleIds, temporaryPassword? }` → `201 { adminUser, temporaryPassword }` (يُنشئ/يربط `users` بالجوال + `user_roles` `admin` + `admin_accounts` بـ`must_change_password`) | `admin.users.manage` |
| PUT `/admin/admin-users/{id}` `{ fullName, roleIds }` | `admin.users.manage` |
| POST `/admin/admin-users/{id}/disable` · `/enable` · `/reset-password` (→ `{ temporaryPassword }`) · `/reset-mfa` · `/unlock` · `/revoke-sessions` | `admin.users.manage` |
| GET `/admin/permissions` → `[ { code, module, name, description } ]` | `admin.roles.manage` |
| GET/POST `/admin/roles`، GET/PUT/DELETE `/admin/roles/{id}` (`{ code, nameAr, nameEn, description, permissionCodes: [] }`) | `admin.roles.manage` |

Audit: `admin_user.create|update|disable|enable|reset_password|reset_mfa|unlock|revoke_sessions`, `role.create|update|delete`، وأحداث الأمان `admin.login`, `admin.login_failed`, `admin.mfa_enrolled`, `admin.mfa_failed`, `admin.password_changed` (في `audit_logs` مع `ip_address`).

### أكواد الأخطاء (F20)
| الكود | HTTP | ar |
|---|---|---|
| `mfa_invalid` | 400 | رمز التحقق الثنائي غير صحيح |
| `mfa_locked` | 429 | تجاوزت عدد المحاولات، سجّل الدخول مجدداً |
| `account_locked` | 429 | الحساب مقفل مؤقتاً بسبب محاولات خاطئة |
| `password_policy_violation` | 422 | كلمة المرور لا تستوفي المتطلبات |
| `password_change_required` | 403 | يجب تغيير كلمة المرور أولاً |
| `report_range_too_large` | 422 | نطاق التقرير أكبر من المسموح |

## F20.6 مؤشرات الأداء (تعريفات موحّدة)

الأبعاد: المدينة (مدينة منطقة الالتقاط)، المنطقة (منطقة الالتقاط `pickup_zone_id` من `fare_quotes` أو `ZoneResolver`)، فئة الرحلة. اليوم = يوم الرياض. نوع التجميع يحدد كيف تُجمع الأيام: `sum` (جمع القيم)، `ratio` (Σ البسط ÷ Σ المقام)، `avg` (Σ المجموع ÷ Σ العدد، مخزنان كبسط/مقام)، `distinct` (يُحسب مباشرة من الجداول للنطاق المطلوب؛ اللقطة اليومية للعرض اليومي فقط).

| الكود | الاسم | الوحدة | التجميع | التعريف |
|---|---|---|---|---|
| `completed_trips` | Completed Trips | count | sum | رحلات `completed` بـ`completed_at` في اليوم |
| `requested_trips` | Requested Trips | count | sum | رحلات أُنشئت (`requested_at`) باستثناء المجدولة التي لم يحل موعدها |
| `completion_rate` | Completion Rate | percent | ratio | المكتملة ÷ (المكتملة + الملغاة + `no_drivers`) للرحلات المنتهية في اليوم |
| `trips_per_active_rider` | Trips per Active Rider | ratio | distinct | `completed_trips` ÷ `active_riders` |
| `average_eta` | Average ETA | seconds | avg | متوسط `arrived_at − assigned_at` للرحلات التي وصل سائقها |
| `average_time_to_assign` | Time to Assign | seconds | avg | متوسط `assigned_at − requested_at` (الفورية) |
| `no_drivers_rate` | No Drivers Rate | percent | ratio | `no_drivers` ÷ الرحلات المنتهية |
| `driver_acceptance_rate` | Driver Acceptance Rate | percent | ratio | عروض `accepted` ÷ العروض المُجاب عنها أو المنتهية (`accepted`+`rejected`+`expired`) |
| `driver_cancellation_rate` | Driver Cancellation Rate | percent | ratio | تعريف `09` §F14.7 |
| `passenger_cancellation_rate` | Passenger Cancellation Rate | percent | ratio | تعريف `09` §F14.7 |
| `average_fare` | Average Fare | sar | ratio | Σ `final_fare` ÷ `completed_trips` |
| `driver_earnings_per_online_hour` | Driver Earnings per Online Hour | sar | ratio | Σ `driver_earnings` + Σ الحوافز المصروفة ÷ Σ ساعات الاتصال (من `driver_status_logs`) |
| `online_hours` | Online Hours | hours | sum | Σ ساعات الاتصال |
| `gmv` | GMV | sar | sum | Σ (`final_fare + discount_total`) للمكتملة + Σ رسوم الإلغاء المحصّلة |
| `platform_revenue` | Platform Revenue | sar | sum | العمولة Σ(`final_fare + discount_total − driver_earnings`) + (رسوم الإلغاء − التعويضات) − الخصومات (`discount_promotion` + `discount_favorite_driver`) − الاستردادات الناجحة |
| `take_rate` | Take Rate | percent | ratio | `platform_revenue` ÷ `gmv` |
| `incentives_paid` | Incentives Paid | sar | sum | Σ حركات `incentive` |
| `refunds_amount` | Refunds | sar | sum | Σ الاستردادات `succeeded` |
| `active_riders` | Active Riders | count | distinct | ركاب لهم ≥ 1 رحلة مكتملة في النطاق |
| `active_drivers` | Active Drivers | count | distinct | سائقون لهم ≥ 1 رحلة مكتملة في النطاق |
| `new_riders` | New Riders | count | sum | ركاب أول رحلة مكتملة لهم في اليوم |
| `repeat_rate` | Repeat Rate | percent | distinct | ركاب لهم ≥ 2 رحلة مكتملة في النطاق ÷ `active_riders` |
| `customer_rating` | Customer Rating | rating | avg | متوسط نجوم تقييمات الركاب للسائقين (`created_at` في اليوم) |
| `support_resolution_time` | Support Resolution Time | hours | avg | تعريف `11` §F18.5 |
| `cancellation_fee_revenue` | Cancellation Fee Revenue | sar | sum | تعريف `09` §F14.7 |
| `repeat_cancellation_rate` | Repeat Cancellation Rate | percent | distinct | تعريف `09` §F14.7 |
| `driver_reliability_rate` | Driver Reliability Rate | percent | ratio | رحلات مكتملة ÷ رحلات أُسندت لسائقين (بما فيها حجوزات المجدولة) |
| `passenger_reliability_rate` | Passenger Reliability Rate | percent | ratio | رحلات مكتملة ÷ رحلات أُسند لها سائق |
| `favorite_driver_booking_rate` | Favorite Driver Booking Rate | percent | ratio | مكتملة بـ`favorite_status = accepted` ÷ `completed_trips` |
| `favorite_driver_discount_usage` | Favorite Driver Discount Usage | sar | sum | Σ خصم `favorite_driver` (والعدد في `numerator`) |
| `scheduled_ride_completion_rate` | Scheduled Ride Completion Rate | percent | ratio | تعريف `11` §F17.5 |
| `scheduled_ride_cancellation_rate` | Scheduled Ride Cancellation Rate | percent | ratio | تعريف `11` §F17.5 |

## F20.7 الـAPI — التقارير (`reports.view`؛ التصدير `reports.export`)
- GET `/admin/reports/kpis?from=2026-09-01&to=2026-09-28&cityId=&zoneId=&rideCategoryId=&compare=previous_period` →
```json
{ "from": "2026-09-01", "to": "2026-09-28", "filters": { "cityId": null, "zoneId": null, "rideCategoryId": null },
  "metrics": [ { "code": "completed_trips", "name": "الرحلات المكتملة", "unit": "count", "value": 12840, "previousValue": 11020,
                 "changePercent": 16.5, "numerator": null, "denominator": null } ] }
```
  النطاق ≤ `Reports:MaxRangeDays` (366) وإلا `422 report_range_too_large { maxDays }`. `metrics=completed_trips,gmv` اختياري لتقليل الحقول.
- GET `/admin/reports/kpis/{code}/series?from=&to=&granularity=day|week|month&cityId=&zoneId=&rideCategoryId=` → `{ "code", "unit", "points": [ { "periodStart": "2026-09-01", "value", "numerator", "denominator" } ] }`.
- GET `/admin/reports/breakdown?metric=&from=&to=&groupBy=city|zone|category` → `{ "metric", "rows": [ { "key", "label", "value", "numerator", "denominator" } ] }`.
- GET `/admin/reports/export?dataset=&from=&to=&cityId=&zoneId=&rideCategoryId=&format=csv` (بث CSV UTF-8 مع BOM، حتى `Reports:MaxExportRows` = 100000 وإلا `422 report_range_too_large`). مجموعات البيانات وأعمدتها:
  - `kpis`: `date, scope, metric_code, value, numerator, denominator`
  - `trips`: `trip_number, requested_at, completed_at, status, category, city, zone, passenger_id, driver_id, booking_type, payment_method, distance_km, duration_min, estimated_fare, final_fare, discount_total, driver_earnings, cancelled_by, cancellation_reason, corporate_account`
  - `payments`: `payment_id, created_at, purpose, method, provider, status, amount, captured_amount, refunded_amount, trip_number, gateway_payment_id`
  - `payouts`: `payout_number, requested_at, driver, amount, status, approved_at, paid_at, bank_reference, batch_number`
  - `cancellations`: `trip_number, created_at, actor, at_fault, stage, reason_code, fee_amount, fee_charged, fee_status, compensation, penalty_points, excuse_status`
  - `ratings`: `trip_number, created_at, rater_role, stars, tags, has_comment`
  - `support_tickets`: `ticket_number, created_at, type, priority, status, first_response_minutes, resolution_hours, sla_met, csat`
  - `drivers`: `driver_id, application_number, status, tier, rating_avg, city, approved_at, completed_trips, online_hours, reliability_level`
  - `incentives`: `incentive, driver, period_start, completed_trips, status, reward_amount, paid_at`
  (معرّفات المستخدمين UUID بدل الأسماء والجوالات في التصدير — تقليل البيانات الشخصية.)
- POST `/admin/reports/snapshots/rebuild` `{ "from", "to" }` → `202` (`reports.export`، audit `report_snapshots.rebuild`).
- GET `/admin/dashboard/summary` (قائم) يضاف إليه `today: { completedTrips, gmv, activeDrivers, onlineDrivers, avgTimeToAssignSeconds, openSafetyCases, openTickets }`.

## F20.8 المهام والإعدادات (F20)
| المهمة | الجدولة | العمل |
|---|---|---|
| `ReportSnapshotJob` | يومياً 01:30 الرياض | حساب مقاييس `sum`/`ratio`/`avg` (وقيم `distinct` اليومية) لليوم السابق لكل `scope_key` (upsert)، وإعادة حساب آخر `Reports:RecomputeTrailingDays` (3) أيام لالتقاط التحديثات المتأخرة (تقييمات، استردادات) |
| `AdminSessionCleanupJob` | كل ساعة | إلغاء جلسات الإدارة المنتهية بالخمول/الحد المطلق |

| المفتاح | الافتراضي |
|---|---|
| `Admin:MfaRequired` / `Admin:MfaTokenMinutes` | `true` / 5 |
| `Admin:MaxFailedLogins` / `Admin:LockoutMinutes` | 5 / 15 |
| `Admin:AccessTokenMinutes` / `Admin:SessionAbsoluteHours` / `Admin:SessionIdleMinutes` | 15 / 12 / 30 |
| `Reports:MaxRangeDays` / `Reports:MaxExportRows` / `Reports:RecomputeTrailingDays` | 366 / 100000 / 3 |

## F20.9 لوحة الإدارة
| المسار | الصفحة |
|---|---|
| `/login` (تعديل) | خطوة MFA (6 خانات أو رمز استرداد)، وخطوة التسجيل (QR من `otpauthUri` عبر مكتبة `qrcode` + تأكيد + عرض رموز الاسترداد مع تنزيل)، وتغيير كلمة المرور الإجباري |
| `/reports` | لوحة KPIs: فلاتر (نطاق التاريخ، المدينة، المنطقة، الفئة، المقارنة)، بطاقات المؤشرات بتغيّر النسبة، رسوم زمنية (Recharts) للمؤشر المختار، جدول التوزيع حسب المنطقة/الفئة، مجموعات v1.1 في قسم مستقل |
| `/reports/exports` | تصدير مجموعات البيانات CSV |
| `/admin-users` و`/admin-users/:id` | المستخدمون: إنشاء، الأدوار، تعطيل، إعادة كلمة المرور/MFA، فك القفل، الجلسات |
| `/roles` و`/roles/:id` | الأدوار: مصفوفة صلاحيات مجمّعة حسب الوحدة |
| `/account/security` | حسابي: تغيير كلمة المرور، رموز الاسترداد، الجلسات النشطة |

الشريط الجانبي (`nav.ts`) يُرشَّح حسب `permissions` من `/admin/me`، والمسارات محمية بمكوّن `RequirePermission`.

---

# F21 — الجاهزية التشغيلية

## F21.1 السجلات (Serilog)
- Serilog مع `CompactJsonFormatter` إلى stdout في الحاويات (قابل للتحويل عبر `Serilog:*` في الإعدادات؛ Seq اختياري في التطوير).
- **معرّف الارتباط**: ترويسة `X-Correlation-Id` (تُقبل من العميل إن كانت UUID، وإلا تُولَّد) وتُعاد في كل استجابة؛ تُضاف لكل سجل مع `traceId`/`spanId` (OpenTelemetry)، `userId`، `role`، `requestPath`، `statusCode`، `elapsedMs`، `module`. عملاء Flutter والموقع واللوحة يرسلون UUID لكل طلب ويعرضونه في رسائل الخطأ العامة ("رمز المرجع").
- `UseSerilogRequestLogging` بمستوى Warning لـ4xx وError لـ5xx.
- **حجب البيانات الشخصية**: أرقام الجوال تُقنَّع (`+9665****5678`)، ولا تُسجَّل أبداً: رموز OTP (عدا `devCode` في التطوير)، التوكنات، رموز البطاقات، IBAN، رقم الهوية، رسائل المحادثة، أسرار الإعدادات. سياسة `IDestructuringPolicy` + قائمة حقول محجوبة (`password`, `code`, `token`, `refreshToken`, `iban`, `nationalId`, `gatewayToken`, `body`).

## F21.2 المقاييس والتتبع (OpenTelemetry)
- التتبع: ASP.NET Core، HttpClient، EF Core، StackExchange.Redis، SignalR؛ المصدّر OTLP إن `Observability:OtlpEndpoint` مضبوط.
- المقاييس: runtime + aspnetcore + Meter مخصص `ATA`، ونقطة Prometheus `GET /metrics` (`MapPrometheusScrapingEndpoint`) — محجوبة من nginx العام، ومقيدة بـ`Observability:MetricsAllowedCidrs`.
- المقاييس المخصصة:

| الاسم | النوع | الوسوم |
|---|---|---|
| `ata_trips_requested_total` | counter | `category`, `booking_type` |
| `ata_trips_completed_total` · `ata_trips_cancelled_total` | counter | `category`, `cancelled_by` |
| `ata_matching_time_to_assign_seconds` | histogram | `category` |
| `ata_offers_total` | counter | `outcome` |
| `ata_online_drivers` | gauge | `city` |
| `ata_payments_total` | counter | `provider`, `purpose`, `status` |
| `ata_payment_gateway_duration_seconds` | histogram | `provider`, `operation` |
| `ata_webhooks_total` | counter | `provider`, `result` |
| `ata_notifications_total` | counter | `channel`, `status` |
| `ata_sms_total` | counter | `provider`, `status` |
| `ata_safety_alerts_total` · `ata_sos_total` | counter | `type` |
| `ata_background_job_runs_total` · `ata_background_job_duration_seconds` | counter/histogram | `job`, `result` |
| `ata_signalr_connections` | gauge | `hub` |

## F21.3 الصحة
- `GET /health/live` → `200 { "status": "Healthy" }` (العملية فقط، بلا تبعيات).
- `GET /health/ready` → فحوص: MySQL، Redis (إن مضبوط)، التخزين قابل للكتابة، إعدادات OneSignal/الدفع موجودة في الإنتاج. `503` عند الفشل مع `{ "status": "Unhealthy", "checks": [ { "name", "status", "durationMs" } ] }`.
- `GET /health` (قائم) = مرادف لـ`/health/ready`.

## F21.4 التنبيهات (إرشادات)
| التنبيه | الشرط | الخطورة |
|---|---|---|
| أخطاء الخادم | نسبة 5xx > 2% لـ5 د | حرج |
| زمن الاستجابة | p95 > 1 ث لـ10 د (عدا التصدير) | عالٍ |
| الجاهزية | `/health/ready` يفشل لـ2 د | حرج |
| المطابقة | `no_drivers_rate` > 30% لـ15 د، أو p90 زمن الإسناد > 90 ث | عالٍ |
| المدفوعات | نسبة فشل الدفع > 10% لـ15 د، أو أي `webhook_signature_invalid` | عالٍ |
| الإشعارات | فشل push أو SMS > 20% لـ15 د | متوسط |
| مهمة خلفية متوقفة | آخر نجاح > 3 × الفاصل المتوقع | عالٍ |
| SOS غير معيّن | حالة `critical` بلا `first_response_at` بعد 2 د | حرج (للعمليات) |
| قاعدة البيانات | الاتصالات > 80%، المساحة > 80%، تأخر النسخ المتماثل > 60 ث | عالٍ |
| Redis | غير متاح لـ1 د | عالٍ |
| النسخ الاحتياطي | فشل نسخة يومية أو عدم وصول binlog لـ30 د | حرج |

## F21.5 Redis
- `Redis:ConnectionString` (فارغ = تنفيذات الذاكرة في التطوير والاختبارات). البادئة `Redis:KeyPrefix` = `ata:{env}:`.
- **الأقفال الموزعة** `IDistributedLock.TryAcquireAsync(string key, TimeSpan ttl, CancellationToken) → IAsyncDisposable?` (تنفيذ `SET key token NX PX ttl` + تحرير بـLua يطابق الرمز + تجديد تلقائي للمهام الطويلة). المفاتيح: `lock:trip:{tripId}` (المطابقة وانتقالات الحالة المتزامنة)، `lock:job:{jobName}` (مهمة واحدة عبر كل النسخ)، `lock:payout:{driverId}`، `lock:promo:{promotionId}`.
- **تحديد المعدل الموزع**: `RateLimiting:Provider` = `memory|redis` (نافذة منزلقة بـLua). السياسات:

| السياسة | الحد | التقسيم |
|---|---|---|
| `otp` (قائم) | 3/10 د لكل رقم + `RateLimiting:OtpPerIpPerHour` (10) لكل IP | الرقم / IP |
| `global-user` | `RateLimiting:UserPerMinute` (300) | المستخدم |
| `global-anonymous` | `RateLimiting:AnonymousPerMinute` (120) | IP |
| `admin-login` | 10 / 15 د | IP |
| `public-share` | `Safety:PublicShareRatePerMinute` (30) | IP |
| `help` | 60 / د | IP |
| `webhooks` | مستثناة (التحقق بالتوقيع) | — |

- **SignalR backplane**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` بقناة `ata:{env}:signalr` عند وجود Redis.
- **الكاش**: كاش المناطق والقوالب والصلاحيات يبقى في الذاكرة مع إبطال عبر Redis pub/sub (`ata:{env}:invalidate`) عند التعديل في أي نسخة.

## F21.6 أمان HTTP
- ضغط الاستجابات: Brotli ثم Gzip لـ`application/json` و`text/*` (مستثنى SignalR وملفات `/files`).
- ترويسات الـAPI: `X-Content-Type-Options: nosniff`، `X-Frame-Options: DENY`، `Referrer-Policy: no-referrer`، `Strict-Transport-Security: max-age=31536000; includeSubDomains` (خارج التطوير)، `Cache-Control: no-store` للاستجابات المصادقة، إزالة ترويسة `Server`. HTTPS إلزامي خلف nginx (`ForwardedHeaders`).
- الموقع واللوحة (nginx): CSP `default-src 'self'; script-src 'self' https://cdn.onesignal.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: blob: https://*.tile.openstreetmap.org; connect-src 'self' https://api.ata.sa wss://api.ata.sa https://*.onesignal.com; frame-ancestors 'none'`، و`Permissions-Policy: geolocation=(self), camera=(), microphone=()`.
- CORS: `Cors:Origins` (قائم) بأصول الإنتاج فقط.
- حد حجم الطلب: 12MB (الرفع 10MB).

## F21.7 الاحتفاظ بالبيانات
`DataRetentionJob` يومياً 03:30 الرياض، حذف على دفعات `Retention:BatchSize` (5000) مع توقف قصير بين الدفعات:

| البيانات | المفتاح | الافتراضي |
|---|---|---|
| `driver_location_history` | `Retention:LocationHistoryDays` | 90 |
| `otp_requests` | `Retention:OtpDays` | 30 |
| `trip_messages` | `Retention:TripMessagesDays` | 180 |
| `notifications` (المقروءة) | `Retention:NotificationsDays` | 365 |
| `notification_deliveries` | `Retention:NotificationDeliveriesDays` | 90 |
| `payment_webhook_events` | `Retention:WebhookEventsDays` | 365 |
| `matching_attempts` / `matching_candidates` | `Retention:MatchingDays` | 90 |
| `demand_snapshots` | `Demand:SnapshotRetentionHours` (قائم) | 48 |
| `refresh_tokens` المنتهية/الملغاة | `Retention:RefreshTokensDays` | 30 |
| `safety_alerts` المحلولة | `Retention:SafetyAlertsDays` | 730 |
| `airport_queue_entries` المنتهية | `Retention:AirportQueueDays` | 30 |
| `audit_logs`، الدفتر المالي، الرحلات، الفواتير | لا تُحذف آلياً (متطلبات مالية/تنظيمية؛ المدة النهائية تُحدد قانونياً) | — |

## F21.8 الخصوصية (PDPL)

### حذف الحساب (توسعة `DELETE /me`)
| جدول | الأعمدة |
|---|---|
| `account_deletion_requests` | id, user_id, requested_at, scheduled_for (`requested_at + Privacy:DeletionGraceDays` = 30)، status (`pending`/`blocked`/`cancelled`/`completed`)، blockers JSON NULL، support_ticket_id NULL, cancelled_at NULL, completed_at NULL, created_at, updated_at |

- `DELETE /me` → `202 { "requestId", "scheduledFor", "blockers": [] }` (يلغي التوكنات فوراً كالقائم ويرسل `account.deletion_scheduled`).
- المعوقات (تُفحص عند الطلب وعند التنفيذ): رحلة نشطة (`active_trip`)، رصيد محفظة سالب (`outstanding_balance`)، رصيد محفظة موجب (`wallet_balance` — تُنشأ تذكرة دعم لتسوية الرصيد)، سحب قيد المعالجة (`pending_payout`)، نزاع أو حالة سلامة مفتوحة (`open_dispute`, `open_safety_case`)، عضوية مسؤول وحيد لشركة (`sole_corporate_admin`). وجود معوق عند التنفيذ → `blocked` حتى زواله. الرحلات المجدولة تُلغى مجاناً عند الطلب.
- الدخول بـOTP خلال فترة السماح يلغي الطلب (`cancelled`) مع إشعار.
- `AccountDeletionJob` (يومياً 04:30) للطلبات المستحقة غير المعاقة — **إخفاء الهوية** في معاملة واحدة:
  - `users`: `phone_number = 'deleted:' + first16(sha256(id))`، `full_name = NULL`، `gender = unknown`، `status = deleted`، `deleted_at = now`.
  - حذف: `saved_places`, `trusted_contacts`, `favorite_drivers` (كطرف)، `user_devices`, `refresh_tokens`, `notification_preferences`, `payment_methods` (مع `DeleteCardTokenAsync`)، `notifications`، `corporate_users` (تعطيل + مسح الجوال).
  - تنظيف: `ratings.comment = NULL` لما كتبه، `trip_messages.body = '[محذوف]'` لما أرسله، `support_messages.body` لما كتبه → `[محذوف]`، `trips.rider_note = NULL`، `trips.guest_phone = NULL` لرحلات الضيوف التي حجزها.
  - السائق: `drivers.national_id`, `iban`, `date_of_birth` → NULL؛ ملفات المستندات تُحذف من التخزين بعد `Privacy:DriverDocumentsRetentionDays` (افتراضياً 0 = فوراً، قابل للتمديد قانونياً) ويُحتفظ بصفوف `driver_documents` بلا ملف.
  - يُحتفظ بالسجلات المالية والرحلات والتدقيق مرتبطة بالمعرّف المُجهَّل.
  - OneSignal: `DELETE /apps/{appId}/users/by/external_id/{userId}` عبر `IPushSender.DeleteUserAsync`.
  - audit `user.anonymize` (actor system).

### تصدير البيانات
| جدول | الأعمدة |
|---|---|
| `data_export_requests` | id, user_id, status (`queued`/`processing`/`ready`/`failed`/`expired`)، file_id NULL, expires_at NULL (`Privacy:ExportExpiryDays` = 7)، error NULL, requested_at, completed_at NULL |

- POST `/me/data-exports` → `202 { "requestId", "status": "queued" }` (طلب واحد كل 24 س وإلا `429 rate_limited`).
- GET `/me/data-exports` → `[ { "id", "status", "requestedAt", "completedAt", "expiresAt", "fileId" } ]`؛ التنزيل عبر `GET /files/{fileId}` (المالك فقط).
- `DataExportJob`: ZIP يحتوي JSON (`profile.json`, `trips.json`, `payments.json`, `wallet_transactions.json`, `ratings_given.json`, `notifications.json`, `support_tickets.json`, `safety_reports.json`, `trusted_contacts.json`, `saved_places.json`) + `README.txt` ثنائي اللغة → `stored_files` → إشعار `privacy.export_ready`. الحذف عند الانتهاء.

### Flutter — صفحة الخصوصية
- feature `account` (توسعة): usecases `RequestDataExport`, `GetDataExports`؛ cubit `PrivacyCubit` (طلب التصدير، الحالة، التنزيل وفتح الملف، حالة طلب الحذف والمعوقات من استجابة `DELETE /me`)؛ الصفحة `/account/privacy` ("الخصوصية والأمان": تنزيل بياناتي، سياسة الخصوصية، حذف الحساب). `DeleteAccountCubit` القائم يعرض `scheduledFor` والمعوقات.

## F21.9 النسخ الاحتياطي والتعافي (محتوى الـRunbooks)

تسليم F21 يُنشئ ثلاثة ملفات من هذا القسم: `docs/runbooks/backup-restore.md`، `docs/runbooks/disaster-recovery.md`، `docs/runbooks/incident-response.md`.

**الأهداف**: RPO ≤ 15 دقيقة، RTO ≤ 4 ساعات لقاعدة البيانات؛ الاستضافة والنسخ داخل المملكة.

**النسخ الاحتياطي**:
1. MySQL: نسخة كاملة يومية 02:00 (Percona XtraBackup أو لقطات الخدمة المُدارة) + binlog مستمر (`binlog_format=ROW`, `binlog_expire_logs_seconds` = 7 أيام) يُرفع كل 5 دقائق للتخزين الكائني.
2. الاحتفاظ: يومية 35 يوماً، شهرية 12 شهراً؛ مشفّرة (AES-256) في حساب/حاوية منفصلة بصلاحيات كتابة فقط (immutable/object-lock).
3. التخزين الكائني (المستندات، الفواتير، التصدير): versioning + نسخ عبر منطقتين داخل المملكة.
4. مفاتيح Data Protection (`/keys`) وأسرار البيئة: ضمن مخزن الأسرار مع نسخة مشفّرة؛ **فقدانها يُبطل PINs الجارية ورموز MFA المشفّرة**.
5. Redis: لا يُنسخ (أقفال/حدود/كاش قابلة لإعادة البناء).

**الاستعادة** (`backup-restore.md`): (1) إعلان الحادث وتجميد النشر؛ (2) إنشاء نسخة MySQL جديدة من آخر كاملة؛ (3) تطبيق binlog حتى النقطة المطلوبة (`mysqlbinlog --stop-datetime`)؛ (4) التحقق: عدد الصفوف للجداول الرئيسية، ثابت توازن الدفتر (`SUM(debit) = SUM(credit)`)، آخر `trip_number`؛ (5) توجيه `ConnectionStrings__Default` وإعادة تشغيل الـAPI؛ (6) تشغيل `api-migrate` للتأكد من المخطط؛ (7) مراجعة المدفوعات عبر مطابقة `payments` مع البوابة للفترة المفقودة.

**اختبار الاستعادة**: شهرياً على بيئة معزولة مع تسجيل الزمن الفعلي مقابل RTO.

**التعافي من الكوارث** (`disaster-recovery.md`): نسخة MySQL متماثلة في منطقة ثانوية؛ صور الحاويات في سجل داخل المملكة؛ خطوات: ترقية النسخة المتماثلة، تحديث DNS (TTL 60 ث)، تشغيل الخدمات من `docker-compose.full.yml`/منصة التشغيل، التحقق من `/health/ready`، إبلاغ أصحاب المصلحة.

**الاستجابة للحوادث** (`incident-response.md`): مستويات الخطورة (SEV1–SEV3)، الأدوار (قائد الحادث، الاتصال، التقني)، قنوات التواصل، قوالب التحديث، مهلة إبلاغ SDAIA خلال 72 ساعة عند تسرب بيانات شخصية (PDPL) وإبلاغ المتأثرين، وإبلاغ NCA وفق الضوابط، ومراجعة ما بعد الحادث خلال 5 أيام عمل.

## F21.10 النشر

### ملفات Docker
- `backend/Dockerfile`: مرحلة بناء `mcr.microsoft.com/dotnet/sdk:10.0` (`dotnet publish -c Release`) ← تشغيل `mcr.microsoft.com/dotnet/aspnet:10.0` مع `curl` لـ`HEALTHCHECK` على `/health/live`، مستخدم غير جذري، `ASPNETCORE_URLS=http://+:8080`، مجلدات `/app/storage` و`/app/keys`.
- أوامر سطر أوامر في `ATA.Api`: `dotnet ATA.Api.dll migrate` (يطبق الترحيلات ويخرج)، و`dotnet ATA.Api.dll seed-demo [...]` (§F21.11). بدون وسائط = تشغيل الخادم.
- `website/Dockerfile` و`dashboard/Dockerfile`: `node:22-alpine` (`npm ci && npm run build`) ← `nginx:1.27-alpine` يقدم `dist/` مع SPA fallback. الإعداد وقت التشغيل: `docker-entrypoint.d/40-config.sh` يكتب `/usr/share/nginx/html/config.js` بـ`window.__ATA_CONFIG__ = { apiBaseUrl, hubUrl, oneSignalAppId }` من متغيرات البيئة؛ `index.html` يحمّل `/config.js` و`src/lib/config.ts` يقرأه أولاً ثم `import.meta.env`.

### `docker-compose.full.yml` (جذر المستودع)
| الخدمة | الصورة | ملاحظات |
|---|---|---|
| `mysql` | `mysql:8.4` | `utf8mb4`، حجم `mysql-data`، healthcheck `mysqladmin ping` |
| `redis` | `redis:7-alpine` | `--appendonly no` |
| `api-migrate` | `./backend` | `command: ["migrate"]`، يعتمد على `mysql` healthy، يعمل مرة واحدة |
| `api` | `./backend` | يعتمد على `api-migrate` (completed) و`redis`؛ أحجام `storage-data`, `dp-keys`؛ المنفذ الداخلي 8080 |
| `website` | `./website` | المنفذ الداخلي 80 |
| `dashboard` | `./dashboard` | المنفذ الداخلي 80 |
| `nginx` | `nginx:1.27-alpine` + `deploy/nginx/ata.conf` | 80 (و443 مع شهادات في الإنتاج) |

المتغيرات من `.env.full` (نموذج `.env.full.example` في الجذر، بلا أسرار حقيقية).

`deploy/nginx/ata.conf`: `ata.localhost` → `website`؛ `admin.ata.localhost` → `dashboard`؛ `api.ata.localhost` → `api` مع ترقية WebSocket لـ`/hubs/` (`proxy_http_version 1.1`, `Upgrade`, `Connection`) و`proxy_read_timeout 3600s`، ومنع `/metrics` خارجياً، و`client_max_body_size 12m`، وتمرير `X-Forwarded-*`، وترويسات الأمان (§F21.6). (`*.localhost` يُحل محلياً في المتصفحات الحديثة.)

### مرجع متغيرات البيئة (بصيغة `Section__Key`)
| القسم | المفاتيح | المرجع |
|---|---|---|
| أساسي | `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Default`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` (سر ≥ 32)، `Cors__Origins__0..n`, `Storage__Root`, `DataProtection__KeysPath` | `03`، `backend/README.md` |
| الهوية | `Otp__DevMode`, `RateLimiting__OtpPerIpPerHour`, `Admin__Username`, `Admin__Password` (سر)، `Admin__MfaRequired`, `Admin__MfaTokenMinutes`, `Admin__MaxFailedLogins`, `Admin__LockoutMinutes`, `Admin__AccessTokenMinutes`, `Admin__SessionAbsoluteHours`, `Admin__SessionIdleMinutes` | `05`، §F20.8 |
| الرحلات والمطابقة | `Trips__*`, `Matching__*`, `Realtime__*` | `06`، `07` |
| التسعير والطلب | `Pricing__*`, `Demand__*` | `07` |
| المدفوعات | `Payments__*` (بما فيها `Payments__Moyasar__SecretKey`, `Payments__Moyasar__WebhookSecret` — أسرار)، `Payouts__*`, `Settlements__*` | `08` §F11.8 |
| الإشعارات | `OneSignal__Enabled`, `OneSignal__AppId`, `OneSignal__RestApiKey` (سر)، `OneSignal__ApiBaseUrl`, `OneSignal__AndroidChannels__*`, `Notifications__*`, `Sms__Provider`, `Sms__Sandbox`, `Sms__SenderName`, `Sms__Unifonic__*`, `Sms__Taqnyat__*` (أسرار)، `Email__Provider`, `Email__From` | `08` §F13.8، §F19.2 |
| السلامة والإلغاء | `Safety__*`, `CallMasking__Provider`, `Cancellation__*`, `Reliability__*` | `09` |
| التقييم والعروض والمفضل | `Ratings__*`, `Promotions__*`, `Tiers__*`, `Incentives__*`, `Favorites__*` | `10` |
| الجدولة والمطار والدعم | `Airport__*`, `Support__*` | `11` |
| الشركات | `Corporate__PortalBaseUrl`, `Corporate__InvitationDays`, `Corporate__InvoiceDayOfMonth`, `Corporate__AutoIssueInvoices`, `Corporate__SuspendAfterOverdueDays`, `Corporate__MaxActiveGuestTripsPerAdmin`, `Corporate__ApiKeysEnabled`, `Corporate__SellerLegalNameAr`, `Corporate__SellerLegalNameEn`, `Corporate__SellerVatNumber`, `Corporate__SellerAddress` | §F19 |
| التقارير | `Reports__*` | §F20.8 |
| التشغيل | `Redis__ConnectionString`, `Redis__KeyPrefix`, `RateLimiting__Provider`, `RateLimiting__UserPerMinute`, `RateLimiting__AnonymousPerMinute`, `Observability__OtlpEndpoint`, `Observability__MetricsAllowedCidrs`, `Serilog__MinimumLevel__Default`, `Retention__*`, `Privacy__DeletionGraceDays`, `Privacy__ExportExpiryDays`, `Privacy__DriverDocumentsRetentionDays` | §F21 |
| البيانات التجريبية | `Seed__DemoEnabled`, `Demo__SimulateDrivers`, `Demo__AutoAcceptOffers`, `Demo__AutoAdvanceTrips` | §F21.11 |
| الموقع/اللوحة (وقت التشغيل) | `ATA_API_BASE_URL`, `ATA_HUB_URL`, `ATA_ONESIGNAL_APP_ID` (→ `config.js`)؛ وقت البناء `VITE_*` | §F21.10 |
| Flutter (`--dart-define`) | `API_BASE_URL`, `HUB_URL`, `DRIVER_PORTAL_URL`, `SHOW_DEV_OTP`, `SIMULATE_LOCATION`, `ONESIGNAL_APP_ID`, `PAYMENTS_PROVIDER` | `mobile_app/README.md` |

كل مهمة خلفية لها أيضاً `Jobs__{JobName}__Enabled` (افتراضي `true`؛ `false` في الاختبارات).

## F21.11 البيانات التجريبية وأدوات الاختبار
- `dotnet run --project src/ATA.Api -- seed-demo --riders 50 --drivers 30 --trips 400 --days 30 [--reset]` — يرفض التشغيل عند `ASPNETCORE_ENVIRONMENT=Production` أو `Seed:DemoEnabled=false`. ينشئ:
  - ركاباً بجوالات `+96650000XXXX` (0001…) وسائقين بـ`+96655000XXXX`، معتمدين بمستندات وهمية سارية ومركبات موزعة على الفئات، ومستويات متنوعة.
  - مواقع سائقين متصلين حول مركز الرياض (24.7136, 46.6753) ضمن 12 كم.
  - محافظ بأرصدة، بطاقات sandbox محفوظة، رحلات تاريخية عبر الأيام بكل الحالات مع تقييمات ومدفوعات وإلغاءات ورسوم، عروض وحوافز، تذاكر دعم، حالة سلامة محلولة، حساب شركة تجريبي "شركة ATA التجريبية" بموظفين وسياسة وفاتورة، ورحلات مجدولة قادمة.
  - ملخص في السجل بالأرقام وكلمات المرور (OTP عبر `devCode`).
- `DemoDriverSimulator` (`Demo:SimulateDrivers=true`، تطوير فقط): يحرّك السائقين التجريبيين عشوائياً، ويقبل العروض باحتمال (`Demo:AutoAcceptOffers`)، ويقدّم الرحلات آلياً (`Demo:AutoAdvanceTrips`: en-route → arrived → PIN → start → complete) لاختبار الطرف الكامل من تطبيق الراكب دون سائق حقيقي.
- مجموعات HTTP: `backend/http/{auth,passenger,driver,admin,payments,…}.http` (متغيرات `@baseUrl`, `@token`) + `docs/postman/ATA.postman_collection.json` مولّدة من `/openapi/v1.json` (سكربت `scripts/export-postman.sh` باستخدام `openapi-to-postmanv2`) مع بيئة `docs/postman/ATA.local.postman_environment.json`.

## F21.12 قائمة المواءمة التنظيمية (تُراجع مع الاستشارة القانونية)

| الجهة | البند | التنفيذ في المنصة | الحالة |
|---|---|---|---|
| هيئة النقل العامة (TGA) | ترخيص المنصة كوسيط لنقل الركاب عبر التطبيقات | خارج النظام (إداري) | ☐ |
| TGA | متطلبات أهلية السائق (الجنسية، العمر، نوع الرخصة، السجل، بطاقة السائق) | أنواع مستندات قابلة للإضافة (F2/F3) + مراجعة الإدارة؛ إضافة نوع `driver_card` إن طُلب | ☐ |
| TGA | متطلبات المركبة (سنة الصنع، الفئة، التأمين، الفحص الدوري) | `vehicles.year` + مفتاح `Drivers:MaxVehicleAgeYears` (يُضاف) + مستند التأمين وانتهاؤه (F13) | ☐ |
| TGA | الربط الإلكتروني وتبادل بيانات الرحلات مع أنظمة الهيئة | تجريد `IRegulatorReporter` (placeholder) يُغذى من `trip_events` | ☐ |
| TGA | شفافية الأجرة قبل الطلب وإيصال بعده | التسعير المسبق (F10) + الإيصال (F11) | ☑ |
| TGA | معالجة الشكاوى ضمن مدد محددة | الدعم وSLA (F18) | ☑ |
| TGA | الاحتفاظ بسجلات الرحلات للمدة النظامية | لا حذف آلي للرحلات (§F21.7)؛ المدة تُحدد | ☐ |
| PDPL / SDAIA | إشعار الخصوصية والموافقة وتسجيل نسخة السياسة المقبولة | `terms_accepted_at` قائم؛ يُضاف `privacy_policy_version` | ☐ |
| PDPL | حقوق صاحب البيانات: الوصول/النسخ، التصحيح، الحذف، سحب موافقة التسويق | التصدير والحذف (§F21.8)، تعديل الملف، تفضيل `offers` | ☑ |
| PDPL | تقليل البيانات وحجبها في السجلات والتصدير | §F21.1، تصدير التقارير بمعرّفات | ☑ |
| PDPL | الإبلاغ عن التسرب خلال 72 ساعة | runbook الحوادث | ☑ |
| PDPL | نقل البيانات خارج المملكة (OneSignal، مزوّدو الخدمات) | تقييم أثر؛ إرسال `external_id` (UUID) فقط والحد من البيانات الشخصية في نص الإشعارات | ☐ |
| PDPL | تعيين مسؤول حماية بيانات وسجل أنشطة المعالجة | خارج النظام | ☐ |
| SAMA | الدفع عبر مزوّد مرخّص، بلا تخزين بيانات بطاقات (PCI DSS SAQ-A) | الترميز فقط (F11) | ☑ |
| SAMA | المحفظة المخزنة للقيمة قد تتطلب ترخيص نقود إلكترونية أو العمل تحت ترخيص شريك | حدود الشحن اليومية (F11)؛ **قرار تنظيمي مطلوب** | ☐ |
| SAMA | 3-D Secure لمدى والبطاقات | مسار `action.url` (F11) | ☑ |
| ZATCA | الفوترة الإلكترونية (المرحلة الثانية) للفواتير الضريبية للشركات، والفواتير المبسطة للأفراد | PDF + QR (F19)؛ التكامل مع منصة فاتورة مطلوب قبل الإنتاج، والإيصال (F11) يحتاج مراجعة كفاتورة مبسطة | ☐ |
| NCA (ECC) | التحقق متعدد العوامل للحسابات المميزة | MFA إلزامي (F20) | ☑ |
| NCA | أقل صلاحية وإدارة الهويات | RBAC (F20) + audit | ☑ |
| NCA | السجلات والمراقبة وربطها بـSIEM | §F21.1–F21.4 | ☐ (الربط بـSIEM) |
| NCA | التشفير أثناء النقل (TLS 1.2+) والتخزين | nginx TLS + تشفير الأقراص والنسخ + Data Protection للحقول الحساسة | ☐ |
| NCA | النسخ الاحتياطي والتعافي | §F21.9 | ☑ |
| NCA / CST | استضافة البيانات داخل المملكة لدى مزوّد سحابي مرخّص (ضوابط CCC) | قرار البنية التحتية | ☐ |
| NCA | إدارة الثغرات واختبار الاختراق والتطوير الآمن (SAST، فحص التبعيات) | CI (يُضاف) | ☐ |

---

## سيناريوهات الاختبار (الخلفية)

الشركات:
1. الدعوة والقبول بمطابقة الجوال؛ الدعوة المنتهية → `410`؛ العضوية في حساب آخر → `409 corporate_member_elsewhere`.
2. دخول البوابة بـ`corporate_admin` لغير عضو → `403 corporate_not_member`؛ العضو → JWT بمطالبة `corp`، ولا يرى بيانات حساب آخر (`404`).
3. تقييم السياسة: كل قاعدة تُنتج المخالفة الصحيحة؛ الميزانية تشمل الرحلات الجارية؛ الحد الائتماني يشمل غير المفوتر وغير المدفوع.
4. رحلة شركة مكتملة → قيد `trip_corporate_charge` متوازن؛ لا عروض (`422 promo_not_eligible`).
5. حجز ضيف → SMS `corporate.guest_trip` بالرابط والـPIN، و`trip_active_exists` لا يمنع حجزاً ثانياً للضيوف.
6. الفاتورة الشهرية: الأسطر والضريبة (15/115) ومجموعها، تفرّد الفترة، التوليد مرة واحدة، `mark-paid` ينشئ قيد التحصيل، `void` يحرر الأسطر.
7. استيراد CSV مع صفوف غير صالحة → تقرير `skipped` بالأسباب.

الصلاحيات:
8. كل نقطة `/admin/*` بلا الصلاحية المطلوبة → `403 forbidden { permission }`؛ `*` يمر.
9. الدخول مع MFA: كلمة صحيحة → `mfaRequired`؛ رمز TOTP صحيح → `AuthResponse`؛ نفس الرمز مرتين → `mfa_invalid` (منع الإعادة)؛ رمز استرداد يُستخدم مرة واحدة؛ 5 أخطاء → `mfa_locked`.
10. التسجيل الإلزامي: `mfaEnrollmentRequired` → enroll → confirm يعيد 10 رموز.
11. 5 كلمات مرور خاطئة → `429 account_locked` ثم الفتح بعد 15 د.
12. تغيير أدوار مستخدم يلغي جلساته؛ إزالة آخر `super_admin` → `409`.
13. خمول الجلسة الإدارية أكثر من 30 د → رفض التجديد.

التقارير:
14. `ReportSnapshotJob` على بيانات معدّة: قيم كل مقياس صحيحة، و`ratio` عبر عدة أيام = Σ البسط ÷ Σ المقام (وليس متوسط النسب)، و`distinct` يُحسب مباشرة للنطاق.
15. التشغيل مرتين لنفس اليوم لا يكرر الصفوف (upsert).
16. نطاق > 366 يوماً → `422 report_range_too_large`؛ التصدير CSV يبدأ بـBOM وبالأعمدة المعرّفة.

التشغيل:
17. `X-Correlation-Id` يُعاد كما أُرسل أو يُولَّد؛ السجلات لا تحتوي أرقام جوال كاملة أو رموز OTP (اختبار على مُجمّع سجلات في الذاكرة).
18. `/health/live` يعمل دون قاعدة البيانات؛ `/health/ready` → `503` عند تعطل MySQL.
19. `IDistributedLock` (تنفيذ الذاكرة واختبار تكامل Redis اختياري): قفل واحد فقط ينجح لنفس المفتاح.
20. `DataRetentionJob` يحذف فقط الأقدم من الحدود.
21. حذف الحساب: المعوقات تمنع التنفيذ؛ الدخول خلال السماح يلغي الطلب؛ التنفيذ يُجهّل الحقول المحددة ويحافظ على الرحلات والدفتر، والجوال يصبح متاحاً للتسجيل من جديد.
22. تصدير البيانات ينتج ZIP بالملفات المحددة ولا يحتوي بيانات مستخدمين آخرين (أرقام السائقين مقنّعة).
23. `seed-demo` يرفض التشغيل في Production.

## قرارات تحتاج تأكيد مالك المنتج
1. عضوية المستخدم في حساب شركة واحد فقط في v1.
2. حجوزات الضيوف تُسجَّل على ملف راكب مسؤول الشركة مع اسم/جوال الضيف، والضيف يستلم الرابط والـPIN بالـSMS.
3. أسعار الرحلات شاملة الضريبة وتُفكك في الفاتورة (÷ 1.15)، والفاتورة PDF + QR مبسّط إلى حين التكامل مع ZATCA.
4. توليد الفواتير في اليوم الأول من الشهر للشهر السابق، مع خيار إصدار تلقائي، وتعليق تلقائي للحساب المتأخر معطّل افتراضياً.
5. البريد الإلكتروني يُستخدم فقط لإرسال فواتير الشركات (`billing_email`) عبر مزوّد placeholder — استثناء من قاعدة "لا بريد" للهوية.
6. MFA إلزامي لكل حسابات الإدارة في الإنتاج، ومدة الوصول الإداري 15 دقيقة مع خمول 30 دقيقة.
7. مقاييس العدّ المميز (الركاب النشطون، التكرار) تُحسب مباشرة للنطاق وليس من مجموع الأيام.
8. مهلة سماح 30 يوماً قبل تنفيذ حذف الحساب، مع الإبقاء على السجلات المالية والرحلات مجهّلة.
9. قائمة المواءمة التنظيمية مرجعية وتتطلب تأكيداً قانونياً، خصوصاً ترخيص المحفظة (SAMA) والفوترة الإلكترونية (ZATCA) ونقل البيانات لـOneSignal.
