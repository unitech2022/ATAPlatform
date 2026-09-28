# البنية التقنية

## نظرة عامة

```
 Flutter (راكب/سائق) ─┐
 Website (React)      ─┼── HTTPS/JSON ──► ASP.NET Core API (.NET 10) ──► MySQL 8
 Dashboard (React)    ─┘         │                 │
                                 │ SignalR (خطوة 2) │ Redis (cache/locks/rate-limit)
                                 ▼                 ▼
                          Object Storage (المستندات)  SMS / FCM / Payment Gateway
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

## التطبيق `mobile_app/` (Flutter)

```
lib/
  main.dart                 # bootstrap + ProviderScope
  app/                      # router (go_router), theme, localization (ar/en)
  core/                     # api client (dio + interceptors), storage (secure), env, result/errors
  design/                   # tokens (colors, shadows, radii), widgets (AtaButton, AtaCard, Keypad, OtpBoxes, Toggle, SettingRow, StatCard, AtaIcon, BottomNav)
  features/
    auth/                   # language, role, phone, otp, driver_pending
    passenger/
      home/ rides/ wallet/ safety/ account/
    driver/
      overview/ documents/ settings/ pending/
  l10n/                     # app_ar.arb, app_en.arb
```

- إدارة الحالة: Riverpod. التوجيه: go_router. الشبكة: dio. التخزين الآمن: flutter_secure_storage. الترجمة: flutter_localizations + intl (gen-l10n).
- الخريطة: `google_maps_flutter` تُضاف في الخطوة 2؛ في الخطوة 1 خريطة مرسومة (SVG-like) مطابقة للتصميم.

## الموقع `website/` ولوحة الإدارة `dashboard/`

React 19 + Vite + TypeScript + Tailwind CSS v4 (`@theme` بنفس Tokens نظام التصميم) + react-router. طبقة API واحدة (`src/lib/api.ts`) مع تخزين التوكن وتجديده.

## البيئات والإعدادات

| المتغير | الوصف |
|---|---|
| `ConnectionStrings__Default` | `Server=localhost;Port=3306;Database=ata;User=ata;Password=ata;` |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` | إعدادات التوكن |
| `Otp__DevMode` | `true` في التطوير: لا SMS، والرمز يُعاد في الاستجابة |
| `Storage__Root` | مجلد تخزين الملفات محلياً (`./storage`) |
| `Cors__Origins` | أصول الموقع ولوحة الإدارة |
