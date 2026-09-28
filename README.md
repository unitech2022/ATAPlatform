# ATA Platform

منصة نقل ركاب عند الطلب (Mobility Marketplace) — السوق الأول: الرياض. المرجع الكامل في مجلد [`docs/`](docs/ROADMAP.md).

| المجلد | المشروع | التقنية |
|---|---|---|
| `mobile_app/` | تطبيق الراكب والسائق | Flutter 3.x (Riverpod, go_router, dio) |
| `backend/` | واجهات الـAPI | ASP.NET Core (.NET 10 LTS) + EF Core + **MySQL 8** |
| `website/` | الموقع التعريفي + بوابة رفع مستندات السائق | React 19 + Vite + Tailwind v4 |
| `dashboard/` | لوحة الإدارة | React 19 + Vite + Tailwind v4 |
| `res/` | الشعار وملف التصميم (Figma Make) | — |
| `docs/` | نظام التصميم، التدفقات، البنية، مخطط MySQL، عقد الـAPI، خارطة التنفيذ | — |

## التشغيل السريع

```bash
# قاعدة البيانات (MySQL 8 + Redis)
cd backend && docker compose up -d

# الـAPI (يطبّق الـMigrations ويزرع البيانات الأولية في بيئة التطوير)
cd backend && dotnet run --project src/ATA.Api        # http://localhost:5000  — واجهة الوثائق: /docs

# التطبيق
cd mobile_app && flutter pub get && flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000/api/v1

# الموقع ولوحة الإدارة
cd website   && npm install && npm run dev            # http://localhost:5173
cd dashboard && npm install && npm run dev            # http://localhost:5174
```

بيئة التطوير: رمز OTP يُعاد في استجابة `/auth/otp/request` (`devCode`)، وحساب الإدارة `admin / Admin@12345`.

## الوثائق

1. [خارطة التنفيذ](docs/ROADMAP.md)
2. [نظام التصميم](docs/01-design-system.md)
3. [تدفقات المستخدم](docs/02-user-flows.md)
4. [البنية التقنية](docs/03-architecture.md)
5. [مخطط قاعدة البيانات (MySQL)](docs/04-database-schema.md)
6. [عقد الـAPI](docs/05-api-contract.md)
