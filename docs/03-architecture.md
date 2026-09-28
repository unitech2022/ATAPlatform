# البنية التقنية

## نظرة عامة

```
 Flutter (راكب/سائق) ─┐
 Website (React)      ─┼── HTTPS/JSON ──► ASP.NET Core API (.NET 10) ──► MySQL 8
 Dashboard (React)    ─┘         │                 │
                                 │ SignalR (خطوة 2) │ Redis (cache/locks/rate-limit)
                                 ▼                 ▼
                          Object Storage (المستندات)  SMS / OneSignal (Push) / Payment Gateway
```

Modular Monolith: مشروع واحد قابل للنشر، مقسم إلى وحدات (Modules) بحدود واضحة، تتواصل عبر خدمات داخلية وأحداث محلية، بدون Microservices في البداية.

## هيكل الخلفية `backend/`

```
backend/
  ATAPlatform.sln
  docker-compose.yml                 # MySQL 8 + Redis للتطوير
  src/
    ATA.Domain/                      # الكيانات والقيم والقواعد (بدون تبعيات على EF)
      Common/        (Entity, AuditableEntity, enums مشتركة, DomainException)
      Identity/      (User, UserRole, OtpRequest, RefreshToken, UserDevice, AdminAccount)
      Passengers/    (PassengerProfile, SavedPlace)
      Drivers/       (DriverProfile, DriverApplication, DriverDocument, Vehicle, VehicleDocument, DriverStatus)
      Catalog/       (RideCategory, DocumentType, City)
      Wallet/        (Wallet, WalletTransaction, LedgerEntry)
      Notifications/ (Notification, NotificationPreference)
      Admin/         (AuditLog)
    ATA.Infrastructure/              # EF Core + Pomelo MySQL، Migrations، التخزين، SMS، الوقت، الكاش
      Persistence/   (AtaDbContext, Configurations/*, Migrations/, Seed/)
      Storage/       (IFileStorage → LocalFileStorage / S3-compatible لاحقاً)
      Sms/           (ISmsSender → LoggingSmsSender في التطوير)
      Security/      (JwtTokenService, PasswordHasher, OtpGenerator)
    ATA.Api/                         # المضيف: Program.cs + Minimal API endpoints لكل وحدة
      Modules/
        Identity/    (Endpoints + Services + Contracts)
        Passengers/
        Drivers/
        Catalog/
        Wallet/
        Notifications/
        Admin/
      Common/        (ApiResults, ErrorHandling, Localization, RateLimiting, CurrentUser)
  tests/
    ATA.Tests/                       # xUnit: وحدات القواعد + اختبارات تكامل خفيفة (SQLite in-memory)
```

مبادئ:
- كل وحدة تملك كياناتها وجداولها؛ لا تُعدِّل وحدة جداول وحدة أخرى مباشرة.
- المفاتيح الأساسية `CHAR(36)` UUID تُولَّد في التطبيق (`Guid.CreateVersion7()`).
- الأخطاء بصيغة موحدة `{ "error": { "code", "message", "details" } }`، والرسائل مترجمة حسب `Accept-Language` (ar افتراضي).
- المصادقة JWT (Bearer) + Refresh Tokens مخزنة (قابلة للإلغاء) + ربط بالجهاز.
- التفويض بالأدوار: `Passenger`, `Driver`, `Admin`, `Operations`, `CorporateAdmin` + صلاحيات دقيقة (`permissions` claim) للإدارة.
- Audit Log لكل عملية إدارية حساسة.
- Rate limiting على OTP وعلى الـAPI عموماً (ASP.NET Core RateLimiter؛ Redis عند التوسع).
- Idempotency-Key header للعمليات المالية (تُفعّل بالكامل في الخطوة 3).
- OpenAPI في `/openapi/v1.json` + Scalar UI في التطوير على `/docs`.

## التطبيق `mobile_app/` (Flutter) — Feature-first + Clean Architecture + Cubit

```
lib/
  main.dart                 # bootstrap + DI + BlocObserver
  app/                      # router (go_router), theme, l10n wiring
  core/                     # di (get_it), network (dio + interceptors), storage, errors (Failure), utils
  design/                   # tokens + widgets مشتركة (AtaButton, AtaCard, Keypad, OtpBoxes, AtaToggle, StatCard, SettingRow, BottomNav, MapCanvas, AtaIcon)
  features/
    <feature>/
      data/                 # models (json), datasources (remote/local), repository implementations
      domain/               # entities, repository interfaces, usecases (كلاس لكل حالة استخدام)
      presentation/         # cubit/ (Cubit + State immutable), pages/, widgets/
  l10n/                     # app_ar.arb, app_en.arb
```

الميزات (features): `auth`, `driver_onboarding`, `passenger_home`, `rides`, `wallet`, `safety`, `account`, `notifications`, `driver_dashboard`, `catalog`.

قواعد إلزامية:
- إدارة الحالة بـ **flutter_bloc / Cubit فقط**. **يُمنع `setState`** في كل التطبيق؛ كل حالة شاشة (إدخال لوحة الأرقام، OTP، المفاتيح، التبويبات، العدّادات، المحطات، الفئة المختارة…) تعيش في Cubit بحالة غير قابلة للتغيير (`equatable` + `copyWith`).
- الواجهات `StatelessWidget` تستخدم `BlocBuilder`/`BlocSelector`/`BlocListener`. لا منطق أعمال داخل الـWidgets.
- الـCubit يستدعي Use Cases فقط؛ الـUse Case يستدعي واجهة Repository؛ التنفيذ في `data/`. النتائج `Either<Failure, T>` (fpdart).
- ملفات صغيرة (< 250 سطر)، لا أرقام سحرية، تسمية موحدة، اختبارات `bloc_test` للـCubits الرئيسية.
- التوجيه: go_router. الشبكة: dio. التخزين الآمن: flutter_secure_storage. الترجمة: gen-l10n.
- الخريطة: `google_maps_flutter` تُضاف في الخطوة 2؛ في الخطوة 1 خريطة مرسومة (CustomPainter) مطابقة للتصميم.

## الموقع `website/` ولوحة الإدارة `dashboard/`

React 19 + Vite + TypeScript + Tailwind CSS v4 (`@theme` بنفس Tokens نظام التصميم) + react-router. طبقة API واحدة (`src/lib/api.ts`) مع تخزين التوكن وتجديده.

## الإشعارات الفورية — OneSignal (قرار نهائي)

- مزوّد واحد للإشعارات: **OneSignal** (تطبيق OneSignal واحد يخدم الراكب والسائق وأندرويد وiOS). لا ربط مباشر مع FCM/APNs من الخلفية؛ OneSignal يتولى ذلك.
- الربط بالمستخدم عبر **External ID = `user_id`**: التطبيق يستدعي `OneSignal.login(userId)` بعد الدخول و`OneSignal.logout()` عند الخروج، فلا حاجة لتخزين Push Tokens كمصدر حقيقة.
- Tags في OneSignal: `role` (passenger/driver)، `lang` (ar/en)، `city`. تُحدَّث من التطبيق بعد الدخول وتغيير اللغة.
- الخلفية ترسل عبر OneSignal REST API (`POST /notifications` مع `include_aliases.external_id`) من خلال تجريد `IPushSender` → `OneSignalPushSender` (و`LoggingPushSender` في التطوير والاختبارات).
- كل إشعار يُحفظ أولاً في جدول `notifications` (صندوق الوارد داخل التطبيق) ثم يُرسل Push وفق `notification_preferences` (الرحلات/المحفظة/السلامة/العروض). إشعارات السلامة والرحلة الحرجة تتجاوز تفضيل الإيقاف.
- الإعدادات: `OneSignal__AppId`, `OneSignal__RestApiKey` (سر لا يُرفع للمستودع)، وفي Flutter `--dart-define=ONESIGNAL_APP_ID=...`.
- SMS يبقى للـOTP والحالات الحرجة فقط عبر مزوّد SMS منفصل.

## البيئات والإعدادات

| المتغير | الوصف |
|---|---|
| `ConnectionStrings__Default` | `Server=localhost;Port=3306;Database=ata;User=ata;Password=ata;` |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` | إعدادات التوكن |
| `Otp__DevMode` | `true` في التطوير: لا SMS، والرمز يُعاد في الاستجابة |
| `Storage__Root` | مجلد تخزين الملفات محلياً (`./storage`) |
| `Cors__Origins` | أصول الموقع ولوحة الإدارة |
| `OneSignal__AppId`, `OneSignal__RestApiKey` | إرسال الإشعارات الفورية (F13) |
