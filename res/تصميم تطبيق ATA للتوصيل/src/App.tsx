import { useEffect, useState, type ReactNode } from "react";
import logo from "./imports/_logo1.png";

type IconName =
  | "arrow"
  | "bell"
  | "car"
  | "check"
  | "chevron"
  | "clock"
  | "document"
  | "home"
  | "location"
  | "menu"
  | "pin"
  | "plus"
  | "phone"
  | "search"
  | "shield"
  | "upload"
  | "user"
  | "wallet";

const paths: Record<IconName, ReactNode> = {
  arrow: <path d="m15 18-6-6 6-6" />,
  bell: (
    <>
      <path d="M6 9a6 6 0 0 1 12 0c0 7 3 7 3 7H3s3 0 3-7" />
      <path d="M10 20h4" />
    </>
  ),
  car: (
    <>
      <path d="M5 17H3v-5l2-5h14l2 5v5h-2" />
      <path d="M5 12h14M7 17h10M7.5 7 9 4h6l1.5 3" />
      <circle cx="7" cy="15" r="1" />
      <circle cx="17" cy="15" r="1" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  chevron: <path d="m9 18 6-6-6-6" />,
  clock: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </>
  ),
  document: (
    <>
      <path d="M6 2h8l4 4v16H6z" />
      <path d="M14 2v5h5M9 12h6M9 16h6" />
    </>
  ),
  home: (
    <>
      <path d="m4 11 8-7 8 7v9H4z" />
      <path d="M9 20v-6h6v6" />
    </>
  ),
  location: (
    <>
      <circle cx="12" cy="12" r="8" />
      <circle cx="12" cy="12" r="2" />
      <path d="M12 2V0M12 24v-2M2 12H0M24 12h-2" />
    </>
  ),
  menu: (
    <>
      <path d="M4 7h16M4 12h16M4 17h16" />
    </>
  ),
  pin: (
    <>
      <path d="M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0Z" />
      <circle cx="12" cy="10" r="2.5" />
    </>
  ),
  plus: <path d="M12 5v14M5 12h14" />,
  phone: (
    <>
      <path d="M7 3h3l1 5-2 1c1 3 3 5 6 6l1-2 5 1v3c0 2-2 4-4 4C9 20 4 15 3 7c0-2 2-4 4-4Z" />
    </>
  ),
  search: (
    <>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-4-4" />
    </>
  ),
  shield: (
    <>
      <path d="M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  upload: (
    <>
      <path d="M12 16V4M7 9l5-5 5 5" />
      <path d="M5 14v6h14v-6" />
    </>
  ),
  user: (
    <>
      <circle cx="12" cy="8" r="4" />
      <path d="M4 21c1-5 4-7 8-7s7 2 8 7" />
    </>
  ),
  wallet: (
    <>
      <path d="M3 6h17v13H3zM3 9h17" />
      <path d="M15 13h5v3h-5z" />
    </>
  ),
};

function Icon({ name, className = "size-5" }: { name: IconName; className?: string }) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="1.8"
    >
      {paths[name]}
    </svg>
  );
}

function Action({
  children,
  className = "",
  onClick,
}: {
  children: ReactNode;
  className?: string;
  onClick?: () => void;
}) {
  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(event) => event.key === "Enter" && onClick?.()}
      className={`cursor-pointer select-none transition active:scale-[0.98] ${className}`}
    >
      {children}
    </div>
  );
}

const rides = [
  { id: "economy", name: "اقتصادي", detail: "سيارة مريحة", eta: "دقيقتان", price: "38 ر.س", icon: "car" as const },
  { id: "family", name: "عائلي", detail: "حتى 6 ركاب", eta: "4 دقائق", price: "54 ر.س", icon: "car" as const },
  { id: "premium", name: "ATA بلس", detail: "رحلة أكثر تميزاً", eta: "6 دقائق", price: "72 ر.س", icon: "shield" as const },
];

type Screen = "home" | "rides" | "safety" | "wallet" | "account";
type AuthStage = "language" | "role" | "phone" | "otp" | "pending" | "driver" | "app";
type UserRole = "customer" | "courier";

const tripHistory = [
  { place: "واجهة الرياض", date: "اليوم، 10:35 ص", price: "38 ر.س", status: "مكتملة" },
  { place: "مطار الملك خالد الدولي", date: "الأحد، 8:20 م", price: "74 ر.س", status: "مكتملة" },
  { place: "بوليفارد سيتي", date: "الخميس، 6:45 م", price: "46 ر.س", status: "ملغاة" },
];

function ScreenTitle({ eyebrow, title, copy }: { eyebrow: string; title: string; copy: string }) {
  return (
    <div className="mb-8">
      <p className="mb-2 text-sm font-extrabold text-brand">{eyebrow}</p>
      <p className="text-3xl font-black tracking-tight sm:text-4xl">{title}</p>
      <p className="mt-3 max-w-2xl leading-7 text-muted">{copy}</p>
    </div>
  );
}

function RegistrationFlow({
  stage,
  role,
  language,
  onStage,
  onRole,
  onLanguage,
}: {
  stage: Exclude<AuthStage, "app" | "driver">;
  role: UserRole | null;
  language: "ar" | "en";
  onStage: (stage: AuthStage) => void;
  onRole: (role: UserRole) => void;
  onLanguage: (language: "ar" | "en") => void;
}) {
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const isArabic = language === "ar";

  const addPhoneDigit = (digit: string) => {
    if (phone.length < 9) setPhone((current) => current + digit);
  };

  const addOtpDigit = (digit: string) => {
    if (otp.length < 4) setOtp((current) => current + digit);
  };

  const keypad = (onDigit: (digit: string) => void, onDelete: () => void) => (
    <div className="mx-auto grid max-w-xs grid-cols-3 gap-3" dir="ltr">
      {["1", "2", "3", "4", "5", "6", "7", "8", "9"].map((digit) => (
        <Action key={digit} onClick={() => onDigit(digit)} className="grid h-13 place-items-center rounded-2xl bg-cloud text-xl font-bold hover:bg-brand-soft">
          {digit}
        </Action>
      ))}
      <div />
      <Action onClick={() => onDigit("0")} className="grid h-13 place-items-center rounded-2xl bg-cloud text-xl font-bold hover:bg-brand-soft">0</Action>
      <Action onClick={onDelete} className="grid h-13 place-items-center rounded-2xl text-sm font-bold text-muted hover:bg-cloud">{isArabic ? "حذف" : "Delete"}</Action>
    </div>
  );

  return (
    <main dir={isArabic ? "rtl" : "ltr"} className="relative min-h-screen overflow-hidden bg-canvas font-sans text-ink">
      <div className="absolute -right-28 -top-28 size-96 rounded-full bg-brand/10" />
      <div className="absolute -bottom-32 -left-24 size-96 rounded-full bg-ink/5" />
      <header className="relative z-10 flex h-20 items-center justify-between px-5 lg:px-10">
        <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
        {stage !== "language" && (
          <Action
            onClick={() => {
              if (stage === "role") onStage("language");
              else if (stage === "otp") onStage("phone");
              else onStage("role");
            }}
            className="flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-extrabold shadow-soft"
          >
            <Icon name="arrow" className="size-4" />
            {isArabic ? "رجوع" : "Back"}
          </Action>
        )}
      </header>

      <div className="app-shell relative z-10 mx-auto flex max-w-6xl items-center justify-center px-5 pb-10">
        {stage === "language" && (
          <div className="w-full max-w-3xl">
            <div className="mb-10 text-center">
              <p className="mb-3 text-sm font-extrabold text-brand">ATA</p>
              <p className="text-4xl font-black tracking-tight sm:text-5xl">اختر لغتك · Choose your language</p>
              <p className="mt-4 text-muted">يمكنك تغيير اللغة لاحقاً من الإعدادات · You can change it later in Settings</p>
            </div>
            <div className="grid gap-5 sm:grid-cols-2">
              <Action
                onClick={() => {
                  onLanguage("ar");
                  onStage("role");
                }}
                className="group rounded-3xl border-2 border-transparent bg-white p-7 text-right shadow-soft hover:-translate-y-1 hover:border-brand hover:shadow-float"
              >
                <div className="mb-10 grid size-16 place-items-center rounded-2xl bg-brand-soft text-xl font-black text-brand">AR</div>
                <p className="text-2xl font-black">العربية</p>
                <p className="mt-3 text-muted">المملكة العربية السعودية</p>
                <div className="mt-8 flex items-center gap-2 font-extrabold text-brand">متابعة بالعربية <Icon name="arrow" className="size-5" /></div>
              </Action>
              <Action
                onClick={() => {
                  onLanguage("en");
                  onStage("role");
                }}
                className="group rounded-3xl border-2 border-transparent bg-ink p-7 text-left text-white shadow-button hover:-translate-y-1 hover:border-brand hover:shadow-float"
              >
                <div className="mb-10 grid size-16 place-items-center rounded-2xl bg-white/10 text-xl font-black text-brand">EN</div>
                <p className="text-2xl font-black">English</p>
                <p className="mt-3 text-white/60">United Kingdom</p>
                <div className="mt-8 flex items-center gap-2 font-extrabold text-brand">Continue in English <Icon name="arrow" className="size-5 rotate-180" /></div>
              </Action>
            </div>
          </div>
        )}

        {stage === "role" && (
          <div className="w-full max-w-4xl">
            <div className="mb-10 text-center">
              <p className="mb-3 text-sm font-extrabold text-brand">{isArabic ? "ابدأ رحلتك مع ATA" : "Start your journey with ATA"}</p>
              <p className="text-4xl font-black tracking-tight sm:text-5xl">{isArabic ? "كيف تريد استخدام التطبيق؟" : "How would you like to use ATA?"}</p>
              <p className="mt-4 text-muted">{isArabic ? "اختر نوع الحساب للمتابعة باستخدام رقم جوالك فقط" : "Choose an account type to continue with your phone number"}</p>
            </div>
            <div className="grid gap-5 md:grid-cols-2">
              <Action
                onClick={() => {
                  onRole("customer");
                  onStage("phone");
                }}
                className="group rounded-3xl border-2 border-transparent bg-white p-7 shadow-soft hover:-translate-y-1 hover:border-brand hover:shadow-float"
              >
                <div className="mb-10 flex items-start justify-between">
                  <div className="grid size-16 place-items-center rounded-2xl bg-brand text-white"><Icon name="user" className="size-8" /></div>
                  <div className="rounded-full bg-brand-soft px-3 py-1.5 text-xs font-extrabold text-brand">{isArabic ? "للركاب" : "For riders"}</div>
                </div>
                <p className="text-2xl font-black">{isArabic ? "التسجيل كعميل" : "Sign up as a rider"}</p>
                <p className="mt-3 leading-7 text-muted">{isArabic ? "اطلب رحلتك خلال دقائق، وتابع الكابتن حتى يصل إليك." : "Request a ride in minutes and track your driver until arrival."}</p>
                <div className="mt-8 flex items-center gap-2 font-extrabold text-brand">{isArabic ? "ابدأ الآن" : "Get started"} <Icon name="arrow" className="size-5" /></div>
              </Action>

              <Action
                onClick={() => {
                  onRole("courier");
                  onStage("phone");
                }}
                className="group rounded-3xl border-2 border-transparent bg-ink p-7 text-white shadow-button hover:-translate-y-1 hover:border-brand hover:shadow-float"
              >
                <div className="mb-10 flex items-start justify-between">
                  <div className="grid size-16 place-items-center rounded-2xl bg-brand text-white"><Icon name="car" className="size-8" /></div>
                  <div className="rounded-full bg-white/10 px-3 py-1.5 text-xs font-extrabold text-brand">{isArabic ? "للسائقين" : "For drivers"}</div>
                </div>
                <p className="text-2xl font-black">{isArabic ? "التسجيل كسائق" : "Sign up as a driver"}</p>
                <p className="mt-3 leading-7 text-white/60">{isArabic ? "سجّل رقمك، ثم ارفع مستنداتك عبر الموقع ليتم تفعيل حسابك." : "Register your number, then upload your documents online for approval."}</p>
                <div className="mt-8 flex items-center gap-2 font-extrabold text-brand">{isArabic ? "انضم إلى ATA" : "Join ATA"} <Icon name="arrow" className="size-5" /></div>
              </Action>
            </div>
            <p className="mt-8 text-center text-xs leading-6 text-muted">{isArabic ? "بمتابعتك، أنت توافق على شروط الاستخدام وسياسة الخصوصية" : "By continuing, you agree to our Terms of Use and Privacy Policy"}</p>
          </div>
        )}

        {stage === "phone" && (
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-panel sm:p-8">
            <div className="mb-7 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand"><Icon name="phone" className="size-7" /></div>
            <p className="text-3xl font-black">{isArabic ? "أدخل رقم جوالك" : "Enter your phone number"}</p>
            <p className="mt-3 leading-7 text-muted">{isArabic ? <>سنرسل لك رمز تحقق لتأكيد الرقم و{role === "courier" ? "بدء طلب الانضمام كسائق" : "إنشاء حسابك"}.</> : <>We will send you a verification code to {role === "courier" ? "start your driver application" : "create your account"}.</>}</p>
            <div className="my-7 flex h-16 items-center overflow-hidden rounded-2xl border-2 border-brand bg-white shadow-brand" dir="ltr">
              <div className="grid h-full place-items-center border-r border-line bg-cloud px-4 font-bold">+966</div>
              <div className="flex-1 px-4 text-lg font-bold tracking-wider">
                {phone || <span className="text-line">5X XXX XXXX</span>}
              </div>
            </div>
            {keypad(addPhoneDigit, () => setPhone((current) => current.slice(0, -1)))}
            <Action
              onClick={() => phone.length === 9 && onStage("otp")}
              className={`mt-7 rounded-2xl py-4 text-center font-extrabold text-white ${phone.length === 9 ? "bg-ink shadow-button" : "cursor-not-allowed bg-line"}`}
            >
              {isArabic ? "إرسال رمز التحقق" : "Send verification code"}
            </Action>
          </div>
        )}

        {stage === "otp" && (
          <div className="w-full max-w-md rounded-3xl bg-white p-6 text-center shadow-panel sm:p-8">
            <div className="mx-auto mb-6 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand"><Icon name="shield" className="size-7" /></div>
            <p className="text-3xl font-black">{isArabic ? "تحقق من رقمك" : "Verify your number"}</p>
            <p className="mt-3 leading-7 text-muted">{isArabic ? "أرسلنا رمزاً من 4 أرقام إلى" : "We sent a 4-digit code to"} <span dir="ltr" className="font-bold text-ink">+966 {phone}</span></p>
            <div className="my-7 flex justify-center gap-3" dir="ltr">
              {[0, 1, 2, 3].map((index) => (
                <div key={index} className={`grid size-14 place-items-center rounded-2xl border-2 text-xl font-black ${otp[index] ? "border-brand bg-brand-soft" : "border-line"}`}>
                  {otp[index] ?? ""}
                </div>
              ))}
            </div>
            {keypad(addOtpDigit, () => setOtp((current) => current.slice(0, -1)))}
            <Action
              onClick={() => {
                if (otp.length === 4) onStage(role === "courier" ? "pending" : "app");
              }}
              className={`mt-7 rounded-2xl py-4 text-center font-extrabold text-white ${otp.length === 4 ? "bg-ink shadow-button" : "cursor-not-allowed bg-line"}`}
            >
              {isArabic ? "تأكيد ومتابعة" : "Verify and continue"}
            </Action>
            <Action className="mt-5 text-sm font-extrabold text-brand">{isArabic ? "إعادة إرسال الرمز" : "Resend code"}</Action>
          </div>
        )}

        {stage === "pending" && (
          <div className="w-full max-w-3xl rounded-3xl bg-white p-6 shadow-panel sm:p-10">
            <div className="mb-7 flex flex-col items-start justify-between gap-5 sm:flex-row sm:items-center">
              <div>
                <div className="mb-5 grid size-16 place-items-center rounded-full bg-brand-soft text-brand"><Icon name="check" className="size-8" /></div>
                <p className="text-3xl font-black">تم إنشاء طلبك بنجاح</p>
                <p className="mt-3 max-w-xl leading-7 text-muted">حسابك كسائق غير نشط حالياً. أكمل رفع المستندات عبر موقع ATA ليقوم فريق الإدارة بمراجعتها وتفعيل حسابك.</p>
              </div>
              <div className="shrink-0 rounded-2xl bg-cloud px-5 py-4 text-center">
                <p className="text-xs font-bold text-muted">رقم الطلب</p>
                <p className="mt-1 font-black text-ink">ATA-28419</p>
              </div>
            </div>

            <div className="mb-7 grid gap-3 sm:grid-cols-3">
              {[
                ["1", "رفع المستندات", "عبر موقع ATA"],
                ["2", "مراجعة الإدارة", "خلال 24–48 ساعة"],
                ["3", "تفعيل الحساب", "إشعار عبر الجوال"],
              ].map(([number, title, copy], index) => (
                <div key={number} className={`rounded-2xl border p-4 ${index === 0 ? "border-brand bg-brand-soft" : "border-line"}`}>
                  <div className={`mb-5 grid size-8 place-items-center rounded-full text-sm font-black ${index === 0 ? "bg-brand text-white" : "bg-cloud text-muted"}`}>{number}</div>
                  <p className="font-extrabold">{title}</p>
                  <p className="mt-1 text-xs text-muted">{copy}</p>
                </div>
              ))}
            </div>

            <div className="mb-7 rounded-2xl border border-line p-5">
              <div className="mb-4 flex items-center gap-3">
                <Icon name="document" className="size-6 text-brand" />
                <p className="font-extrabold">المستندات المطلوبة</p>
              </div>
              <div className="grid gap-3 text-sm text-muted sm:grid-cols-2">
                {["الهوية الوطنية أو الإقامة", "رخصة قيادة سارية", "استمارة المركبة", "صورة شخصية واضحة"].map((document) => (
                  <div key={document} className="flex items-center gap-2"><Icon name="check" className="size-4 text-brand" />{document}</div>
                ))}
              </div>
            </div>

            <Action className="flex items-center justify-center gap-3 rounded-2xl bg-ink py-4 font-extrabold text-white shadow-button">
              <Icon name="upload" className="size-5" />
              الانتقال لموقع رفع المستندات
            </Action>
            <Action onClick={() => onStage("driver")} className="mt-3 rounded-2xl border border-line py-4 text-center font-extrabold text-ink hover:bg-cloud">
              معاينة حساب السائق بعد التفعيل
            </Action>
            <Action onClick={() => onStage("role")} className="mt-5 text-center text-sm font-extrabold text-muted">العودة إلى تسجيل الدخول</Action>
          </div>
        )}
      </div>
    </main>
  );
}

function DriverAccount({ onBack, onLogout }: { onBack: () => void; onLogout: () => void }) {
  const [online, setOnline] = useState(false);
  const [driverSection, setDriverSection] = useState<"overview" | "documents" | "settings">("overview");

  return (
    <main dir="rtl" className="min-h-screen bg-canvas font-sans text-ink">
      <header className="relative z-20 flex h-20 items-center justify-between border-b border-line bg-white px-5 shadow-soft lg:px-10">
        <div className="flex items-center gap-3">
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
          <div className="hidden h-8 border-r border-line sm:block" />
          <p className="hidden font-extrabold sm:block">بوابة السائق</p>
        </div>
        <div className="flex items-center gap-3">
          <Action className="relative grid size-11 place-items-center rounded-full bg-cloud text-ink">
            <Icon name="clock" />
            <span className="absolute left-1 top-1 size-2 rounded-full bg-brand" />
          </Action>
          <Action className="flex items-center gap-3 rounded-full bg-ink py-2 pe-4 ps-2 text-white">
            <div className="grid size-8 place-items-center rounded-full bg-brand"><Icon name="user" className="size-4" /></div>
            <span className="hidden text-sm font-extrabold sm:block">خالد أحمد</span>
          </Action>
          <Action onClick={onBack} className="flex items-center gap-2 rounded-full border border-line bg-white px-4 py-2.5 text-sm font-extrabold text-ink">
            <Icon name="arrow" className="size-4" />
            <span className="hidden sm:block">رجوع</span>
          </Action>
        </div>
      </header>

      <div className="mx-auto max-w-7xl px-5 py-8 lg:px-10">
        <div className="mb-7 flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="mb-2 text-sm font-extrabold text-brand">حساب السائق</p>
            <p className="text-3xl font-black sm:text-4xl">مرحباً، خالد</p>
            <p className="mt-2 text-muted">أدر رحلاتك وأرباحك وبيانات حسابك من مكان واحد.</p>
          </div>
          <Action onClick={() => setOnline((current) => !current)} className={`flex items-center justify-between gap-5 rounded-2xl px-5 py-3 font-extrabold shadow-soft ${online ? "bg-brand text-white" : "bg-white text-muted"}`}>
            <span>{online ? "متاح لاستقبال الرحلات" : "غير متصل"}</span>
            <div className={`flex h-7 w-12 items-center rounded-full p-1 ${online ? "justify-start bg-white/25" : "justify-end bg-line"}`}>
              <span className={`size-5 rounded-full ${online ? "bg-white" : "bg-muted"}`} />
            </div>
          </Action>
        </div>

        <div className="mb-7 flex gap-2 overflow-auto rounded-2xl bg-white p-2 shadow-soft">
          {[
            ["overview", "نظرة عامة"],
            ["documents", "المستندات والمركبة"],
            ["settings", "إعدادات الحساب"],
          ].map(([id, label]) => (
            <Action key={id} onClick={() => setDriverSection(id as typeof driverSection)} className={`whitespace-nowrap rounded-xl px-5 py-3 text-sm font-extrabold ${driverSection === id ? "bg-ink text-white" : "text-muted hover:bg-cloud"}`}>
              {label}
            </Action>
          ))}
        </div>

        {driverSection === "overview" && (
          <>
            <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {[
                ["أرباح اليوم", "286 ر.س", "wallet" as const, "+18%"],
                ["الرحلات", "12", "car" as const, "رحلة مكتملة"],
                ["ساعات العمل", "6.5", "clock" as const, "ساعة اليوم"],
                ["التقييم", "4.9", "shield" as const, "من 5.0"],
              ].map(([title, value, icon, meta]) => (
                <div key={title} className="rounded-3xl bg-white p-5 shadow-soft">
                  <div className="mb-5 flex items-center justify-between">
                    <div className="grid size-11 place-items-center rounded-xl bg-brand-soft text-brand"><Icon name={icon as IconName} /></div>
                    <span className="text-xs font-bold text-muted">{meta}</span>
                  </div>
                  <p className="text-sm font-bold text-muted">{title}</p>
                  <p className="mt-2 text-3xl font-black">{value}</p>
                </div>
              ))}
            </div>

            <div className="grid gap-6 lg:grid-cols-3">
              <div className="rounded-3xl bg-white p-6 shadow-soft lg:col-span-2">
                <div className="mb-6 flex items-center justify-between"><p className="text-lg font-black">آخر الرحلات</p><Action className="text-sm font-extrabold text-brand">عرض الكل</Action></div>
                <div className="space-y-3">
                  {[
                    ["واجهة الرياض", "حي النرجس", "38 ر.س", "10:35 ص"],
                    ["مطار الملك خالد", "حي العليا", "74 ر.س", "9:10 ص"],
                    ["جامعة الملك سعود", "حي الملقا", "42 ر.س", "8:20 ص"],
                  ].map(([destination, pickup, price, time]) => (
                    <div key={time} className="flex items-center gap-4 rounded-2xl border border-line p-4">
                      <div className="grid size-11 shrink-0 place-items-center rounded-xl bg-cloud text-ink"><Icon name="pin" /></div>
                      <div className="min-w-0 flex-1"><p className="truncate font-extrabold">{pickup} ← {destination}</p><p className="mt-1 text-xs text-muted">{time} · مكتملة</p></div>
                      <p className="font-black">{price}</p>
                    </div>
                  ))}
                </div>
              </div>
              <div className="rounded-3xl bg-ink p-6 text-white shadow-button">
                <div className="mb-7 flex items-center justify-between">
                  <div className="grid size-12 place-items-center rounded-2xl bg-brand text-white"><Icon name="wallet" /></div>
                  <span className="rounded-full bg-white/10 px-3 py-1 text-xs font-bold text-brand">هذا الأسبوع</span>
                </div>
                <p className="text-sm text-white/60">إجمالي الأرباح</p>
                <p className="mt-2 text-4xl font-black">1,840 <span className="text-lg">ر.س</span></p>
                <div className="my-6 h-2 overflow-hidden rounded-full bg-white/10"><div className="h-full w-3/4 rounded-full bg-brand" /></div>
                <div className="flex justify-between text-xs text-white/60"><span>هدف الأسبوع</span><span>2,500 ر.س</span></div>
                <Action className="mt-7 rounded-2xl bg-white py-3 text-center font-extrabold text-ink">تحويل الأرباح</Action>
              </div>
            </div>
          </>
        )}

        {driverSection === "documents" && (
          <div className="grid gap-6 lg:grid-cols-3">
            <div className="rounded-3xl bg-white p-6 shadow-soft lg:col-span-2">
              <div className="mb-6 flex items-center justify-between"><p className="text-lg font-black">المستندات</p><span className="rounded-full bg-brand-soft px-3 py-1 text-xs font-extrabold text-brand">الحساب موثّق</span></div>
              {[
                ["الهوية الوطنية", "تنتهي في 18 يونيو 2027", "تم التحقق"],
                ["رخصة القيادة", "تنتهي في 03 مارس 2026", "تم التحقق"],
                ["استمارة المركبة", "تنتهي في 21 ديسمبر 2025", "تحديث قريباً"],
                ["التأمين", "تنتهي في 09 سبتمبر 2026", "تم التحقق"],
              ].map(([title, copy, status]) => (
                <Action key={title} className="flex items-center gap-4 border-b border-line py-4 last:border-0">
                  <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="document" /></div>
                  <div className="flex-1"><p className="font-extrabold">{title}</p><p className="text-xs text-muted">{copy}</p></div>
                  <span className={`text-xs font-extrabold ${status === "تحديث قريباً" ? "text-danger" : "text-brand"}`}>{status}</span>
                </Action>
              ))}
            </div>
            <div className="rounded-3xl bg-white p-6 shadow-soft">
              <div className="mb-6 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand"><Icon name="car" className="size-7" /></div>
              <p className="text-xl font-black">تويوتا كامري</p>
              <p className="mt-1 text-sm text-muted">أبيض · موديل 2023</p>
              <div className="my-6 rounded-2xl bg-cloud p-4 text-center"><p className="text-xs text-muted">رقم اللوحة</p><p className="mt-2 text-xl font-black tracking-widest">أ ب ج 2841</p></div>
              <Action className="rounded-2xl border border-line py-3 text-center font-extrabold">تحديث بيانات المركبة</Action>
            </div>
          </div>
        )}

        {driverSection === "settings" && (
          <div className="max-w-3xl rounded-3xl bg-white p-5 shadow-soft sm:p-7">
            {[
              ["البيانات الشخصية", "الاسم، الجوال والصورة الشخصية", "user" as const],
              ["الحساب البنكي", "إدارة الآيبان وتحويل الأرباح", "wallet" as const],
              ["إعدادات الرحلات", "نطاق العمل وتفضيلات الطلبات", "car" as const],
              ["الإشعارات", "تنبيهات الرحلات والأرباح", "clock" as const],
              ["اللغة", "العربية", "document" as const],
              ["المساعدة والدعم", "تواصل مع فريق دعم السائقين", "shield" as const],
            ].map(([title, copy, icon]) => (
              <Action key={title} className="flex items-center gap-4 border-b border-line py-4 last:border-0">
                <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name={icon as IconName} /></div>
                <div className="flex-1"><p className="font-extrabold">{title}</p><p className="text-xs text-muted">{copy}</p></div>
                <Icon name="chevron" className="size-4 text-muted" />
              </Action>
            ))}
            <Action onClick={onLogout} className="mt-6 rounded-2xl border border-danger py-3 text-center font-extrabold text-danger">تسجيل الخروج</Action>
          </div>
        )}
      </div>
    </main>
  );
}

type AccountPanel = "main" | "language" | "notifications" | "contact" | "terms" | "delete";

function InnerScreen({
  screen,
  onNavigate,
  language,
  onLanguage,
  onLogout,
}: {
  screen: Exclude<Screen, "home">;
  onNavigate: (screen: Screen) => void;
  language: "ar" | "en";
  onLanguage: (language: "ar" | "en") => void;
  onLogout: () => void;
}) {
  const [accountPanel, setAccountPanel] = useState<AccountPanel>("main");
  const [walletBalance, setWalletBalance] = useState(125);
  const [showTopUp, setShowTopUp] = useState(false);
  const [topUpAmount, setTopUpAmount] = useState(100);
  const [topUpSuccess, setTopUpSuccess] = useState(false);
  const [notificationPrefs, setNotificationPrefs] = useState({
    trips: true,
    offers: true,
    wallet: true,
    safety: true,
  });

  if (screen === "rides") {
    return (
      <div className="page-wrap">
        <ScreenTitle eyebrow="نشاطك" title="رحلاتي" copy="راجع رحلاتك السابقة، تفاصيل الدفع، واطلب نفس الرحلة من جديد." />
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="rounded-3xl bg-white p-4 shadow-soft sm:p-6 lg:col-span-2">
            <div className="mb-5 flex items-center justify-between">
              <p className="text-lg font-extrabold">آخر الرحلات</p>
              <Action className="rounded-full bg-brand-soft px-4 py-2 text-xs font-extrabold text-brand">الكل</Action>
            </div>
            <div className="space-y-3">
              {tripHistory.map((trip) => (
                <Action key={trip.date} className="flex items-center gap-4 rounded-2xl border border-line p-4 hover:border-brand">
                  <div className="grid size-12 shrink-0 place-items-center rounded-2xl bg-cloud text-ink">
                    <Icon name="car" className="size-6" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p className="truncate font-extrabold">{trip.place}</p>
                      <span className={`text-xs font-bold ${trip.status === "ملغاة" ? "text-muted" : "text-brand"}`}>{trip.status}</span>
                    </div>
                    <p className="mt-1 text-sm text-muted">{trip.date}</p>
                  </div>
                  <div className="text-left">
                    <p className="font-black">{trip.price}</p>
                    <Icon name="chevron" className="mr-auto mt-1 size-4 text-muted" />
                  </div>
                </Action>
              ))}
            </div>
          </div>
          <div className="rounded-3xl bg-ink p-6 text-white shadow-button">
            <div className="mb-8 grid size-12 place-items-center rounded-2xl bg-white/10 text-brand">
              <Icon name="clock" className="size-6" />
            </div>
            <p className="text-xl font-black">وجهتك المعتادة أقرب</p>
            <p className="mt-3 text-sm leading-7 text-white/70">احفظ الأماكن التي تزورها كثيراً لطلب رحلتك بخطوة واحدة.</p>
            <Action className="mt-8 rounded-2xl bg-brand py-3 text-center font-extrabold" onClick={() => onNavigate("home")}>
              احجز رحلة الآن
            </Action>
          </div>
        </div>
      </div>
    );
  }

  if (screen === "wallet") {
    if (showTopUp) {
      return (
        <div className="page-wrap flex items-center justify-center">
          <div className="w-full max-w-lg rounded-3xl bg-white p-6 shadow-panel sm:p-8">
            {topUpSuccess ? (
              <div className="py-8 text-center">
                <div className="mx-auto mb-6 grid size-20 place-items-center rounded-full bg-brand-soft text-brand"><Icon name="check" className="size-10" /></div>
                <p className="text-3xl font-black">تم شحن المحفظة</p>
                <p className="mt-3 text-muted">تمت إضافة {topUpAmount} ر.س إلى رصيد محفظتك بنجاح.</p>
                <p className="mt-7 text-sm font-bold text-muted">الرصيد الجديد</p>
                <p className="mt-2 text-4xl font-black text-brand">{walletBalance}.00 <span className="text-lg">ر.س</span></p>
                <Action onClick={() => { setShowTopUp(false); setTopUpSuccess(false); }} className="mt-8 rounded-2xl bg-ink py-4 font-extrabold text-white">
                  العودة إلى المحفظة
                </Action>
              </div>
            ) : (
              <>
                <Action onClick={() => setShowTopUp(false)} className="mb-7 flex w-fit items-center gap-2 rounded-full bg-cloud px-4 py-2 text-sm font-extrabold">
                  <Icon name="arrow" className="size-4" />
                  رجوع
                </Action>
                <p className="text-3xl font-black">شحن المحفظة</p>
                <p className="mt-3 text-muted">اختر المبلغ الذي تريد إضافته إلى رصيد ATA.</p>
                <div className="my-7 rounded-3xl bg-ink p-6 text-center text-white">
                  <p className="text-sm text-white/60">مبلغ الشحن</p>
                  <p className="mt-2 text-4xl font-black">{topUpAmount}.00 <span className="text-lg">ر.س</span></p>
                </div>
                <div className="mb-7 grid grid-cols-3 gap-3">
                  {[50, 100, 200].map((amount) => (
                    <Action key={amount} onClick={() => setTopUpAmount(amount)} className={`rounded-2xl border-2 py-3 text-center font-extrabold ${topUpAmount === amount ? "border-brand bg-brand-soft text-brand" : "border-line"}`}>
                      {amount} ر.س
                    </Action>
                  ))}
                </div>
                <div className="mb-6 flex items-center justify-between rounded-2xl border border-line p-4">
                  <div className="flex items-center gap-3">
                    <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="wallet" /></div>
                    <div><p className="font-extrabold">بطاقة مدى</p><p className="text-xs text-muted">تنتهي بـ 2841</p></div>
                  </div>
                  <Icon name="chevron" className="size-4 text-muted" />
                </div>
                <Action
                  onClick={() => {
                    setWalletBalance((balance) => balance + topUpAmount);
                    setTopUpSuccess(true);
                  }}
                  className="rounded-2xl bg-ink py-4 text-center font-extrabold text-white shadow-button"
                >
                  تأكيد شحن {topUpAmount} ر.س
                </Action>
              </>
            )}
          </div>
        </div>
      );
    }

    return (
      <div className="page-wrap">
        <ScreenTitle eyebrow="مدفوعات آمنة" title="محفظة ATA" copy="تحكم في رصيدك وطرق الدفع، واطّلع على كل معاملاتك من مكان واحد." />
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="relative overflow-hidden rounded-3xl bg-ink p-7 text-white shadow-button">
            <div className="absolute -left-12 -top-12 size-40 rounded-full bg-brand/20" />
            <div className="relative">
              <div className="flex items-center justify-between">
                <img src={logo} alt="ATA" className="h-9 w-20 rounded-lg bg-white object-contain px-2" />
                <Icon name="wallet" className="size-6 text-brand" />
              </div>
              <p className="mt-12 text-sm text-white/60">رصيدك الحالي</p>
              <p className="mt-2 text-4xl font-black">{walletBalance}.00 <span className="text-lg">ر.س</span></p>
              <p className="mt-10 text-sm tracking-widest text-white/70">••••  2841</p>
            </div>
          </div>
          <div className="rounded-3xl bg-white p-6 shadow-soft lg:col-span-2">
            <div className="mb-5 flex items-center justify-between">
              <p className="text-lg font-extrabold">طرق الدفع</p>
              <Action onClick={() => setShowTopUp(true)} className="flex items-center gap-2 rounded-full bg-brand px-4 py-2 text-sm font-extrabold text-white">
                <Icon name="plus" className="size-4" />
                شحن المحفظة
              </Action>
            </div>
            <div className="space-y-3">
              <Action className="flex items-center gap-4 rounded-2xl border-2 border-brand bg-brand-soft p-4">
                <div className="grid size-11 place-items-center rounded-xl bg-white text-brand"><Icon name="wallet" /></div>
                <div className="flex-1"><p className="font-extrabold">محفظة ATA</p><p className="text-xs text-muted">الرصيد: {walletBalance}.00 ر.س</p></div>
                <span className="size-4 rounded-full border-4 border-brand bg-white" />
              </Action>
              <Action className="flex items-center gap-4 rounded-2xl border border-line p-4">
                <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="wallet" /></div>
                <div className="flex-1"><p className="font-extrabold">بطاقة مدى</p><p className="text-xs text-muted">تنتهي بـ 2841</p></div>
                <span className="size-4 rounded-full border-2 border-line bg-white" />
              </Action>
              <Action className="flex items-center gap-4 rounded-2xl border border-line p-4">
                <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="wallet" /></div>
                <div className="flex-1"><p className="font-extrabold">الدفع نقداً</p><p className="text-xs text-muted">ادفع للكابتن بعد الرحلة</p></div>
                <span className="size-4 rounded-full border-2 border-line bg-white" />
              </Action>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (screen === "safety") {
    return (
      <div className="page-wrap">
        <ScreenTitle eyebrow="السلامة أولاً" title="أمانك في كل رحلة" copy="أدوات ذكية وفريق دعم متاح دائماً ليمنحك تجربة مطمئنة من الانطلاق حتى الوصول." />
        <div className="grid gap-4 md:grid-cols-3">
          {[
            ["مشاركة الرحلة", "أرسل مسارك ومعلومات الكابتن لأشخاص تثق بهم.", "pin" as const],
            ["مركز المساعدة", "تواصل مباشرة مع فريق السلامة على مدار الساعة.", "shield" as const],
            ["جهات موثوقة", "أضف أشخاصاً ليتم تنبيههم عند الحاجة.", "user" as const],
          ].map(([title, copy, icon]) => (
            <Action key={title} className="group rounded-3xl bg-white p-6 shadow-soft hover:-translate-y-1 hover:shadow-float">
              <div className="mb-8 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand transition group-hover:bg-brand group-hover:text-white">
                <Icon name={icon as IconName} className="size-7" />
              </div>
              <p className="text-xl font-black">{title}</p>
              <p className="mt-3 text-sm leading-7 text-muted">{copy}</p>
              <div className="mt-6 flex items-center gap-2 text-sm font-extrabold text-brand">معرفة المزيد <Icon name="arrow" className="size-4" /></div>
            </Action>
          ))}
        </div>
        <div className="mt-6 flex flex-col items-start justify-between gap-5 rounded-3xl bg-ink p-7 text-white sm:flex-row sm:items-center">
          <div className="flex items-center gap-4">
            <div className="grid size-14 place-items-center rounded-full bg-brand text-white"><Icon name="shield" className="size-7" /></div>
            <div><p className="text-lg font-black">هل تحتاج مساعدة عاجلة؟</p><p className="mt-1 text-sm text-white/60">فريق السلامة متاح الآن</p></div>
          </div>
          <Action className="w-full rounded-2xl bg-white px-6 py-3 text-center font-extrabold text-ink sm:w-auto">تواصل معنا</Action>
        </div>
      </div>
    );
  }

  if (accountPanel === "language") {
    return (
      <div className="page-wrap">
        <Action onClick={() => setAccountPanel("main")} className="mb-6 flex w-fit items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-extrabold shadow-soft">
          <Icon name="arrow" className="size-4" />
          العودة إلى الإعدادات
        </Action>
        <ScreenTitle eyebrow="التفضيلات" title="لغة التطبيق" copy="اختر اللغة التي تفضل استخدامها داخل تطبيق ATA." />
        <div className="max-w-2xl rounded-3xl bg-white p-5 shadow-soft">
          {[
            { id: "ar" as const, title: "العربية", copy: "العربية — المملكة العربية السعودية", tag: "AR" },
            { id: "en" as const, title: "English", copy: "English — United Kingdom", tag: "EN" },
          ].map((option) => (
            <Action
              key={option.id}
              onClick={() => onLanguage(option.id)}
              className={`mb-3 flex items-center gap-4 rounded-2xl border-2 p-4 last:mb-0 ${language === option.id ? "border-brand bg-brand-soft" : "border-line"}`}
            >
              <div className={`grid size-12 place-items-center rounded-xl font-black ${language === option.id ? "bg-brand text-white" : "bg-cloud text-ink"}`}>{option.tag}</div>
              <div className="flex-1"><p className="font-extrabold">{option.title}</p><p className="text-sm text-muted">{option.copy}</p></div>
              <span className={`grid size-6 place-items-center rounded-full border-2 ${language === option.id ? "border-brand bg-brand text-white" : "border-line"}`}>
                {language === option.id && <Icon name="check" className="size-4" />}
              </span>
            </Action>
          ))}
          <p className="mt-5 rounded-2xl bg-cloud p-4 text-sm leading-7 text-muted">سيتم حفظ اختيار اللغة تلقائياً واستخدامه عند فتح التطبيق مرة أخرى.</p>
        </div>
      </div>
    );
  }

  if (accountPanel === "notifications") {
    const notificationOptions = [
      { id: "trips" as const, title: "تنبيهات الرحلات", copy: "حالة الطلب، وصول السائق، وتحديثات الرحلة", icon: "car" as const },
      { id: "wallet" as const, title: "المحفظة والمدفوعات", copy: "عمليات الشحن، الخصم، وإيصالات الرحلات", icon: "wallet" as const },
      { id: "safety" as const, title: "تنبيهات السلامة", copy: "التنبيهات المهمة وتحديثات الأمان", icon: "shield" as const },
      { id: "offers" as const, title: "العروض والأخبار", copy: "الخصومات والعروض الحصرية من ATA", icon: "clock" as const },
    ];

    return (
      <div className="page-wrap">
        <Action onClick={() => setAccountPanel("main")} className="mb-6 flex w-fit items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-extrabold shadow-soft">
          <Icon name="arrow" className="size-4" />
          العودة إلى الإعدادات
        </Action>
        <ScreenTitle eyebrow="ابقَ على اطلاع" title="الإشعارات" copy="اختر التنبيهات التي ترغب في استقبالها من ATA." />
        <div className="max-w-2xl rounded-3xl bg-white p-5 shadow-soft">
          {notificationOptions.map((option) => {
            const enabled = notificationPrefs[option.id];
            return (
              <Action
                key={option.id}
                onClick={() => setNotificationPrefs((current) => ({ ...current, [option.id]: !enabled }))}
                className="flex items-center gap-4 border-b border-line py-5 first:pt-0 last:border-0 last:pb-0"
              >
                <div className="grid size-12 place-items-center rounded-xl bg-cloud text-ink"><Icon name={option.icon} /></div>
                <div className="flex-1"><p className="font-extrabold">{option.title}</p><p className="mt-1 text-xs text-muted">{option.copy}</p></div>
                <div className={`flex h-7 w-12 items-center rounded-full p-1 transition ${enabled ? "justify-start bg-brand" : "justify-end bg-line"}`}>
                  <span className="size-5 rounded-full bg-white shadow-soft" />
                </div>
              </Action>
            );
          })}
        </div>
      </div>
    );
  }

  if (accountPanel === "contact") {
    return (
      <div className="page-wrap">
        <Action onClick={() => setAccountPanel("main")} className="mb-6 flex w-fit items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-extrabold shadow-soft">
          <Icon name="arrow" className="size-4" />
          العودة إلى الإعدادات
        </Action>
        <ScreenTitle eyebrow="نحن هنا لمساعدتك" title="اتصل بنا" copy="اختر الطريقة الأنسب للتواصل مع فريق دعم ATA." />
        <div className="grid max-w-4xl gap-4 md:grid-cols-3">
          {[
            ["المحادثة المباشرة", "متاحون الآن", "ابدأ المحادثة", "user" as const],
            ["اتصل بنا", "9200 123 45", "يومياً، على مدار الساعة", "phone" as const],
            ["البريد الإلكتروني", "help@ata.sa", "نرد خلال 24 ساعة", "document" as const],
          ].map(([title, value, copy, icon]) => (
            <Action key={title} className="rounded-3xl bg-white p-6 shadow-soft hover:-translate-y-1 hover:shadow-float">
              <div className="mb-8 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand"><Icon name={icon as IconName} className="size-7" /></div>
              <p className="text-lg font-black">{title}</p>
              <p className="mt-3 font-extrabold text-brand" dir="auto">{value}</p>
              <p className="mt-2 text-sm text-muted">{copy}</p>
            </Action>
          ))}
        </div>
      </div>
    );
  }

  if (accountPanel === "terms") {
    return (
      <div className="page-wrap">
        <Action onClick={() => setAccountPanel("main")} className="mb-6 flex w-fit items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-extrabold shadow-soft">
          <Icon name="arrow" className="size-4" />
          العودة إلى الإعدادات
        </Action>
        <ScreenTitle eyebrow="آخر تحديث: يناير 2025" title="الشروط والأحكام" copy="يرجى قراءة شروط استخدام خدمات ATA بعناية." />
        <div className="max-w-3xl rounded-3xl bg-white p-6 shadow-soft sm:p-8">
          {[
            ["1. استخدام الخدمة", "يوفر تطبيق ATA منصة تقنية لطلب خدمات النقل. باستخدام التطبيق، يقر المستخدم بصحة البيانات المقدمة والتزامه بالأنظمة المعمول بها."],
            ["2. الحساب والمسؤولية", "يتحمل المستخدم مسؤولية حماية رقم جواله وحسابه، وإبلاغ فريق الدعم فوراً عند الاشتباه في أي استخدام غير مصرح به."],
            ["3. الرحلات والمدفوعات", "تظهر تكلفة الرحلة التقديرية قبل الطلب، وقد تتغير وفقاً للمسافة والوقت الفعليين أو الرسوم النظامية الإضافية."],
            ["4. الخصوصية", "تُعالج بيانات الموقع والرحلات بهدف تقديم الخدمة وتحسينها، وفق سياسة الخصوصية ومعايير حماية البيانات المعتمدة."],
          ].map(([title, copy]) => (
            <div key={title} className="border-b border-line py-5 first:pt-0 last:border-0 last:pb-0">
              <p className="font-black">{title}</p>
              <p className="mt-3 text-sm leading-7 text-muted">{copy}</p>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (accountPanel === "delete") {
    return (
      <div className="page-wrap flex items-center justify-center">
        <div className="w-full max-w-lg rounded-3xl bg-white p-7 text-center shadow-panel">
          <div className="mx-auto mb-6 grid size-16 place-items-center rounded-full bg-danger-soft text-danger"><Icon name="document" className="size-8" /></div>
          <p className="text-3xl font-black">حذف التطبيق والبيانات؟</p>
          <p className="mt-4 leading-7 text-muted">سيتم حذف حسابك وسجل رحلاتك وبياناتك المحفوظة نهائياً. لا يمكن التراجع عن هذا الإجراء.</p>
          <div className="mt-7 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">لن تتمكن من استعادة بيانات الحساب بعد تأكيد الحذف.</div>
          <Action onClick={onLogout} className="mt-6 rounded-2xl bg-danger py-4 font-extrabold text-white">تأكيد الحذف</Action>
          <Action onClick={() => setAccountPanel("main")} className="mt-3 rounded-2xl bg-cloud py-4 font-extrabold text-ink">إلغاء</Action>
        </div>
      </div>
    );
  }

  return (
    <div className="page-wrap">
      <ScreenTitle
        eyebrow={language === "ar" ? "حسابي" : "My account"}
        title={language === "ar" ? "مرحباً، عبدالله" : "Welcome, Abdullah"}
        copy={language === "ar" ? "أدر بياناتك، إعدادات رحلاتك، وخيارات الخصوصية." : "Manage your details, ride settings, and privacy preferences."}
      />
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="rounded-3xl bg-white p-6 text-center shadow-soft">
          <div className="mx-auto grid size-24 place-items-center rounded-full bg-brand-soft text-brand"><Icon name="user" className="size-11" /></div>
          <p className="mt-4 text-xl font-black">عبدالله محمد</p>
          <p className="mt-1 text-sm text-muted">عضو منذ 2024</p>
          <div className="mt-5 rounded-2xl bg-cloud p-4"><p className="text-2xl font-black text-brand">4.9</p><p className="text-xs font-bold text-muted">تقييم الركاب</p></div>
        </div>
        <div className="rounded-3xl bg-white p-4 shadow-soft sm:p-6 lg:col-span-2">
          <p className="mb-5 text-lg font-extrabold">{language === "ar" ? "الإعدادات" : "Settings"}</p>
          {[
            ["البيانات الشخصية", "الاسم، رقم الجوال والبريد", "user" as const],
            ["الأماكن المحفوظة", "المنزل والعمل", "home" as const],
            ["الخصوصية والأمان", "إدارة بياناتك وصلاحياتك", "shield" as const],
          ].map(([title, copy, icon]) => (
            <Action key={title} className="flex items-center gap-4 border-b border-line py-4 last:border-0">
              <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name={icon as IconName} /></div>
              <div className="flex-1"><p className="font-extrabold">{title}</p><p className="text-xs text-muted">{copy}</p></div>
              <Icon name="chevron" className="size-4 text-muted" />
            </Action>
          ))}
          <Action onClick={() => setAccountPanel("notifications")} className="flex items-center gap-4 border-b border-line py-4">
            <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="clock" /></div>
            <div className="flex-1"><p className="font-extrabold">{language === "ar" ? "الإشعارات" : "Notifications"}</p><p className="text-xs text-muted">{language === "ar" ? "تحكم في التنبيهات والعروض" : "Manage alerts and offers"}</p></div>
            <div className="size-2 rounded-full bg-brand" />
            <Icon name="chevron" className="size-4 text-muted" />
          </Action>
          <Action onClick={() => setAccountPanel("language")} className="flex items-center gap-4 border-b border-line py-4">
            <div className="grid size-11 place-items-center rounded-xl bg-brand-soft text-sm font-black text-brand">{language.toUpperCase()}</div>
            <div className="flex-1"><p className="font-extrabold">{language === "ar" ? "اللغة" : "Language"}</p><p className="text-xs text-muted">{language === "ar" ? "العربية" : "English"}</p></div>
            <Icon name="chevron" className="size-4 text-muted" />
          </Action>
          <Action onClick={() => setAccountPanel("contact")} className="flex items-center gap-4 border-b border-line py-4">
            <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="phone" /></div>
            <div className="flex-1"><p className="font-extrabold">{language === "ar" ? "اتصل بنا" : "Contact us"}</p><p className="text-xs text-muted">{language === "ar" ? "الدعم والمساعدة" : "Support and help"}</p></div>
            <Icon name="chevron" className="size-4 text-muted" />
          </Action>
          <Action onClick={() => setAccountPanel("terms")} className="flex items-center gap-4 border-b border-line py-4">
            <div className="grid size-11 place-items-center rounded-xl bg-cloud text-ink"><Icon name="document" /></div>
            <div className="flex-1"><p className="font-extrabold">{language === "ar" ? "الشروط والأحكام" : "Terms and conditions"}</p><p className="text-xs text-muted">{language === "ar" ? "شروط استخدام خدمات ATA" : "ATA service terms"}</p></div>
            <Icon name="chevron" className="size-4 text-muted" />
          </Action>
          <div className="mt-5 grid gap-3 sm:grid-cols-2">
            <Action onClick={onLogout} className="rounded-2xl border border-line py-3 text-center font-extrabold text-ink hover:bg-cloud">
              {language === "ar" ? "تسجيل الخروج" : "Log out"}
            </Action>
            <Action onClick={() => setAccountPanel("delete")} className="rounded-2xl bg-danger-soft py-3 text-center font-extrabold text-danger">
              {language === "ar" ? "حذف التطبيق" : "Delete app"}
            </Action>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function App() {
  const [authStage, setAuthStage] = useState<AuthStage>("language");
  const [userRole, setUserRole] = useState<UserRole | null>(null);
  const [language, setLanguage] = useState<"ar" | "en">(
    () => (localStorage.getItem("ata-language") as "ar" | "en" | null) ?? "ar",
  );
  const [selectedRide, setSelectedRide] = useState("economy");
  const [requested, setRequested] = useState(false);
  const [femaleDriverOnly, setFemaleDriverOnly] = useState(false);
  const [stops, setStops] = useState<string[]>([]);
  const [showMenu, setShowMenu] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);
  const [activeScreen, setActiveScreen] = useState<Screen>("home");
  const selected = rides.find((ride) => ride.id === selectedRide) ?? rides[0];
  const estimatedPrice = `${Number.parseInt(selected.price, 10) + stops.length * 12} ر.س`;
  const stopOptions = ["النخيل مول", "برج المملكة"];

  useEffect(() => {
    localStorage.setItem("ata-language", language);
    document.documentElement.lang = language;
  }, [language]);

  if (authStage === "driver") {
    return (
      <DriverAccount
        onBack={() => setAuthStage("pending")}
        onLogout={() => {
          setAuthStage("language");
          setUserRole(null);
        }}
      />
    );
  }

  if (authStage !== "app") {
    return (
      <RegistrationFlow
        stage={authStage}
        role={userRole}
        language={language}
        onStage={setAuthStage}
        onRole={setUserRole}
        onLanguage={setLanguage}
      />
    );
  }

  return (
    <main dir="rtl" className="min-h-screen bg-canvas font-sans text-ink">
      <header className="relative z-30 flex h-20 items-center justify-between border-b border-line bg-white px-5 shadow-soft lg:px-10">
        <div className="flex items-center gap-3">
          <Action
            onClick={() => {
              setShowNotifications((current) => !current);
              setShowMenu(false);
            }}
            className="relative grid size-11 place-items-center rounded-full bg-cloud text-ink"
          >
            <Icon name="bell" />
            <span className="absolute right-1 top-1 grid size-4 place-items-center rounded-full bg-brand text-xs font-black text-white">3</span>
          </Action>
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
        </div>
        <div className="hidden items-center gap-8 text-sm font-bold lg:flex">
          <Action onClick={() => setActiveScreen("home")} className={activeScreen === "home" ? "text-brand" : "text-muted hover:text-ink"}>اطلب رحلة</Action>
          <Action onClick={() => setActiveScreen("rides")} className={activeScreen === "rides" ? "text-brand" : "text-muted hover:text-ink"}>رحلاتي</Action>
          <Action onClick={() => setActiveScreen("safety")} className={activeScreen === "safety" ? "text-brand" : "text-muted hover:text-ink"}>السلامة</Action>
          <Action onClick={() => setActiveScreen("account")} className={activeScreen === "account" ? "text-brand" : "text-muted hover:text-ink"}>حسابي</Action>
        </div>
        <div className="flex items-center gap-3">
          <Action onClick={() => setActiveScreen("wallet")} className="hidden items-center gap-2 rounded-full bg-cloud px-4 py-2.5 text-sm font-bold sm:flex">
            <Icon name="wallet" className="size-4" />
            المحفظة
          </Action>
          {activeScreen !== "home" && (
            <Action onClick={() => setActiveScreen("home")} className="flex items-center gap-2 rounded-full border border-line bg-white px-3 py-2.5 text-sm font-extrabold text-ink">
              <Icon name="arrow" className="size-4" />
              <span className="hidden sm:block">رجوع</span>
            </Action>
          )}
          <Action
            onClick={() => {
              setShowMenu((current) => !current);
              setShowNotifications(false);
            }}
            className={`grid size-11 place-items-center rounded-full transition ${showMenu ? "bg-brand text-white" : "bg-ink text-white"}`}
          >
            <Icon name="menu" />
          </Action>
        </div>

        {showNotifications && (
          <div className="absolute right-5 top-16 mt-2 w-80 rounded-3xl border border-line bg-white p-4 shadow-panel lg:right-10">
            <div className="mb-4 flex items-center justify-between">
              <p className="font-black">الإشعارات</p>
              <Action onClick={() => { setActiveScreen("account"); setShowNotifications(false); }} className="text-xs font-extrabold text-brand">إدارة الإشعارات</Action>
            </div>
            {[
              ["السائق في طريقه إليك", "يصل خلال دقيقتين", "car" as const],
              ["تم شحن محفظتك", "تمت إضافة 100 ر.س", "wallet" as const],
              ["عرض جديد لك", "خصم 20% على رحلتك القادمة", "shield" as const],
            ].map(([title, copy, icon]) => (
              <Action key={title} className="flex items-center gap-3 border-b border-line py-3 last:border-0">
                <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand"><Icon name={icon as IconName} className="size-5" /></div>
                <div className="min-w-0 flex-1"><p className="truncate text-sm font-extrabold">{title}</p><p className="mt-1 text-xs text-muted">{copy}</p></div>
                <span className="size-2 rounded-full bg-brand" />
              </Action>
            ))}
          </div>
        )}

        {showMenu && (
          <div className="absolute left-5 top-16 mt-2 w-72 rounded-3xl border border-line bg-white p-3 shadow-panel lg:left-10">
            <div className="mb-2 flex items-center gap-3 rounded-2xl bg-cloud p-3">
              <div className="grid size-11 place-items-center rounded-full bg-ink text-white"><Icon name="user" /></div>
              <div><p className="font-extrabold">عبدالله محمد</p><p className="text-xs text-muted">عرض الملف الشخصي</p></div>
            </div>
            <p className="px-3 pb-2 pt-3 text-xs font-extrabold text-muted">الإعدادات</p>
            {[
              ["إعدادات الحساب", "user" as const],
              ["اللغة", "document" as const],
              ["الإشعارات", "bell" as const],
              ["السلامة والخصوصية", "shield" as const],
              ["اتصل بنا", "phone" as const],
            ].map(([label, icon]) => (
              <Action
                key={label}
                onClick={() => {
                  setActiveScreen(label === "السلامة والخصوصية" ? "safety" : "account");
                  setShowMenu(false);
                }}
                className="flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-extrabold text-ink hover:bg-cloud"
              >
                <Icon name={icon as IconName} className="size-5 text-muted" />
                <span className="flex-1">{label}</span>
                <Icon name="chevron" className="size-4 text-muted" />
              </Action>
            ))}
          </div>
        )}
      </header>

      {activeScreen === "home" ? <div className="app-shell relative flex overflow-hidden">
        <section className="app-shell relative flex-1 bg-map">
          <svg className="absolute inset-0 size-full" viewBox="0 0 1200 800" preserveAspectRatio="xMidYMid slice">
            <path className="map-block" d="M-20 80 170 20l90 120-80 130L0 240ZM310 0h240l20 170-250 30-70-100ZM650 20l180 10 70 170-260 50-60-120ZM970-20l260 40v220l-240-30-70-120ZM30 340l190-40 100 150-80 160-260 20ZM400 290l210-40 90 160-50 170-260-10-80-140ZM780 290l170-40 120 130-100 180-250-20-20-150ZM1080 310l160-30v300l-190-40 20-130ZM30 700l250-50 110 170H0ZM460 650l210-20 80 190H370ZM820 620l210-30 200 160v80H780Z" />
            <path className="road road-wide" d="M-30 680C210 560 240 330 480 310s300 100 450-10 190-230 320-240" />
            <path className="road" d="M130-20c10 190 170 240 190 390s-70 250 0 460M700-20c-70 170 30 270 10 430s-100 230-80 420M1010-20c-50 160 30 270-20 390s-120 210-70 460M-20 180c190 10 290 90 460 40s260-90 400 10 250 40 400 80M-20 510c190 40 300-50 440 20s280 120 430 50 220-10 390 40" />
            <path className="route-shadow" d="M398 552c75-35 73-115 146-154 78-42 133 20 200-25 63-43 51-105 115-142" />
            <path className="route" d="M398 552c75-35 73-115 146-154 78-42 133 20 200-25 63-43 51-105 115-142" />
          </svg>

          <div className="absolute left-[15%] top-[13%] hidden rounded-lg bg-white/80 px-3 py-1 text-xs font-bold text-muted lg:block">حي النخيل</div>
          <div className="absolute left-[50%] top-[54%] hidden rounded-lg bg-white/80 px-3 py-1 text-xs font-bold text-muted lg:block">طريق الملك فهد</div>
          <div className="absolute right-[9%] top-[31%] hidden rounded-lg bg-white/80 px-3 py-1 text-xs font-bold text-muted lg:block">العليا</div>

          <div className="absolute left-[31%] top-[68%] grid size-12 place-items-center rounded-full border-4 border-white bg-ink text-white shadow-float">
            <Icon name="pin" className="size-6" />
          </div>
          <div className="absolute left-[69%] top-[25%] grid size-14 place-items-center rounded-full border-4 border-white bg-brand text-white shadow-float">
            <Icon name="car" className="size-7" />
          </div>
          <div className="absolute left-[66%] top-[42%] hidden rounded-full bg-ink px-3 py-1.5 text-xs font-bold text-white shadow-float md:block">
            دقيقتان
          </div>

          <div className="absolute bottom-5 left-5 flex flex-col gap-3">
            <Action className="grid size-12 place-items-center rounded-full bg-white text-ink shadow-float">
              <Icon name="location" />
            </Action>
            <Action className="grid size-12 place-items-center rounded-full bg-white text-ink shadow-float">
              <Icon name="plus" />
            </Action>
          </div>
        </section>

        <aside className="absolute inset-x-0 bottom-0 z-20 max-h-[78vh] overflow-auto rounded-t-4xl bg-white p-5 shadow-panel lg:static lg:w-108 lg:max-h-none lg:rounded-none lg:p-8">
          <div className="mx-auto mb-4 h-1.5 w-12 rounded-full bg-line lg:hidden" />

          {requested ? (
            <div className="flex min-h-120 flex-col items-center justify-center text-center">
              <div className="relative mb-8 grid size-28 place-items-center rounded-full bg-brand-soft text-brand">
                <div className="absolute inset-0 animate-ping rounded-full bg-brand/10" />
                <Icon name="car" className="relative size-12" />
              </div>
              <p className="mb-2 text-2xl font-extrabold">جاري البحث عن كابتن</p>
              <p className="mb-8 max-w-xs leading-7 text-muted">نبحث لك عن أقرب كابتن. سيصل إليك خلال {selected.eta}.</p>
              <div className="mb-7 flex w-full items-center justify-between rounded-2xl bg-cloud p-4 text-right">
                <div>
                  <p className="font-extrabold">{selected.name}</p>
                  <p className="mt-1 text-sm text-muted">الدفع نقداً عند الوصول</p>
                </div>
                <div className="text-left">
                  <p className="text-lg font-extrabold">{estimatedPrice}</p>
                  {stops.length > 0 && <p className="mt-1 text-xs text-muted">{stops.length} محطة إضافية</p>}
                </div>
              </div>
              {femaleDriverOnly && (
                <div className="mb-7 flex w-full items-center gap-3 rounded-2xl bg-brand-soft p-4 text-right text-brand">
                  <div className="grid size-10 place-items-center rounded-full bg-brand text-white"><Icon name="user" className="size-5" /></div>
                  <div><p className="font-extrabold">تم طلب سائقة</p><p className="text-xs">سنبحث عن أقرب سائقة متاحة</p></div>
                </div>
              )}
              <Action
                onClick={() => setRequested(false)}
                className="w-full rounded-2xl border border-line py-4 text-center font-extrabold hover:bg-cloud"
              >
                إلغاء الطلب
              </Action>
            </div>
          ) : (
            <>
              <div className="mb-6">
                <p className="text-sm font-bold text-brand">أهلاً بك في ATA</p>
                <p className="mt-1 text-3xl font-black tracking-tight">إلى أين تود الذهاب؟</p>
              </div>

              <div className="relative mb-6">
                <div className="absolute right-6 top-10 h-12 border-r-2 border-dashed border-line" />
                <Action className="mb-2 flex items-center gap-3 rounded-2xl bg-cloud p-4">
                  <span className="size-3 shrink-0 rounded-full border-4 border-brand bg-white" />
                  <div className="min-w-0 flex-1 text-right">
                    <p className="text-xs font-bold text-muted">موقع الانطلاق</p>
                    <p className="truncate font-extrabold">موقعك الحالي</p>
                  </div>
                  <Icon name="location" className="size-5 text-brand" />
                </Action>
                {stops.map((stop, index) => (
                  <div key={stop} className="mb-2 flex items-center gap-3 rounded-2xl border border-line bg-white p-4">
                    <span className="grid size-5 shrink-0 place-items-center rounded-full bg-brand-soft text-xs font-black text-brand">{index + 1}</span>
                    <div className="min-w-0 flex-1 text-right">
                      <p className="text-xs font-bold text-muted">محطة إضافية</p>
                      <p className="truncate font-extrabold">{stop}</p>
                    </div>
                    <Action onClick={() => setStops((current) => current.filter((item) => item !== stop))} className="rounded-lg bg-danger-soft px-2 py-1 text-xs font-bold text-danger">حذف</Action>
                  </div>
                ))}
                <Action className="flex items-center gap-3 rounded-2xl border-2 border-brand bg-white p-4 shadow-brand">
                  <span className="size-3 shrink-0 rounded-sm bg-ink" />
                  <div className="min-w-0 flex-1 text-right">
                    <p className="text-xs font-bold text-muted">الوجهة</p>
                    <p className="truncate font-extrabold">واجهة الرياض</p>
                  </div>
                  <Icon name="search" className="size-5 text-muted" />
                </Action>
                <Action
                  onClick={() => {
                    if (stops.length < stopOptions.length) setStops((current) => [...current, stopOptions[current.length]]);
                  }}
                  className={`mt-3 flex items-center justify-center gap-2 rounded-xl border border-dashed py-3 text-sm font-extrabold ${
                    stops.length < stopOptions.length ? "border-brand text-brand" : "cursor-not-allowed border-line text-muted"
                  }`}
                >
                  <Icon name="plus" className="size-4" />
                  {stops.length < stopOptions.length ? "إضافة محطة أخرى" : "تمت إضافة الحد الأقصى للمحطات"}
                </Action>
              </div>

              <div className="mb-6 flex gap-2">
                <Action className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-brand-soft py-3 text-sm font-extrabold text-brand">
                  <Icon name="clock" className="size-4" />
                  الآن
                </Action>
                <Action className="flex flex-1 items-center justify-center gap-2 rounded-xl bg-cloud py-3 text-sm font-extrabold text-muted">
                  <Icon name="clock" className="size-4" />
                  جدولة
                </Action>
              </div>

              <Action
                onClick={() => setFemaleDriverOnly((current) => !current)}
                className={`mb-6 flex items-center gap-3 rounded-2xl border-2 p-4 ${
                  femaleDriverOnly ? "border-brand bg-brand-soft" : "border-line bg-white"
                }`}
              >
                <div className={`grid size-11 place-items-center rounded-full ${femaleDriverOnly ? "bg-brand text-white" : "bg-cloud text-ink"}`}>
                  <Icon name="user" className="size-5" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <p className="font-extrabold">أفضّل سائقة</p>
                    <span className="rounded-full bg-white px-2 py-1 text-xs font-extrabold text-brand">للعميلات</span>
                  </div>
                  <p className="mt-1 text-xs text-muted">خيار مخصص للنساء لطلب سائقة عند توفرها</p>
                </div>
                <div className={`flex h-7 w-12 items-center rounded-full p-1 transition ${femaleDriverOnly ? "justify-start bg-brand" : "justify-end bg-line"}`}>
                  <span className="size-5 rounded-full bg-white shadow-soft" />
                </div>
              </Action>

              <div className="mb-4 flex items-center justify-between">
                <p className="text-lg font-extrabold">اختر رحلتك</p>
                <p className="text-xs font-bold text-muted">الأسعار تقديرية</p>
              </div>
              <div className="space-y-2">
                {rides.map((ride) => (
                  <Action
                    key={ride.id}
                    onClick={() => setSelectedRide(ride.id)}
                    className={`flex items-center gap-3 rounded-2xl border-2 p-3 ${
                      selectedRide === ride.id ? "border-brand bg-brand-soft" : "border-transparent bg-cloud"
                    }`}
                  >
                    <div className={`grid size-12 place-items-center rounded-xl ${selectedRide === ride.id ? "bg-brand text-white" : "bg-white text-ink"}`}>
                      <Icon name={ride.icon} className="size-7" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <p className="font-extrabold">{ride.name}</p>
                        <span className="text-xs font-bold text-brand">{ride.eta}</span>
                      </div>
                      <p className="text-xs text-muted">{ride.detail}</p>
                    </div>
                    <p className="font-black">{ride.price}</p>
                  </Action>
                ))}
              </div>

              <div className="my-5 flex items-center justify-between rounded-xl border border-line px-4 py-3">
                <div className="flex items-center gap-3">
                  <Icon name="wallet" className="size-5 text-brand" />
                  <div>
                    <p className="text-sm font-extrabold">طريقة الدفع</p>
                    <p className="text-xs text-muted">نقداً</p>
                  </div>
                </div>
                <Icon name="chevron" className="size-4 text-muted" />
              </div>

              <Action onClick={() => setRequested(true)} className="flex items-center justify-center gap-3 rounded-2xl bg-ink py-4 font-extrabold text-white shadow-button hover:bg-ink-soft">
                اطلب {selected.name} · {estimatedPrice}
                <Icon name="arrow" className="size-5 rotate-180" />
              </Action>
              <div className="mt-4 flex items-center justify-center gap-2 text-xs font-bold text-muted">
                <Icon name="shield" className="size-4 text-brand" />
                رحلتك آمنة ومتابعة على مدار الساعة
              </div>
            </>
          )}
        </aside>
      </div> : (
        <InnerScreen
          screen={activeScreen}
          onNavigate={setActiveScreen}
          language={language}
          onLanguage={setLanguage}
          onLogout={() => {
            setAuthStage("language");
            setActiveScreen("home");
            setUserRole(null);
          }}
        />
      )}

      {activeScreen !== "home" && (
        <nav className="fixed inset-x-4 bottom-4 z-40 flex items-center justify-around rounded-2xl bg-ink p-2 text-white shadow-float lg:hidden">
          {[
            ["home", "الرئيسية", "home" as const],
            ["rides", "رحلاتي", "car" as const],
            ["wallet", "المحفظة", "wallet" as const],
            ["account", "حسابي", "user" as const],
          ].map(([id, label, icon]) => (
            <Action key={id} onClick={() => setActiveScreen(id as Screen)} className={`flex min-w-16 flex-col items-center gap-1 rounded-xl px-3 py-2 text-xs font-bold ${activeScreen === id ? "bg-brand" : "text-white/60"}`}>
              <Icon name={icon as IconName} className="size-5" />
              {label}
            </Action>
          ))}
        </nav>
      )}
    </main>
  );
}
