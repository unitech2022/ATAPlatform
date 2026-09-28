# خارطة تنفيذ منصة ATA (Mobility Marketplace)

المرجع: مواصفات "تطبيق ATA – نسخة تنفيذية موحدة" + الإصدار 1.1، والتصميم/الفلو في `res/تصميم تطبيق ATA للتوصيل.zip` (نموذج Figma Make).

القرارات الثابتة:

| البند | القرار |
|---|---|
| قاعدة البيانات | **MySQL 8.x** (InnoDB, utf8mb4) عبر EF Core + Pomelo |
| الخلفية | ASP.NET Core **.NET 10 LTS**، Modular Monolith |
| التطبيق | Flutter (تطبيق واحد بواجهتين: راكب/سائق) |
| الموقع | React + Vite + Tailwind v4 — الصفحة التعريفية + بوابة رفع مستندات السائق |
| لوحة الإدارة | React + Vite + Tailwind v4 |
| الهوية | رقم الجوال + OTP فقط، لا بريد إلكتروني |
| اللغات | العربية (RTL، افتراضية) والإنجليزية |
| الذكاء الاصطناعي | لا يوجد في النسخة الأولى |

## الخطوات

| # | الخطوة | المحتوى | الحالة |
|---|---|---|---|
| 1 | **الأساس والهوية وانضمام السائق** | نظام التصميم، الوثائق، بنية الخلفية، مخطط MySQL، Identity (OTP/JWT)، ملف الراكب، طلب انضمام السائق + المستندات + المركبة، مراجعة الإدارة، فئات الرحلات، تطبيق Flutter (الفلو الكامل من التصميم)، الموقع (بوابة المستندات)، لوحة الإدارة (المصادقة + مراجعة السائقين) | ✅ هذه الخطوة |
| 2 | دورة الرحلة والمطابقة والتسعير | Trips + TripStops + TripEvents، Matching Engine (Rules)، Pricing Engine، Zones، Demand Levels، Offer Your Price، DriverLocations + SignalR | ⏳ |
| 3 | المدفوعات والمحفظة والدفتر المالي | Payment Gateway (tokenization)، Wallets، Ledger القابل للتدقيق، Payouts/Settlements، Idempotency | ⏳ |
| 4 | السلامة والإشعارات والوقت الحقيقي | PIN، مشاركة الرحلة، زر طوارئ، كشف التوقف/الانحراف، FCM/APNs، SMS | ⏳ |
| 5 | الإلغاء والموثوقية والتقييم والسائق المفضل | Cancellation & Reliability Engine، Ratings، Promotions، FavoriteDrivers + Discount Rules | ⏳ |
| 6 | الرحلات المجدولة والمطار | نافذة 7 أيام، التذكيرات، سياسات الإلغاء الخاصة، Airport Pickup/Drop-off | ⏳ |
| 7 | لوحة الإدارة الكاملة والتقارير | Live Map، Pricing/Zones/Demand/Incentives، Support، Safety Cases، Reports & KPIs، Roles & Permissions | ⏳ |
| 8 | حسابات الشركات | Corporate Accounts/Users/Policies، الفواتير الشهرية، التقارير | ⏳ |
| 9 | الجاهزية التشغيلية | Observability، Backups، Rate limiting متقدم، Data retention، المواءمة التنظيمية (هيئة النقل) | ⏳ |

كل خطوة تُسلَّم كـ commit مستقل على فرع `claude/system-readiness-bc02lz` مع تحديث هذا الملف.
