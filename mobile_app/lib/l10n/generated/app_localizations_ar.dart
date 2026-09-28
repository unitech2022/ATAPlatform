// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appName => 'ATA';

  @override
  String get back => 'رجوع';

  @override
  String get cancel => 'إلغاء';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String get currency => 'ر.س';

  @override
  String priceWithCurrency(String amount) {
    return '$amount ر.س';
  }

  @override
  String minutesLabel(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count دقيقة',
      few: '$count دقائق',
      two: 'دقيقتان',
      one: 'دقيقة واحدة',
      zero: 'الآن',
    );
    return '$_temp0';
  }

  @override
  String get errorNetwork => 'تعذر الاتصال بالخادم، تحقق من اتصالك بالإنترنت';

  @override
  String get errorUnexpected => 'حدث خطأ غير متوقع، حاول مرة أخرى';

  @override
  String get errorUnauthorized => 'انتهت الجلسة، سجّل الدخول مجدداً';

  @override
  String get loading => 'جارٍ التحميل…';

  @override
  String get comingSoon => 'قريباً';

  @override
  String get keypadDelete => 'حذف';

  @override
  String get languageTitle => 'اختر لغتك · Choose your language';

  @override
  String get languageCopy =>
      'يمكنك تغيير اللغة لاحقاً من الإعدادات · You can change it later in Settings';

  @override
  String get arabicName => 'العربية';

  @override
  String get arabicRegion => 'المملكة العربية السعودية';

  @override
  String get arabicContinue => 'متابعة بالعربية';

  @override
  String get englishName => 'English';

  @override
  String get englishRegion => 'United Kingdom';

  @override
  String get englishContinue => 'Continue in English';

  @override
  String get roleEyebrow => 'ابدأ رحلتك مع ATA';

  @override
  String get roleTitle => 'كيف تريد استخدام التطبيق؟';

  @override
  String get roleCopy => 'اختر نوع الحساب للمتابعة باستخدام رقم جوالك فقط';

  @override
  String get riderTag => 'للركاب';

  @override
  String get riderTitle => 'التسجيل كعميل';

  @override
  String get riderCopy => 'اطلب رحلتك خلال دقائق، وتابع الكابتن حتى يصل إليك.';

  @override
  String get riderCta => 'ابدأ الآن';

  @override
  String get driverTag => 'للسائقين';

  @override
  String get driverTitle => 'التسجيل كسائق';

  @override
  String get driverCopy =>
      'سجّل رقمك، ثم ارفع مستنداتك عبر الموقع ليتم تفعيل حسابك.';

  @override
  String get driverCta => 'انضم إلى ATA';

  @override
  String get roleTerms =>
      'بمتابعتك، أنت توافق على شروط الاستخدام وسياسة الخصوصية';

  @override
  String get phoneTitle => 'أدخل رقم جوالك';

  @override
  String get phoneCopyRider => 'سنرسل لك رمز تحقق لتأكيد الرقم وإنشاء حسابك.';

  @override
  String get phoneCopyDriver =>
      'سنرسل لك رمز تحقق لتأكيد الرقم وبدء طلب الانضمام كسائق.';

  @override
  String get phonePlaceholder => '5X XXX XXXX';

  @override
  String get phoneCountryCode => '+966';

  @override
  String get phoneSend => 'إرسال رمز التحقق';

  @override
  String get phoneInvalid => 'أدخل رقم جوال سعودي صحيح يبدأ بـ 5';

  @override
  String rateLimited(int seconds) {
    return 'تم تجاوز الحد المسموح، حاول بعد $seconds ثانية';
  }

  @override
  String get otpTitle => 'تحقق من رقمك';

  @override
  String get otpCopy => 'أرسلنا رمزاً من 4 أرقام إلى';

  @override
  String get otpVerify => 'تأكيد ومتابعة';

  @override
  String get otpResend => 'إعادة إرسال الرمز';

  @override
  String otpResendIn(int seconds) {
    return 'إعادة الإرسال بعد $seconds ث';
  }

  @override
  String otpDevHint(String code) {
    return 'رمز التطوير: $code';
  }

  @override
  String otpInvalid(int attempts) {
    return 'رمز التحقق غير صحيح، المحاولات المتبقية: $attempts';
  }

  @override
  String get otpExpired => 'انتهت صلاحية الرمز، أعد الإرسال';

  @override
  String get otpLocked => 'تم قفل التحقق مؤقتاً، حاول لاحقاً';

  @override
  String get termsEyebrow => 'خطوة أخيرة';

  @override
  String get termsTitle => 'ما اسمك؟';

  @override
  String get termsCopy =>
      'أدخل اسمك كما تحب أن يناديك الكابتن ووافق على الشروط للمتابعة.';

  @override
  String get fullNameLabel => 'الاسم الكامل';

  @override
  String get fullNameHint => 'مثال: عبدالله محمد';

  @override
  String get acceptTermsLabel => 'أوافق على شروط الاستخدام وسياسة الخصوصية';

  @override
  String get termsContinue => 'ابدأ استخدام ATA';

  @override
  String get pendingTitle => 'تم إنشاء طلبك بنجاح';

  @override
  String get pendingCopy =>
      'حسابك كسائق غير نشط حالياً. أكمل رفع المستندات عبر موقع ATA ليقوم فريق الإدارة بمراجعتها وتفعيل حسابك.';

  @override
  String get pendingReviewTitle => 'طلبك قيد المراجعة';

  @override
  String get pendingReviewCopy =>
      'يراجع فريق الإدارة مستنداتك الآن، وسنعلمك عبر الجوال فور تفعيل حسابك.';

  @override
  String get pendingRejectedTitle => 'تم رفض الطلب';

  @override
  String get pendingRejectedCopy =>
      'راجع سبب الرفض وحدّث مستنداتك عبر موقع ATA ثم أعد إرسال الطلب.';

  @override
  String get pendingSuspendedTitle => 'الحساب موقوف';

  @override
  String get pendingSuspendedCopy =>
      'تم إيقاف حسابك مؤقتاً. تواصل مع فريق دعم السائقين لمزيد من التفاصيل.';

  @override
  String get pendingApprovedTitle => 'تم تفعيل حسابك';

  @override
  String get pendingApprovedCopy =>
      'حسابك كسائق نشط الآن. افتح بوابة السائق لبدء استقبال الرحلات.';

  @override
  String get applicationNumber => 'رقم الطلب';

  @override
  String get step1Title => 'رفع المستندات';

  @override
  String get step1Copy => 'عبر موقع ATA';

  @override
  String get step2Title => 'مراجعة الإدارة';

  @override
  String get step2Copy => 'خلال 24–48 ساعة';

  @override
  String get step3Title => 'تفعيل الحساب';

  @override
  String get step3Copy => 'إشعار عبر الجوال';

  @override
  String get requiredDocuments => 'المستندات المطلوبة';

  @override
  String get docStatusRequired => 'مطلوب';

  @override
  String get docStatusPending => 'قيد المراجعة';

  @override
  String get docStatusVerified => 'مقبول';

  @override
  String get docStatusRejected => 'مرفوض';

  @override
  String get docNationalId => 'الهوية الوطنية أو الإقامة';

  @override
  String get docDrivingLicense => 'رخصة قيادة سارية';

  @override
  String get docVehicleRegistration => 'استمارة المركبة';

  @override
  String get docPersonalPhoto => 'صورة شخصية واضحة';

  @override
  String get openUploadPortal => 'الانتقال لموقع رفع المستندات';

  @override
  String get openDriverPortal => 'فتح بوابة السائق';

  @override
  String get backToLogin => 'العودة إلى تسجيل الدخول';

  @override
  String get refreshStatus => 'تحديث الحالة';

  @override
  String rejectionReason(String reason) {
    return 'سبب الرفض: $reason';
  }

  @override
  String get portalOpenFailed => 'تعذر فتح الرابط';

  @override
  String get navHome => 'الرئيسية';

  @override
  String get navRides => 'رحلاتي';

  @override
  String get navWallet => 'المحفظة';

  @override
  String get navAccount => 'حسابي';

  @override
  String get notificationsTitle => 'الإشعارات';

  @override
  String get manageNotifications => 'إدارة الإشعارات';

  @override
  String get notificationsEmpty => 'لا توجد إشعارات جديدة';

  @override
  String get markAllRead => 'تحديد الكل كمقروء';

  @override
  String get menuViewProfile => 'عرض الملف الشخصي';

  @override
  String get menuSettings => 'الإعدادات';

  @override
  String get menuAccountSettings => 'إعدادات الحساب';

  @override
  String get menuLanguage => 'اللغة';

  @override
  String get menuNotifications => 'الإشعارات';

  @override
  String get menuSafety => 'السلامة والخصوصية';

  @override
  String get menuContact => 'اتصل بنا';

  @override
  String get guestName => 'عميل ATA';

  @override
  String get homeEyebrow => 'أهلاً بك في ATA';

  @override
  String get homeTitle => 'إلى أين تود الذهاب؟';

  @override
  String get pickupLabel => 'موقع الانطلاق';

  @override
  String get pickupCurrent => 'موقعك الحالي';

  @override
  String get stopLabel => 'محطة إضافية';

  @override
  String get removeStop => 'حذف';

  @override
  String get destinationLabel => 'الوجهة';

  @override
  String get destinationDefault => 'واجهة الرياض';

  @override
  String get addStop => 'إضافة محطة أخرى';

  @override
  String get maxStopsReached => 'تمت إضافة الحد الأقصى للمحطات';

  @override
  String get stopOption1 => 'النخيل مول';

  @override
  String get stopOption2 => 'برج المملكة';

  @override
  String get stopOption3 => 'حديقة الملك عبدالله';

  @override
  String get timeNow => 'الآن';

  @override
  String get timeSchedule => 'جدولة';

  @override
  String get scheduleComingSoon => 'الجدولة متاحة قريباً (حتى 7 أيام مسبقاً)';

  @override
  String get femaleDriverTitle => 'أفضّل سائقة';

  @override
  String get femaleDriverTag => 'للعميلات';

  @override
  String get femaleDriverCopy => 'خيار مخصص للنساء لطلب سائقة عند توفرها';

  @override
  String get chooseRide => 'اختر رحلتك';

  @override
  String get pricesEstimated => 'الأسعار تقديرية';

  @override
  String get paymentMethod => 'طريقة الدفع';

  @override
  String get paymentCash => 'نقداً';

  @override
  String get paymentWallet => 'محفظة ATA';

  @override
  String get paymentCard => 'بطاقة';

  @override
  String requestRide(String name, String price) {
    return 'اطلب $name · $price';
  }

  @override
  String get safeRide => 'رحلتك آمنة ومتابعة على مدار الساعة';

  @override
  String get searchingTitle => 'جاري البحث عن كابتن';

  @override
  String searchingCopy(String eta) {
    return 'نبحث لك عن أقرب كابتن. سيصل إليك خلال $eta.';
  }

  @override
  String get cashOnArrival => 'الدفع نقداً عند الوصول';

  @override
  String get payWithWallet => 'الدفع من محفظة ATA';

  @override
  String get payWithCard => 'الدفع بالبطاقة';

  @override
  String extraStopsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count محطة إضافية',
      few: '$count محطات إضافية',
      two: 'محطتان إضافيتان',
      one: 'محطة إضافية واحدة',
    );
    return '$_temp0';
  }

  @override
  String get femaleRequestedTitle => 'تم طلب سائقة';

  @override
  String get femaleRequestedCopy => 'سنبحث عن أقرب سائقة متاحة';

  @override
  String get cancelRequest => 'إلغاء الطلب';

  @override
  String get categoriesError => 'تعذر تحميل فئات الرحلات';

  @override
  String get ridesEyebrow => 'نشاطك';

  @override
  String get ridesTitle => 'رحلاتي';

  @override
  String get ridesCopy =>
      'راجع رحلاتك السابقة، تفاصيل الدفع، واطلب نفس الرحلة من جديد.';

  @override
  String get recentTrips => 'آخر الرحلات';

  @override
  String get allTrips => 'الكل';

  @override
  String get tripStatusCompleted => 'مكتملة';

  @override
  String get tripStatusCancelled => 'ملغاة';

  @override
  String get tripStatusActive => 'جارية';

  @override
  String get ridesEmptyTitle => 'لا توجد رحلات بعد';

  @override
  String get ridesEmptyCopy => 'ستظهر رحلاتك هنا بعد أول طلب.';

  @override
  String get promoTitle => 'وجهتك المعتادة أقرب';

  @override
  String get promoCopy =>
      'احفظ الأماكن التي تزورها كثيراً لطلب رحلتك بخطوة واحدة.';

  @override
  String get promoCta => 'احجز رحلة الآن';

  @override
  String tripRoute(String pickup, String destination) {
    return '$pickup ← $destination';
  }

  @override
  String get walletEyebrow => 'مدفوعات آمنة';

  @override
  String get walletTitle => 'محفظة ATA';

  @override
  String get walletCopy =>
      'تحكم في رصيدك وطرق الدفع، واطّلع على كل معاملاتك من مكان واحد.';

  @override
  String get currentBalance => 'رصيدك الحالي';

  @override
  String get paymentMethods => 'طرق الدفع';

  @override
  String get topUp => 'شحن المحفظة';

  @override
  String walletBalanceLine(String amount) {
    return 'الرصيد: $amount';
  }

  @override
  String get madaCard => 'بطاقة مدى';

  @override
  String cardEnding(String digits) {
    return 'تنتهي بـ $digits';
  }

  @override
  String get payCash => 'الدفع نقداً';

  @override
  String get payCashCopy => 'ادفع للكابتن بعد الرحلة';

  @override
  String get topUpCopy => 'اختر المبلغ الذي تريد إضافته إلى رصيد ATA.';

  @override
  String get topUpAmount => 'مبلغ الشحن';

  @override
  String confirmTopUp(String amount) {
    return 'تأكيد شحن $amount';
  }

  @override
  String get topUpSuccessTitle => 'تم شحن المحفظة';

  @override
  String topUpSuccessCopy(String amount) {
    return 'تمت إضافة $amount إلى رصيد محفظتك بنجاح.';
  }

  @override
  String get newBalance => 'الرصيد الجديد';

  @override
  String get backToWallet => 'العودة إلى المحفظة';

  @override
  String get sandboxMethod => 'الدفع التجريبي';

  @override
  String get sandboxCopy => 'بيئة تجريبية، لا يتم خصم أي مبلغ';

  @override
  String get walletError => 'تعذر تحميل المحفظة';

  @override
  String get safetyEyebrow => 'السلامة أولاً';

  @override
  String get safetyTitle => 'أمانك في كل رحلة';

  @override
  String get safetyCopy =>
      'أدوات ذكية وفريق دعم متاح دائماً ليمنحك تجربة مطمئنة من الانطلاق حتى الوصول.';

  @override
  String get shareTripTitle => 'مشاركة الرحلة';

  @override
  String get shareTripCopy => 'أرسل مسارك ومعلومات الكابتن لأشخاص تثق بهم.';

  @override
  String get helpCenterTitle => 'مركز المساعدة';

  @override
  String get helpCenterCopy => 'تواصل مباشرة مع فريق السلامة على مدار الساعة.';

  @override
  String get trustedContactsTitle => 'جهات موثوقة';

  @override
  String get trustedContactsCopy => 'أضف أشخاصاً ليتم تنبيههم عند الحاجة.';

  @override
  String get learnMore => 'معرفة المزيد';

  @override
  String get emergencyTitle => 'هل تحتاج مساعدة عاجلة؟';

  @override
  String get emergencyCopy => 'فريق السلامة متاح الآن';

  @override
  String get contactUs => 'تواصل معنا';

  @override
  String get accountEyebrow => 'حسابي';

  @override
  String accountWelcome(String name) {
    return 'مرحباً، $name';
  }

  @override
  String get accountCopy => 'أدر بياناتك، إعدادات رحلاتك، وخيارات الخصوصية.';

  @override
  String memberSince(String year) {
    return 'عضو منذ $year';
  }

  @override
  String get passengerRating => 'تقييم الركاب';

  @override
  String get settings => 'الإعدادات';

  @override
  String get personalInfo => 'البيانات الشخصية';

  @override
  String get personalInfoCopy => 'الاسم، رقم الجوال والبريد';

  @override
  String get savedPlaces => 'الأماكن المحفوظة';

  @override
  String get savedPlacesCopy => 'المنزل والعمل';

  @override
  String get privacySecurity => 'الخصوصية والأمان';

  @override
  String get privacySecurityCopy => 'إدارة بياناتك وصلاحياتك';

  @override
  String get notificationsRow => 'الإشعارات';

  @override
  String get notificationsRowCopy => 'تحكم في التنبيهات والعروض';

  @override
  String get languageRow => 'اللغة';

  @override
  String get languageArabic => 'العربية';

  @override
  String get languageEnglish => 'English';

  @override
  String get contactRow => 'اتصل بنا';

  @override
  String get contactRowCopy => 'الدعم والمساعدة';

  @override
  String get termsRow => 'الشروط والأحكام';

  @override
  String get termsRowCopy => 'شروط استخدام خدمات ATA';

  @override
  String get logout => 'تسجيل الخروج';

  @override
  String get deleteApp => 'حذف التطبيق';

  @override
  String get backToSettings => 'العودة إلى الإعدادات';

  @override
  String get languagePanelEyebrow => 'التفضيلات';

  @override
  String get languagePanelTitle => 'لغة التطبيق';

  @override
  String get languagePanelCopy =>
      'اختر اللغة التي تفضل استخدامها داخل تطبيق ATA.';

  @override
  String get languageArabicCopy => 'العربية — المملكة العربية السعودية';

  @override
  String get languageEnglishCopy => 'English — United Kingdom';

  @override
  String get languageSavedNote =>
      'سيتم حفظ اختيار اللغة تلقائياً واستخدامه عند فتح التطبيق مرة أخرى.';

  @override
  String get notifPrefsEyebrow => 'ابقَ على اطلاع';

  @override
  String get notifPrefsTitle => 'الإشعارات';

  @override
  String get notifPrefsCopy => 'اختر التنبيهات التي ترغب في استقبالها من ATA.';

  @override
  String get prefTripsTitle => 'تنبيهات الرحلات';

  @override
  String get prefTripsCopy => 'حالة الطلب، وصول السائق، وتحديثات الرحلة';

  @override
  String get prefWalletTitle => 'المحفظة والمدفوعات';

  @override
  String get prefWalletCopy => 'عمليات الشحن، الخصم، وإيصالات الرحلات';

  @override
  String get prefSafetyTitle => 'تنبيهات السلامة';

  @override
  String get prefSafetyCopy => 'التنبيهات المهمة وتحديثات الأمان';

  @override
  String get prefOffersTitle => 'العروض والأخبار';

  @override
  String get prefOffersCopy => 'الخصومات والعروض الحصرية من ATA';

  @override
  String get contactEyebrow => 'نحن هنا لمساعدتك';

  @override
  String get contactTitle => 'اتصل بنا';

  @override
  String get contactCopy => 'اختر الطريقة الأنسب للتواصل مع فريق دعم ATA.';

  @override
  String get liveChatTitle => 'المحادثة المباشرة';

  @override
  String get liveChatValue => 'متاحون الآن';

  @override
  String get liveChatCopy => 'ابدأ المحادثة';

  @override
  String get callTitle => 'اتصل بنا';

  @override
  String get callValue => '9200 123 45';

  @override
  String get callCopy => 'يومياً، على مدار الساعة';

  @override
  String get emailTitle => 'البريد الإلكتروني';

  @override
  String get emailValue => 'help@ata.sa';

  @override
  String get emailCopy => 'نرد خلال 24 ساعة';

  @override
  String get termsPanelEyebrow => 'آخر تحديث: يناير 2025';

  @override
  String get termsPanelTitle => 'الشروط والأحكام';

  @override
  String get termsPanelCopy => 'يرجى قراءة شروط استخدام خدمات ATA بعناية.';

  @override
  String get terms1Title => '1. استخدام الخدمة';

  @override
  String get terms1Copy =>
      'يوفر تطبيق ATA منصة تقنية لطلب خدمات النقل. باستخدام التطبيق، يقر المستخدم بصحة البيانات المقدمة والتزامه بالأنظمة المعمول بها.';

  @override
  String get terms2Title => '2. الحساب والمسؤولية';

  @override
  String get terms2Copy =>
      'يتحمل المستخدم مسؤولية حماية رقم جواله وحسابه، وإبلاغ فريق الدعم فوراً عند الاشتباه في أي استخدام غير مصرح به.';

  @override
  String get terms3Title => '3. الرحلات والمدفوعات';

  @override
  String get terms3Copy =>
      'تظهر تكلفة الرحلة التقديرية قبل الطلب، وقد تتغير وفقاً للمسافة والوقت الفعليين أو الرسوم النظامية الإضافية.';

  @override
  String get terms4Title => '4. الخصوصية';

  @override
  String get terms4Copy =>
      'تُعالج بيانات الموقع والرحلات بهدف تقديم الخدمة وتحسينها، وفق سياسة الخصوصية ومعايير حماية البيانات المعتمدة.';

  @override
  String get deleteTitle => 'حذف التطبيق والبيانات؟';

  @override
  String get deleteCopy =>
      'سيتم حذف حسابك وسجل رحلاتك وبياناتك المحفوظة نهائياً. لا يمكن التراجع عن هذا الإجراء.';

  @override
  String get deleteWarning =>
      'لن تتمكن من استعادة بيانات الحساب بعد تأكيد الحذف.';

  @override
  String get confirmDelete => 'تأكيد الحذف';

  @override
  String get driverPortal => 'بوابة السائق';

  @override
  String get driverAccountEyebrow => 'حساب السائق';

  @override
  String driverWelcome(String name) {
    return 'مرحباً، $name';
  }

  @override
  String get driverDashboardCopy =>
      'أدر رحلاتك وأرباحك وبيانات حسابك من مكان واحد.';

  @override
  String get driverGuestName => 'كابتن ATA';

  @override
  String get onlineLabel => 'متاح لاستقبال الرحلات';

  @override
  String get offlineLabel => 'غير متصل';

  @override
  String get tabOverview => 'نظرة عامة';

  @override
  String get tabDocuments => 'المستندات والمركبة';

  @override
  String get tabSettings => 'إعدادات الحساب';

  @override
  String get statEarningsToday => 'أرباح اليوم';

  @override
  String get statTrips => 'الرحلات';

  @override
  String get statTripsMeta => 'رحلة مكتملة';

  @override
  String get statHours => 'ساعات العمل';

  @override
  String get statHoursMeta => 'ساعة اليوم';

  @override
  String get statRating => 'التقييم';

  @override
  String get statRatingMeta => 'من 5.0';

  @override
  String get viewAll => 'عرض الكل';

  @override
  String get driverTripsEmpty =>
      'لا توجد رحلات بعد، فعّل حالتك لاستقبال الطلبات.';

  @override
  String get thisWeek => 'هذا الأسبوع';

  @override
  String get totalEarnings => 'إجمالي الأرباح';

  @override
  String get weeklyTarget => 'هدف الأسبوع';

  @override
  String get transferEarnings => 'تحويل الأرباح';

  @override
  String get documents => 'المستندات';

  @override
  String get accountVerified => 'الحساب موثّق';

  @override
  String expiresOn(String date) {
    return 'تنتهي في $date';
  }

  @override
  String get noExpiry => 'بدون تاريخ انتهاء';

  @override
  String get docVerified => 'تم التحقق';

  @override
  String get docExpiringSoon => 'تحديث قريباً';

  @override
  String get docExpired => 'منتهي';

  @override
  String get documentsEmpty => 'لم يتم رفع أي مستند بعد';

  @override
  String get plateNumber => 'رقم اللوحة';

  @override
  String vehicleMeta(String color, String year) {
    return '$color · موديل $year';
  }

  @override
  String get updateVehicle => 'تحديث بيانات المركبة';

  @override
  String get noVehicle => 'لم تتم إضافة مركبة بعد';

  @override
  String get driverPersonalCopy => 'الاسم، الجوال والصورة الشخصية';

  @override
  String get driverBank => 'الحساب البنكي';

  @override
  String get driverBankCopy => 'إدارة الآيبان وتحويل الأرباح';

  @override
  String get driverTripSettings => 'إعدادات الرحلات';

  @override
  String get driverTripSettingsCopy => 'نطاق العمل وتفضيلات الطلبات';

  @override
  String get driverNotifCopy => 'تنبيهات الرحلات والأرباح';

  @override
  String get driverSupport => 'المساعدة والدعم';

  @override
  String get driverSupportCopy => 'تواصل مع فريق دعم السائقين';

  @override
  String get driverNotApproved => 'لم يتم اعتماد حسابك بعد';

  @override
  String get tripEyebrow => 'رحلتك الحالية';

  @override
  String tripNumberLabel(String number) {
    return 'رحلة $number';
  }

  @override
  String etaChip(String eta) {
    return 'يصل خلال $eta';
  }

  @override
  String get etaUnknown => 'جارٍ حساب وقت الوصول';

  @override
  String get driverAssignedTitle => 'تم تعيين كابتن لرحلتك';

  @override
  String get driverEnRouteTitle => 'الكابتن في طريقه إليك';

  @override
  String get driverArrivedTitle => 'الكابتن وصل';

  @override
  String get waitingCopy => 'الكابتن بانتظارك عند نقطة الالتقاط';

  @override
  String get waitingTimerLabel => 'مدة الانتظار';

  @override
  String get pinTitle => 'رمز بدء الرحلة';

  @override
  String get pinCopy => 'أخبر الكابتن بهذا الرمز عند صعودك';

  @override
  String get callDriver => 'اتصال';

  @override
  String get shareTrip => 'مشاركة';

  @override
  String shareTripText(
    String number,
    String driver,
    String vehicle,
    String plate,
  ) {
    return 'رحلتي مع ATA رقم $number. الكابتن: $driver، المركبة: $vehicle ($plate).';
  }

  @override
  String ratingValue(String rating) {
    return 'تقييم $rating';
  }

  @override
  String vehicleLine(String make, String model, String color) {
    return '$make $model · $color';
  }

  @override
  String get inTripTitle => 'رحلتك جارية';

  @override
  String get inTripCopy => 'استرخِ، سنعلمك عند الوصول إلى وجهتك.';

  @override
  String get readyToStartTitle => 'جاهز للانطلاق';

  @override
  String get readyToStartCopy => 'تم التحقق من الرمز، ستنطلق الرحلة الآن.';

  @override
  String get receiptTitle => 'وصلت بسلامة';

  @override
  String get receiptCopy => 'شكراً لاستخدامك ATA، إليك ملخص رحلتك.';

  @override
  String get receiptFare => 'الأجرة';

  @override
  String get receiptDistance => 'المسافة';

  @override
  String get receiptDuration => 'المدة';

  @override
  String get receiptPayment => 'طريقة الدفع';

  @override
  String kmValue(String km) {
    return '$km كم';
  }

  @override
  String metersValue(String meters) {
    return '$meters م';
  }

  @override
  String get rateTrip => 'قيّم الرحلة';

  @override
  String get done => 'تم';

  @override
  String get cancelledTitle => 'تم إلغاء الرحلة';

  @override
  String get cancelledCopy => 'يمكنك طلب رحلة جديدة في أي وقت.';

  @override
  String get noDriversTitle => 'لم نجد كابتن قريباً';

  @override
  String get noDriversCopy =>
      'كل الكباتن مشغولون حالياً، حاول مرة أخرى بعد قليل.';

  @override
  String get retryRequest => 'طلب رحلة جديدة';

  @override
  String get cancelTrip => 'إلغاء الرحلة';

  @override
  String get cancelReasonTitle => 'لماذا تريد الإلغاء؟';

  @override
  String get cancelReasonCopy => 'اختر السبب لإلغاء الرحلة مباشرة.';

  @override
  String get reasonChangedMind => 'غيّرت رأيي';

  @override
  String get reasonDriverLate => 'الكابتن تأخر';

  @override
  String get reasonWrongPickup => 'موقع الانطلاق غير صحيح';

  @override
  String get reasonOther => 'سبب آخر';

  @override
  String get keepTrip => 'الاستمرار في الرحلة';

  @override
  String get offeredPriceLabel => 'اقتراح سعر';

  @override
  String get offeredPriceHint => 'اختياري: اقترح السعر الذي يناسبك';

  @override
  String offeredPriceActive(String price) {
    return 'سعرك المقترح: $price';
  }

  @override
  String get offeredPriceClear => 'إزالة';

  @override
  String offeredPriceMin(String price) {
    return 'الحد الأدنى $price';
  }

  @override
  String offeredPriceMax(String price) {
    return 'الحد الأقصى $price';
  }

  @override
  String get offeredPriceDecrease => 'خفض السعر بريال';

  @override
  String get offeredPriceIncrease => 'رفع السعر بريال';

  @override
  String offerOutOfRange(String min, String max) {
    return 'السعر المقترح خارج النطاق المسموح ($min – $max ر.س)، تم تعديله';
  }

  @override
  String get quoteExpiredError =>
      'انتهت صلاحية السعر وتم تحديثه، اضغط للتأكيد مرة أخرى';

  @override
  String get quoteLoading => 'جارٍ حساب السعر…';

  @override
  String get quoteFailed => 'تعذر حساب السعر، الأسعار المعروضة تقديرية';

  @override
  String get quoteExpiredHint => 'انتهت صلاحية السعر';

  @override
  String get refreshQuote => 'تحديث السعر';

  @override
  String get fareDetails => 'تفاصيل السعر';

  @override
  String fareDetailsCopy(String category) {
    return 'كيف حُسب سعر $category';
  }

  @override
  String get fareBaseFare => 'التعرفة الأساسية';

  @override
  String fareDistance(String distance) {
    return 'المسافة ($distance)';
  }

  @override
  String fareTime(String duration) {
    return 'الوقت ($duration)';
  }

  @override
  String get fareMinApplied => 'تم تطبيق الحد الأدنى للتعرفة';

  @override
  String get fareTimeMultiplier => 'مضاعِف الوقت';

  @override
  String get fareDemandMultiplier => 'مضاعِف الطلب';

  @override
  String get fareBookingFee => 'رسوم الحجز';

  @override
  String get fareServiceFee => 'رسوم الخدمة';

  @override
  String get fareDiscount => 'الخصم';

  @override
  String get fareTotal => 'الإجمالي';

  @override
  String multiplierValue(String value) {
    return '×$value';
  }

  @override
  String demandBadge(String name, String multiplier) {
    return '$name ×$multiplier';
  }

  @override
  String get demandNormal => 'الطلب طبيعي';

  @override
  String get demandModerate => 'الطلب متوسط الآن';

  @override
  String get demandHigh => 'الطلب مرتفع الآن';

  @override
  String get demandVeryHigh => 'الطلب مرتفع جداً الآن';

  @override
  String get offerPassengerOffered => 'اقتراح من الراكب';

  @override
  String offerRound(int round) {
    return 'الجولة $round';
  }

  @override
  String get tripActiveExists => 'لديك رحلة نشطة بالفعل';

  @override
  String get offerExpiredError => 'انتهت صلاحية هذا العرض';

  @override
  String pinInvalid(int attempts) {
    return 'الرمز غير صحيح، المحاولات المتبقية: $attempts';
  }

  @override
  String get pinLocked => 'تم قفل التحقق من الرمز، تواصل مع الدعم';

  @override
  String get callFailed => 'تعذر بدء الاتصال';

  @override
  String get offerEyebrow => 'طلب جديد';

  @override
  String get offerTitle => 'رحلة جديدة بالقرب منك';

  @override
  String offerSecondsLeft(int seconds) {
    return '$seconds ث';
  }

  @override
  String get offerDistanceToPickup => 'المسافة إليك';

  @override
  String get offerEta => 'الوصول للراكب';

  @override
  String get offerTripDistance => 'مسافة الرحلة';

  @override
  String get offerPassengerPrice => 'سعر الراكب';

  @override
  String get offerNetEarnings => 'صافي أرباحك';

  @override
  String get offerPassenger => 'الراكب';

  @override
  String get acceptOffer => 'قبول الرحلة';

  @override
  String get rejectOffer => 'رفض';

  @override
  String get offerExpiredTitle => 'انتهى العرض';

  @override
  String get offerExpiredCopy => 'سيصلك الطلب التالي فور توفره.';

  @override
  String get pickupTitle => 'نقطة الالتقاط';

  @override
  String get dropoffTitle => 'الوجهة';

  @override
  String stopN(int n) {
    return 'محطة $n';
  }

  @override
  String get driverTripEyebrow => 'الرحلة الحالية';

  @override
  String get actionEnRoute => 'انطلقت إلى الراكب';

  @override
  String get actionArrived => 'وصلت';

  @override
  String get actionStart => 'ابدأ الرحلة';

  @override
  String get actionComplete => 'إنهاء الرحلة';

  @override
  String get enterPinTitle => 'أدخل رمز الراكب';

  @override
  String get enterPinCopy => 'اطلب من الراكب رمز بدء الرحلة المكوّن من 4 أرقام';

  @override
  String get verifyPinAction => 'تأكيد الرمز';

  @override
  String get stageDriverAssigned => 'تم قبول الرحلة';

  @override
  String get stageEnRoute => 'في الطريق إلى الراكب';

  @override
  String get stageArrived => 'وصلت إلى نقطة الالتقاط';

  @override
  String get stageWaiting => 'بانتظار الراكب';

  @override
  String get stagePinVerified => 'جاهز للانطلاق';

  @override
  String get stageInTrip => 'الرحلة جارية';

  @override
  String get stageCompleted => 'اكتملت الرحلة';

  @override
  String get callPassenger => 'اتصال بالراكب';

  @override
  String get backToDashboard => 'العودة إلى لوحة السائق';

  @override
  String get earningsLine => 'أرباحك من الرحلة';

  @override
  String get driverTripCompletedCopy =>
      'أحسنت! تمت إضافة أرباح الرحلة إلى محفظتك.';

  @override
  String get driverTripCancelledCopy =>
      'تم إلغاء هذه الرحلة، ستصلك طلبات جديدة قريباً.';

  @override
  String get locationDenied => 'يحتاج التطبيق إذن الموقع لاستقبال الرحلات';

  @override
  String get locationDeniedForever =>
      'فعّل إذن الموقع لتطبيق ATA من إعدادات الجهاز';

  @override
  String get locationServiceDisabled =>
      'فعّل خدمة الموقع (GPS) لاستقبال الرحلات';

  @override
  String get locationStreaming => 'موقعك يُشارك مع الركاب';
}
