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

## منهجية التسليم: ميزة كاملة على كل الطبقات (Vertical Feature Slices)

كل ميزة تُنفَّذ **كاملة** عبر الطبقات الأربع في نفس التسليم قبل الانتقال للميزة التالية:

`قاعدة البيانات (MySQL) → الـAPI (وحدة في الـMonolith) → تطبيق Flutter (feature بطبقات data/domain/presentation + Cubit) → الموقع (إن كان معنياً) → لوحة الإدارة (شاشات الإدارة الخاصة بالميزة)`

قواعد Flutter: تقسيم feature-first، Clean Architecture، إدارة الحالة بـ Cubit فقط، **لا `setState`**، Clean Code (انظر `docs/03-architecture.md`).

## الميزات وترتيب التنفيذ

| # | الميزة | MySQL + API | Flutter | الموقع | لوحة الإدارة | الحالة |
|---|---|---|---|---|---|---|
| F1 | **الهوية والدخول** (لغة، نوع الحساب، جوال + OTP، JWT/Refresh، الأجهزة، الشروط) | identity module | feature `auth` | دخول بوابة السائق | دخول الإدارة | 🔨 قيد التنفيذ |
| F2 | **انضمام السائق** (الطلب، البيانات، المركبة، المستندات، الإرسال، المراجعة، الاعتماد) | drivers + catalog + files | `driver_onboarding` | بوابة رفع المستندات | طلبات السائقين + المستندات + Audit | 🔨 قيد التنفيذ |
| F3 | **الفهارس** (فئات الرحلات، أنواع المستندات، المدن) | catalog | `catalog` | الفئات في الصفحة التعريفية | إدارة الفئات | 🔨 قيد التنفيذ |
| F4 | **حساب الراكب والإعدادات** (الملف، الأماكن المحفوظة، التفضيلات، الإشعارات، اللغة، الحذف) | passengers + notifications | `account`, `notifications` | — | الركاب (تعليق/إعادة) | 🔨 قيد التنفيذ |
| F5 | **المحفظة والدفتر المالي** (الرصيد، الحركات، الشحن التجريبي، Ledger متوازن، Idempotency) | wallet | `wallet` | — | (الخطوة 3: المحافظ) | 🔨 قيد التنفيذ |
| F6 | **بوابة السائق** (متاح/غير متصل، ملخص الأرباح، المستندات والمركبة، الإعدادات) | drivers | `driver_dashboard` | — | السائقون | 🔨 قيد التنفيذ |
| F7 | **الشاشة الرئيسية للراكب** (الخريطة، المحطات، الفئات، أفضّل سائقة، طريقة الدفع، الطلب) | — (F8) | `passenger_home` | — | — | 🔨 واجهة فقط |
| F8 | **دورة الرحلة** (Trips/Stops/Events، الحالات، PIN، السائق المعيّن، التتبع SignalR) | trips module | `trip` (راكب + سائق) | — | الرحلات + Live Map | ⏳ |
| F9 | **المطابقة** (بحث جغرافي، أهلية، Score، عرض، مهلة، التالي) | matching + driver_locations | استقبال الطلب للسائق | — | قواعد المطابقة | ⏳ |
| F10 | **التسعير والمناطق والطلب** (Base+Distance+Time+Fees+Waiting×Demand−Discount، Zones، Demand Levels، Offer Your Price) | pricing + zones | تقدير السعر واقتراح السعر | — | Pricing Rules / Zones / Demand | ⏳ |
| F11 | **المدفوعات** (بوابة الدفع + Tokenization، مدى/Visa/MC/Apple Pay، الاسترداد، التسويات والسحب) | payments + payouts | طرق الدفع، السحب للسائق | — | Payments & Settlements | ⏳ |
| F12 | **السلامة** (مشاركة الرحلة، جهات موثوقة، طوارئ، كشف التوقف/الانحراف، البلاغات) | safety | `safety` | — | Safety Cases | ⏳ |
| F13 | **الإشعارات الفورية** (FCM/APNs، SMS للحالات الحرجة، قوالب) | notifications | تكامل FCM | — | القوالب | ⏳ |
| F14 | **الإلغاء والموثوقية** (قواعد، أسباب، أحداث، عقوبات الطرفين، ReliabilityProfiles) | cancellation | إلغاء بسبب + رسوم | — | قواعد الإلغاء | ⏳ |
| F15 | **التقييم والعروض** (Ratings، Promo Codes، مستويات السائق، الحوافز) | ratings + promotions + incentives | `rating`, `promotions` | — | Promotions / Incentives | ⏳ |
| F16 | **السائق المفضل** (المفضلة، الاختيار بالاسم، الخصم، قواعد عدم الجمع) | favorites | `favorite_drivers` | — | قواعد خصم المفضل | ⏳ |
| F17 | **الرحلات المجدولة والمطار** (نافذة 7 أيام، تذكيرات، سياسات إلغاء، Terminal/Pickup Zone) | scheduling + airport | الجدولة + المطار | — | قواعد الجدولة | ⏳ |
| F18 | **الدعم** (مركز المساعدة، البلاغات، المفقودات، النزاعات) | support | `support` | صفحة المساعدة | Support Tickets | ⏳ |
| F19 | **الشركات** (الحسابات، الموظفون، السياسات، الفواتير) | corporate | حجز الشركة | بوابة الشركات | Corporate Accounts | ⏳ |
| F20 | **التقارير والصلاحيات** (KPIs، Roles & Permissions، MFA للإدارة) | reporting + rbac | — | — | Reports / Roles | ⏳ |
| F21 | **الجاهزية التشغيلية** (Observability، Backups، Retention، المواءمة التنظيمية) | infra | — | — | — | ⏳ |

كل ميزة تُسلَّم كـ commit مستقل على فرع `claude/system-readiness-bc02lz` مع تحديث هذا الملف. التسليم الحالي يغطي F1–F7 (الأساس).
