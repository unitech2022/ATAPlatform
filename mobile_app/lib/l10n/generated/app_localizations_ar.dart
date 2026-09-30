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
  String get promoCopy => 'نسخ';

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

  @override
  String get noDriversNearby => 'لا يوجد كباتن قريبون الآن';

  @override
  String get cardBrandMada => 'مدى';

  @override
  String get cardBrandVisa => 'فيزا';

  @override
  String get cardBrandMastercard => 'ماستركارد';

  @override
  String cardMasked(String brand, String last4) {
    return '$brand •••• $last4';
  }

  @override
  String cardExpiry(String expiry) {
    return 'تنتهي $expiry';
  }

  @override
  String get cardExpired => 'البطاقة منتهية الصلاحية';

  @override
  String get cardPendingVerification => 'بانتظار التحقق من البنك';

  @override
  String get cardDefault => 'الافتراضية';

  @override
  String get cardMakeDefault => 'تعيين كافتراضية';

  @override
  String get cardRemove => 'حذف';

  @override
  String get cardRemoveTitle => 'حذف البطاقة؟';

  @override
  String cardRemoveCopy(String card) {
    return 'سيتم حذف $card من حسابك.';
  }

  @override
  String get savedCardsTitle => 'بطاقاتي';

  @override
  String get savedCardsCopy => 'بطاقات الدفع المحفوظة للرحلات وشحن المحفظة.';

  @override
  String get savedCardsEmpty => 'لا توجد بطاقات محفوظة بعد';

  @override
  String get manageCards => 'إدارة البطاقات';

  @override
  String get addCard => 'إضافة بطاقة';

  @override
  String get addCardTitle => 'إضافة بطاقة جديدة';

  @override
  String get addCardCopy =>
      'يتم ترميز بطاقتك على جهازك، ولا يصل رقمها إلى خوادمنا.';

  @override
  String get cardNumberLabel => 'رقم البطاقة';

  @override
  String get cardNumberHint => '0000 0000 0000 0000';

  @override
  String get cardNumberError => 'رقم البطاقة غير صحيح';

  @override
  String get cardExpiryLabel => 'تاريخ الانتهاء';

  @override
  String get cardExpiryHint => 'MM/YY';

  @override
  String get cardExpiryError => 'تاريخ غير صالح';

  @override
  String get cardCvcLabel => 'رمز الأمان';

  @override
  String get cardCvcHint => '123';

  @override
  String get cardCvcError => 'رمز غير صحيح';

  @override
  String get cardHolderLabel => 'اسم حامل البطاقة';

  @override
  String get cardHolderHint => 'كما يظهر على البطاقة';

  @override
  String get cardSetDefault => 'استخدامها كبطاقة افتراضية';

  @override
  String get saveCard => 'حفظ البطاقة';

  @override
  String get sandboxCardsHint =>
      'بيئة تجريبية: 4000 0000 0000 0002 مرفوضة، 4000 0000 0000 3220 تتطلب تحققاً، وبطاقة مدى التجريبية 4406 4700 0000 0007.';

  @override
  String get paymentActionTitle => 'مطلوب تحقق من البنك';

  @override
  String get paymentActionCopy =>
      'أكمل التحقق في صفحة البنك، وسيتم تحديث الحالة تلقائياً بعد التأكيد.';

  @override
  String get paymentActionOpen => 'فتح صفحة التحقق';

  @override
  String get topUpSource => 'طريقة الدفع';

  @override
  String get topUpToContinue => 'اشحن المحفظة للمتابعة';

  @override
  String get outstandingBalanceTitle => 'يوجد مبلغ مستحق على حسابك';

  @override
  String get outstandingBalanceCopy =>
      'رصيد محفظتك سالب. اشحن المحفظة لتتمكن من طلب رحلات جديدة.';

  @override
  String outstandingBalanceError(String amount) {
    return 'يوجد مبلغ مستحق $amount ر.س على حسابك، اشحن المحفظة للمتابعة';
  }

  @override
  String get paymentFailedError =>
      'تعذّر إتمام الدفع، جرّب بطاقة أخرى أو ادفع نقداً';

  @override
  String get paymentMethodExpiredError => 'البطاقة منتهية الصلاحية';

  @override
  String get paymentMethodInUseError => 'البطاقة مرتبطة برحلة جارية';

  @override
  String get paymentProviderUnavailableError =>
      'خدمة الدفع غير متاحة حالياً، حاول لاحقاً';

  @override
  String get paymentFallbackCash =>
      'تعذّر الدفع بالبطاقة، فتم تحويل الرحلة إلى الدفع نقداً';

  @override
  String collectCashLine(String amount) {
    return 'حصّل نقداً من الراكب: $amount';
  }

  @override
  String get viewReceipt => 'عرض الإيصال';

  @override
  String get receiptEyebrow => 'إيصال الرحلة';

  @override
  String get receiptBreakdown => 'تفاصيل الأجرة';

  @override
  String get receiptSubtotal => 'المجموع الفرعي';

  @override
  String get receiptDiscountTotal => 'إجمالي الخصم';

  @override
  String receiptVat(String rate, String amount) {
    return 'شامل ضريبة القيمة المضافة $rate٪: $amount';
  }

  @override
  String get receiptPaid => 'المبلغ المدفوع';

  @override
  String get receiptRefunded => 'المسترد';

  @override
  String get receiptNetPaid => 'صافي المدفوع';

  @override
  String get receiptUnavailable => 'لا يتوفر إيصال لهذه الرحلة';

  @override
  String get discountSourcePromotion => 'عرض ترويجي';

  @override
  String get discountSourceFavoriteDriver => 'الكابتن المفضل';

  @override
  String get cashDebtLimitError =>
      'تجاوزت مستحقات النقد الحد المسموح، سدّدها للاتصال';

  @override
  String get cantGoOnlineTitle => 'لا يمكنك الاتصال الآن';

  @override
  String get cashDebtTitle => 'مستحقات النقد';

  @override
  String get cashDebtCopy =>
      'أجور الرحلات النقدية المستحقة للمنصة. سدّدها قبل الوصول إلى الحد.';

  @override
  String cashDebtLimit(String limit) {
    return 'الحد المسموح: $limit';
  }

  @override
  String get settleDebt => 'سداد';

  @override
  String get settleDebtTitle => 'سداد مستحقات النقد';

  @override
  String get settleDebtCopy =>
      'اشحن محفظة الكابتن لتسديد مستحقات الرحلات النقدية.';

  @override
  String get driverWalletEyebrow => 'المحفظة والأرباح';

  @override
  String get earningsStatementTitle => 'كشف الأرباح';

  @override
  String get earningsStatementCopy =>
      'أرباحك وعمولة المنصة والنقد المحصّل للفترة.';

  @override
  String get periodToday => 'اليوم';

  @override
  String get periodWeek => 'الأسبوع';

  @override
  String get periodMonth => 'الشهر';

  @override
  String get statementNet => 'الصافي';

  @override
  String statementTrips(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count رحلة',
      few: '$count رحلات',
      two: 'رحلتان',
      one: 'رحلة واحدة',
      zero: 'لا رحلات',
    );
    return '$_temp0';
  }

  @override
  String get statementGross => 'إجمالي الأجور';

  @override
  String get statementCommission => 'عمولة المنصة';

  @override
  String get statementEarnings => 'أرباحك';

  @override
  String get statementIncentives => 'الحوافز';

  @override
  String get statementCompensation => 'تعويضات الإلغاء';

  @override
  String get statementAdjustments => 'التسويات';

  @override
  String get statementCashCollected => 'النقد المحصّل';

  @override
  String get statementPayouts => 'التحويلات';

  @override
  String get statementDaily => 'حسب اليوم';

  @override
  String get payoutsTitle => 'تحويل الأرباح';

  @override
  String get payoutsCopy => 'اطلب تحويل رصيدك إلى حسابك البنكي وتابع الحالة.';

  @override
  String get payoutsEmpty => 'لا توجد طلبات تحويل بعد';

  @override
  String get payoutHistory => 'سجل التحويلات';

  @override
  String get requestPayout => 'طلب تحويل';

  @override
  String get payoutRequestCopy =>
      'يُحوَّل المبلغ إلى الآيبان المسجل بعد اعتماد الطلب.';

  @override
  String get payoutAvailable => 'المتاح للتحويل';

  @override
  String payoutAvailableLine(String amount) {
    return 'المتاح للتحويل: $amount';
  }

  @override
  String get payoutIban => 'الآيبان';

  @override
  String get payoutIbanMissing => 'غير مضاف';

  @override
  String get payoutAmountLabel => 'المبلغ';

  @override
  String get payoutAmountInvalid => 'أدخل مبلغاً صحيحاً';

  @override
  String payoutMinimumHint(String amount) {
    return 'الحد الأدنى للتحويل $amount';
  }

  @override
  String confirmPayout(String amount) {
    return 'تأكيد تحويل $amount';
  }

  @override
  String get payoutRequestedTitle => 'تم إرسال طلب التحويل';

  @override
  String get payoutRequestedCopy => 'سنبلغك عند اعتماد الطلب وتحويل المبلغ.';

  @override
  String get payoutUnavailable => 'طلب التحويل غير متاح حالياً';

  @override
  String get payoutRequested => 'قيد المراجعة';

  @override
  String get payoutApproved => 'معتمد';

  @override
  String get payoutPaid => 'مدفوع';

  @override
  String get payoutRejected => 'مرفوض';

  @override
  String get payoutCancelled => 'ملغى';

  @override
  String payoutRejectedReason(String reason) {
    return 'السبب: $reason';
  }

  @override
  String payoutBelowMinimumError(String amount) {
    return 'المبلغ أقل من الحد الأدنى للسحب ($amount ر.س)';
  }

  @override
  String get payoutBelowMinimumReason => 'رصيدك أقل من الحد الأدنى للسحب';

  @override
  String get payoutCashDebtReason => 'سدّد مستحقات النقد أولاً';

  @override
  String get payoutPendingExistsError => 'لديك طلب سحب قيد المعالجة';

  @override
  String get ibanMissingError => 'أضف رقم الآيبان أولاً';

  @override
  String get insufficientBalanceError => 'المبلغ أكبر من الرصيد المتاح';

  @override
  String get shareNotFoundError => 'رابط التتبع غير موجود';

  @override
  String get shareExpiredError => 'انتهت صلاحية رابط التتبع';

  @override
  String get trustedContactsLimitError => 'الحد الأقصى 5 جهات موثوقة';

  @override
  String get trustedContactExistsError => 'الجهة مضافة مسبقاً';

  @override
  String get chatClosedError => 'المحادثة مغلقة لهذه الرحلة';

  @override
  String get lostItemWindowClosedError => 'انتهت مدة الإبلاغ عن المفقودات';

  @override
  String get cancellationReasonInvalidError => 'سبب الإلغاء غير صالح';

  @override
  String get cancellationFeeChangedError =>
      'تغيّرت رسوم الإلغاء، راجعها وأعد المحاولة';

  @override
  String noShowTooEarlyError(int minutes) {
    return 'لم تنتهِ مدة الانتظار المطلوبة بعد (متبقٍ $minutes د)';
  }

  @override
  String get accountRestrictedError => 'حسابك مقيّد مؤقتاً بسبب تكرار الإلغاء';

  @override
  String accountRestrictedUntil(String date) {
    return 'حسابك مقيّد مؤقتاً بسبب تكرار الإلغاء حتى $date';
  }

  @override
  String get cancelNoteHint => 'اكتب سبب الإلغاء';

  @override
  String get cancelNoteRequired => 'هذا السبب يتطلب ملاحظة';

  @override
  String get confirmCancel => 'تأكيد الإلغاء';

  @override
  String get reasonEmergencyBadge => 'طارئ';

  @override
  String get reasonExcusableBadge => 'يخضع للمراجعة';

  @override
  String get cancelEmergencyNote =>
      'سيُنشأ بلاغ سلامة ويتواصل معك فريق السلامة، ولن تُحتسب رسوم أو نقاط حتى المراجعة.';

  @override
  String get cancelExcusableNote =>
      'سيراجع فريق العمليات هذا العذر، ولن تُخصم رسوم أو نقاط حتى تتم المراجعة.';

  @override
  String get cancelFree => 'مجاني';

  @override
  String penaltyPointsValue(int count) {
    return '+$count نقاط';
  }

  @override
  String get scheduledCancelFeeLabel => 'رسوم إلغاء الحجز المجدول';

  @override
  String get cancelPointsLabel => 'أثر الإلغاء على موثوقيتك';

  @override
  String get cancelFeeLabel => 'رسوم الإلغاء';

  @override
  String cancelFreeUntil(String time) {
    return 'الإلغاء مجاني حتى $time';
  }

  @override
  String get cancelRequiresReview => 'الرسوم محتملة وتُحسم بعد مراجعة العذر';

  @override
  String noShowAvailableIn(String time) {
    return 'يمكنك تسجيل عدم حضور الراكب بعد $time';
  }

  @override
  String get noShowAvailableNow =>
      'انتهت مدة الانتظار، يمكنك تسجيل عدم حضور الراكب';

  @override
  String get noShowAction => 'لم يحضر الراكب';

  @override
  String get noShowConfirmTitle => 'تأكيد عدم حضور الراكب؟';

  @override
  String get noShowConfirmCopy =>
      'سيتم إلغاء الرحلة واحتساب رسوم عدم الحضور على الراكب مع تعويضك حسب السياسة.';

  @override
  String get keepWaiting => 'متابعة الانتظار';

  @override
  String get noShowRecorded => 'تم تسجيل عدم حضور الراكب وإلغاء الرحلة.';

  @override
  String compensationLine(String amount) {
    return 'تعويضك: $amount';
  }

  @override
  String get cancelFeePendingReview => 'رسوم الإلغاء قيد مراجعة فريق العمليات.';

  @override
  String cancelFeeCharged(String amount) {
    return 'تم خصم $amount رسوم إلغاء.';
  }

  @override
  String get levelNone => 'ممتاز';

  @override
  String get levelWarning => 'تنبيه';

  @override
  String get levelDeprioritized => 'أولوية أقل في المطابقة';

  @override
  String get levelIncentivesReduced => 'مكافآت مخفضة';

  @override
  String get levelRestricted => 'مقيّد مؤقتاً';

  @override
  String get levelSuspended => 'موقوف';

  @override
  String get levelNoneCopy => 'سجلك جيد، استمر في إكمال رحلاتك.';

  @override
  String get levelWarningCopy =>
      'تكرار الإلغاء يرفع نقاطك وقد يؤدي إلى تقييد حسابك.';

  @override
  String get levelDeprioritizedCopy =>
      'ستصلك عروض أقل مؤقتاً بسبب ارتفاع نسبة الإلغاء.';

  @override
  String get levelIncentivesReducedCopy =>
      'تصلك عروض أقل وتُخفَّض مكافآتك حتى تتحسن موثوقيتك.';

  @override
  String get levelRestrictedDriverCopy =>
      'لا يمكنك الاتصال واستقبال الطلبات حتى انتهاء التقييد.';

  @override
  String get levelRestrictedRiderCopy =>
      'لا يمكنك طلب رحلات جديدة حتى انتهاء التقييد.';

  @override
  String get levelSuspendedCopy => 'الحساب موقوف حتى يراجعه فريق العمليات.';

  @override
  String get excusePending => 'العذر قيد المراجعة';

  @override
  String get excuseApproved => 'تم قبول العذر';

  @override
  String get excuseRejected => 'تم رفض العذر';

  @override
  String get reliabilityTitle => 'موثوقيتك';

  @override
  String get reliabilityCopy => 'نسبة الإلغاء والنقاط وأثرها على حسابك.';

  @override
  String get cancellationRateLabel => 'نسبة الإلغاء';

  @override
  String get penaltyPointsLabel => 'النقاط';

  @override
  String get acceptanceRateLabel => 'نسبة القبول';

  @override
  String restrictedUntilLine(String date) {
    return 'مقيّد حتى $date';
  }

  @override
  String get reliabilityDetails => 'عرض التفاصيل';

  @override
  String get tripsAcceptedLabel => 'رحلات مُسندة';

  @override
  String get tripsCompletedLabel => 'رحلات مكتملة';

  @override
  String get cancellationsAtFaultLabel => 'إلغاءات محتسبة';

  @override
  String get reliabilityRateLabel => 'نسبة الإكمال';

  @override
  String get noShowCountLabel => 'عدم الحضور';

  @override
  String get matchingFactorLabel => 'معامل المطابقة';

  @override
  String get incentiveMultiplierLabel => 'معامل المكافآت';

  @override
  String reliabilityWindow(int days) {
    return 'آخر $days يوماً';
  }

  @override
  String nextLevelLine(String level, String points, String rate) {
    return 'المستوى التالي «$level» عند $points نقطة أو نسبة إلغاء $rate';
  }

  @override
  String get recentCancellations => 'آخر الإلغاءات';

  @override
  String get noRecentCancellations => 'لا توجد إلغاءات محتسبة.';

  @override
  String get caseTypeSos => 'نداء طوارئ';

  @override
  String get caseTypeReport => 'بلاغ سلامة';

  @override
  String get alertUnexpectedStop => 'توقف غير متوقع';

  @override
  String get alertRouteDeviation => 'انحراف عن المسار';

  @override
  String get alertTripOverrun => 'تأخر كبير في الرحلة';

  @override
  String get caseStatusOpen => 'مفتوحة';

  @override
  String get caseStatusInProgress => 'قيد المعالجة';

  @override
  String get caseStatusEscalated => 'مُصعَّدة';

  @override
  String get caseStatusResolved => 'مغلقة';

  @override
  String get alertUnexpectedStopCopy =>
      'لاحظنا أن الرحلة متوقفة منذ مدة في مكان غير متوقع.';

  @override
  String get alertRouteDeviationCopy =>
      'لاحظنا أن الرحلة ابتعدت عن المسار المتوقع.';

  @override
  String get alertTripOverrunCopy =>
      'الرحلة تستغرق وقتاً أطول بكثير من المتوقع.';

  @override
  String get safetyCheckCopy => 'نريد الاطمئنان عليك أثناء الرحلة.';

  @override
  String get reportUnsafeDriving => 'قيادة غير آمنة';

  @override
  String get reportHarassment => 'تحرش أو إساءة';

  @override
  String get reportVehicleMismatch => 'المركبة لا تطابق التطبيق';

  @override
  String get reportDriverMismatch => 'الكابتن لا يطابق التطبيق';

  @override
  String get reportPassengerMisconduct => 'سلوك غير لائق من الراكب';

  @override
  String get reportOther => 'أخرى';

  @override
  String get lostPhone => 'جوال';

  @override
  String get lostWallet => 'محفظة';

  @override
  String get lostBag => 'حقيبة';

  @override
  String get lostKeys => 'مفاتيح';

  @override
  String get lostDocuments => 'مستندات';

  @override
  String get lostOther => 'أخرى';

  @override
  String get lostStatusOpen => 'بانتظار رد الكابتن';

  @override
  String get lostStatusDriverContacted => 'تم التواصل مع الكابتن';

  @override
  String get lostStatusFound => 'تم العثور عليه';

  @override
  String get lostStatusReturned => 'تمت الإعادة';

  @override
  String get lostStatusNotFound => 'لم يُعثر عليه';

  @override
  String get lostStatusClosed => 'مغلق';

  @override
  String get sosSemantics => 'زر الطوارئ، اضغط مطولاً للتأكيد';

  @override
  String get sosLabel => 'SOS';

  @override
  String get sosHoldHint => 'اضغط مطولاً للطوارئ';

  @override
  String get sosKeepHolding => 'استمر بالضغط…';

  @override
  String get sosSending => 'جارٍ إرسال نداء الطوارئ…';

  @override
  String get sosActiveTitle => 'تم إرسال نداء الطوارئ لفريق السلامة';

  @override
  String get sosCancelledTitle => 'تم إلغاء نداء الطوارئ';

  @override
  String get sosFailedTitle => 'تعذّر إرسال نداء الطوارئ';

  @override
  String sosCaseLine(String number, String status) {
    return 'الحالة $number · $status';
  }

  @override
  String sosContactsNotified(int count) {
    return 'تم إبلاغ $count من جهاتك الموثوقة';
  }

  @override
  String get sosSharingLocation => 'نشارك موقعك مع فريق السلامة كل 10 ثوانٍ';

  @override
  String get sosCancelledCopy => 'سيتواصل معك فريق السلامة للتأكد من سلامتك.';

  @override
  String callEmergency(String number) {
    return 'اتصل بالطوارئ $number';
  }

  @override
  String get sosPressedByMistake => 'ضغطت بالخطأ';

  @override
  String get close => 'إغلاق';

  @override
  String get safetyCheckTitle => 'هل أنت بخير؟';

  @override
  String get safetyCheckHelpSent => 'تم إبلاغ فريق السلامة وسيتواصل معك فوراً.';

  @override
  String get safetyCheckOkThanks => 'شكراً لك، سعداء أنك بخير.';

  @override
  String get safetyCheckExpired => 'لم نتلقَّ ردك، سيتواصل معك فريق السلامة.';

  @override
  String safetyCheckCountdown(int seconds) {
    return 'يرجى الرد خلال $seconds ث';
  }

  @override
  String get safetyCheckOk => 'أنا بخير';

  @override
  String get safetyCheckHelp => 'أحتاج مساعدة';

  @override
  String get manageSharing => 'إدارة';

  @override
  String shareTripLinkText(String url) {
    return 'تابع رحلتي مع ATA مباشرة: $url';
  }

  @override
  String get shareSheetCopy => 'أرسل رابط تتبع مباشر، ويمكنك إيقافه في أي وقت.';

  @override
  String get shareToContacts => 'إرسال لجهاتك الموثوقة';

  @override
  String get addTrustedContact => 'إضافة جهة موثوقة';

  @override
  String get sendBySms => 'إرسال عبر رسالة نصية';

  @override
  String smsSentTo(int count) {
    return 'تم الإرسال إلى $count';
  }

  @override
  String get activeLinks => 'الروابط النشطة';

  @override
  String get noActiveLinks => 'لا توجد روابط نشطة.';

  @override
  String shareViews(int count) {
    return '$count مشاهدة';
  }

  @override
  String get revokeLink => 'إيقاف';

  @override
  String get chatAction => 'محادثة';

  @override
  String maskedCallPin(String pin) {
    return 'رمز الاتصال: $pin';
  }

  @override
  String get callUnavailable => 'الاتصال غير متاح حالياً، تواصل عبر المحادثة.';

  @override
  String get chatWithDriver => 'المحادثة مع الكابتن';

  @override
  String get chatWithPassenger => 'المحادثة مع الراكب';

  @override
  String get chatMaskedNote =>
      'لا تتم مشاركة أرقام الهواتف، وتُخفى الأرقام داخل الرسائل.';

  @override
  String get chatEmpty => 'لا توجد رسائل بعد.';

  @override
  String get chatInputHint => 'اكتب رسالة…';

  @override
  String get chatSend => 'إرسال';

  @override
  String get chatClosedBanner => 'انتهت الرحلة، المحادثة للقراءة فقط.';

  @override
  String get messageSending => 'جارٍ الإرسال…';

  @override
  String get messageFailed => 'تعذّر الإرسال، اضغط لإعادة المحاولة';

  @override
  String get messageRead => 'مقروءة';

  @override
  String get sosHoldCopy =>
      'اضغط مطولاً على SOS لإبلاغ فريق السلامة بموقعك فوراً';

  @override
  String get shareTripOnTripOnly =>
      'متاحة أثناء الرحلة من بطاقة الكابتن، ويمكن إرسالها تلقائياً لجهاتك الموثوقة.';

  @override
  String get myReportsTitle => 'بلاغاتي';

  @override
  String get myReportsCopy => 'تابع حالة بلاغات السلامة ونداءات الطوارئ.';

  @override
  String get lostItemsTitle => 'المفقودات';

  @override
  String get lostItemsCopy => 'تابع بلاغات الأغراض المفقودة في رحلاتك.';

  @override
  String get trustedContactsPageCopy =>
      'يمكنك إضافة حتى 5 جهات تصلها رسالة عند الطوارئ، وتلقائياً رابط تتبع رحلاتك إن فعّلت المشاركة التلقائية.';

  @override
  String get noTrustedContacts => 'لم تضف أي جهة موثوقة بعد.';

  @override
  String trustedContactsCount(int count, int max) {
    return '$count من $max';
  }

  @override
  String get edit => 'تعديل';

  @override
  String get delete => 'حذف';

  @override
  String get autoShareLabel => 'مشاركة رحلاتي تلقائياً';

  @override
  String get notifyOnSosLabel => 'إبلاغها عند الطوارئ';

  @override
  String get contactPhoneSelf => 'لا يمكنك إضافة رقمك';

  @override
  String get editTrustedContact => 'تعديل الجهة الموثوقة';

  @override
  String get contactNameLabel => 'الاسم';

  @override
  String get contactNameRequired => 'الاسم مطلوب';

  @override
  String get contactPhoneLabel => 'رقم الجوال';

  @override
  String get contactPhoneHint => '05XXXXXXXX';

  @override
  String get contactRelationshipLabel => 'صلة القرابة (اختياري)';

  @override
  String get save => 'حفظ';

  @override
  String get noReports => 'لا توجد بلاغات.';

  @override
  String get caseNoUpdates => 'لا توجد تحديثات بعد، سيتواصل معك فريق السلامة.';

  @override
  String get safetyReportTitle => 'الإبلاغ عن مشكلة سلامة';

  @override
  String get safetyReportCopy =>
      'أخبرنا بما حدث خلال رحلتك (خلال 7 أيام)، وسيراجعه فريق السلامة.';

  @override
  String get reportSubmitted => 'تم استلام بلاغك';

  @override
  String reportNumberLine(String number) {
    return 'رقم البلاغ: $number';
  }

  @override
  String get chooseCategory => 'اختر نوع المشكلة';

  @override
  String get describeWhatHappened => 'صف ما حدث…';

  @override
  String get descriptionRequired => 'الوصف مطلوب';

  @override
  String get submitReport => 'إرسال البلاغ';

  @override
  String get noPendingSafetyCheck => 'لا يوجد سؤال سلامة معلّق.';

  @override
  String get lostItemTitle => 'الإبلاغ عن غرض مفقود';

  @override
  String get lostItemCopy =>
      'صف الغرض وسنبلغ الكابتن ونتابع معك عبر الدعم (خلال 7 أيام من الرحلة).';

  @override
  String get lostItemSubmitted => 'تم استلام بلاغ المفقودات';

  @override
  String get lostItemDescribe => 'صف الغرض (اللون، العلامة، مكانه في السيارة…)';

  @override
  String get lostItemContactPhone => 'رقم للتواصل (اختياري)';

  @override
  String get lostItemsPageCopy => 'حالة بلاغات الأغراض المفقودة.';

  @override
  String get noLostItems => 'لا توجد بلاغات مفقودات.';

  @override
  String get driverLostItemsTitle => 'المفقودات';

  @override
  String get driverLostItemsCopy => 'أغراض أبلغ عنها الركاب في رحلاتك.';

  @override
  String get lostItemFound => 'وجدته';

  @override
  String get lostItemNotFound => 'لم أجده';

  @override
  String get tripHelpTitle => 'هل تحتاج مساعدة بخصوص هذه الرحلة؟';

  @override
  String get lostItemRowCopy => 'نسيت شيئاً في السيارة؟';

  @override
  String get safetyReportRowCopy => 'أبلغ عن قيادة غير آمنة أو سلوك غير لائق';

  @override
  String get later => 'لاحقاً';

  @override
  String get rateShort => 'قيّم';

  @override
  String get rateNow => 'قيّم الآن';

  @override
  String get ratePassenger => 'قيّم الراكب';

  @override
  String get tripRated => 'تم تقييم الرحلة';

  @override
  String get rateTripCopy =>
      'تقييمك يساعدنا على تحسين الخدمة ويبقى مجهول الهوية.';

  @override
  String get rateDriverQuestion => 'كيف كانت رحلتك مع الكابتن؟';

  @override
  String rateDriverQuestionNamed(String name) {
    return 'كيف كانت رحلتك مع $name؟';
  }

  @override
  String get ratePassengerQuestion => 'كيف كان الراكب؟';

  @override
  String ratePassengerQuestionNamed(String name) {
    return 'كيف كان الراكب $name؟';
  }

  @override
  String get ratingStarsNone => 'اختر عدد النجوم';

  @override
  String get ratingStars1 => 'سيئة';

  @override
  String get ratingStars2 => 'دون المتوقع';

  @override
  String get ratingStars3 => 'مقبولة';

  @override
  String get ratingStars4 => 'جيدة';

  @override
  String get ratingStars5 => 'ممتازة';

  @override
  String get ratingTagsPositive => 'ما الذي أعجبك؟';

  @override
  String get ratingTagsNegative => 'ما الذي لم يعجبك؟';

  @override
  String get ratingTagDriving => 'القيادة';

  @override
  String get ratingTagCleanliness => 'النظافة';

  @override
  String get ratingTagBehaviour => 'التعامل';

  @override
  String get ratingTagNavigation => 'معرفة الطريق';

  @override
  String get ratingTagVehicleCondition => 'حالة المركبة';

  @override
  String get ratingTagPunctuality => 'الالتزام بالوقت';

  @override
  String get ratingCommentHint => 'أضف تعليقاً (اختياري)';

  @override
  String get ratingSubmit => 'إرسال التقييم';

  @override
  String get ratingThanks => 'شكراً لتقييمك';

  @override
  String get ratingThanksCopy => 'ملاحظاتك تساعدنا على تقديم رحلات أفضل.';

  @override
  String get ratingWindowClosedError => 'انتهت مدة التقييم';

  @override
  String get ratingExistsError => 'تم تقييم هذه الرحلة مسبقاً';

  @override
  String get pendingRatingTitle => 'قيّم رحلتك الأخيرة';

  @override
  String get pendingRatingCopy => 'أخبرنا كيف كانت الرحلة.';

  @override
  String pendingRatingCopyNamed(String name) {
    return 'كيف كانت رحلتك مع $name؟';
  }

  @override
  String get driverRatingsTitle => 'تقييماتي';

  @override
  String get driverRatingsCopy =>
      'متوسط تقييمك وأكثر ما يذكره الركاب، دون أسماء.';

  @override
  String ratingCountLine(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count تقييم',
      few: '$count تقييمات',
      two: 'تقييمان',
      one: 'تقييم واحد',
      zero: 'لا تقييمات بعد',
    );
    return '$_temp0';
  }

  @override
  String get ratingTopTags => 'الأكثر ذكراً';

  @override
  String get ratingRecentComments => 'آخر التعليقات';

  @override
  String get ratingNoComments => 'لا توجد تعليقات بعد.';

  @override
  String get fareTotalBeforeDiscount => 'قبل الخصم';

  @override
  String receiptPromo(String code) {
    return 'كود الخصم $code';
  }

  @override
  String get promoReserved => 'محجوز';

  @override
  String get promoCodeLabel => 'كود خصم';

  @override
  String get promoAddHint => 'أضف كود الخصم إن كان لديك';

  @override
  String get promoNotWithOffer => 'لا ينطبق الخصم مع اقتراح السعر';

  @override
  String promoAppliedLine(String code) {
    return '$code مطبّق';
  }

  @override
  String promoAppliedDiscount(String code, String amount) {
    return '$code مطبّق · وفّرت $amount';
  }

  @override
  String get promoRemove => 'إزالة';

  @override
  String get promoCodeTitle => 'كود الخصم';

  @override
  String get promoCodeCopy => 'أدخل الكود وسنتحقق منه على سعر رحلتك الحالية.';

  @override
  String get promoCodeHint => 'مثال: ATA10';

  @override
  String get promoApply => 'تطبيق';

  @override
  String get promoNotFoundError => 'كود الخصم غير صحيح';

  @override
  String get promoExpiredError => 'انتهت صلاحية كود الخصم';

  @override
  String get promoNotEligibleError => 'كود الخصم لا ينطبق على هذه الرحلة';

  @override
  String get promoUsageLimitError => 'تم استنفاد كود الخصم';

  @override
  String get promoUserLimitError => 'استخدمت هذا الكود الحد الأقصى من المرات';

  @override
  String get promoReasonFirstTrip => 'هذا الكود لرحلتك الأولى فقط';

  @override
  String get promoReasonNewUsers => 'هذا الكود للمستخدمين الجدد فقط';

  @override
  String get promoReasonCity => 'هذا الكود غير متاح في مدينتك';

  @override
  String get promoReasonCategory =>
      'هذا الكود لا ينطبق على فئة الرحلة المختارة';

  @override
  String get promoReasonZone => 'هذا الكود لا ينطبق على منطقة الالتقاط';

  @override
  String get promoReasonPaymentMethod =>
      'هذا الكود لا ينطبق على طريقة الدفع المختارة';

  @override
  String get promoReasonBookingType => 'هذا الكود لا ينطبق على نوع الحجز';

  @override
  String get promoReasonMinFare => 'أجرة الرحلة أقل من الحد الأدنى لهذا الكود';

  @override
  String get promoReasonPricingMode => 'لا ينطبق الخصم مع اقتراح السعر';

  @override
  String get promotionsTitle => 'العروض';

  @override
  String get promotionsCopy =>
      'أكواد الخصم المتاحة لك وما استخدمته أو انتهت صلاحيته.';

  @override
  String get promotionsLinkCopy => 'أكواد الخصم المتاحة لك';

  @override
  String get promotionsEmpty => 'لا توجد عروض هنا حالياً.';

  @override
  String get promoTabAvailable => 'المتاحة';

  @override
  String get promoTabUsed => 'المستخدمة';

  @override
  String get promoTabExpired => 'المنتهية';

  @override
  String promoPercentOff(String percent) {
    return 'خصم $percent%';
  }

  @override
  String promoUpTo(String discount, String cap) {
    return '$discount حتى $cap';
  }

  @override
  String promoAmountOff(String amount) {
    return 'خصم $amount';
  }

  @override
  String get promoFreeBookingFee => 'رسوم حجز مجانية';

  @override
  String promoMinFare(String amount) {
    return 'الحد الأدنى للأجرة $amount';
  }

  @override
  String get promoFirstTripOnly => 'للرحلة الأولى فقط';

  @override
  String promoValidTo(String date) {
    return 'صالح حتى $date';
  }

  @override
  String get promoUse => 'استخدم';

  @override
  String promoCopied(String code) {
    return 'تم نسخ الكود $code';
  }

  @override
  String get tierTitle => 'مستوى الكابتن';

  @override
  String get tierCopy => 'يُحسب أسبوعياً من أدائك في آخر 28 يوماً.';

  @override
  String get tierBronze => 'برونزي';

  @override
  String get tierSilver => 'فضي';

  @override
  String get tierGold => 'ذهبي';

  @override
  String get tierPlatinum => 'بلاتيني';

  @override
  String get tierTopReached => 'وصلت إلى أعلى مستوى، حافظ على أدائك.';

  @override
  String tierTripsToNext(int count, String tier) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'تبقّت $count رحلة للوصول إلى $tier',
      few: 'تبقّت $count رحلات للوصول إلى $tier',
      two: 'تبقّت رحلتان للوصول إلى $tier',
      one: 'تبقّت رحلة واحدة للوصول إلى $tier',
    );
    return '$_temp0';
  }

  @override
  String tierProgressTo(String tier) {
    return 'تقدّمك نحو $tier';
  }

  @override
  String tierChecksMet(int met, int total) {
    return '$met من $total شروط محققة';
  }

  @override
  String tierCommissionDiscount(String percent) {
    return 'خصم العمولة $percent%';
  }

  @override
  String tierRequirementsTitle(String tier, int days) {
    return 'شروط $tier (آخر $days يوماً)';
  }

  @override
  String get tierCriterionTrips => 'الرحلات المكتملة';

  @override
  String get tierCriterionRating => 'متوسط التقييم';

  @override
  String get tierCriterionAcceptance => 'نسبة القبول';

  @override
  String get tierCriterionCancellation => 'نسبة الإلغاء';

  @override
  String tierRecalcAt(String date) {
    return 'إعادة الحساب القادمة: $date';
  }

  @override
  String get tierRecalcWeekly => 'يُعاد حساب المستوى كل أحد.';

  @override
  String get incentivesTitle => 'الحوافز';

  @override
  String get incentivesCopy =>
      'أكمل الرحلات المطلوبة خلال الفترة واحصل على المكافأة.';

  @override
  String get incentivesLinkCopy => 'التحديات النشطة والقادمة ومكافآتها';

  @override
  String get incentivesEmpty => 'لا توجد حوافز هنا حالياً.';

  @override
  String get incentiveNearest => 'أقرب حافز';

  @override
  String get incentiveTabActive => 'النشطة';

  @override
  String get incentiveTabUpcoming => 'القادمة';

  @override
  String get incentiveTabCompleted => 'المنتهية';

  @override
  String get incentiveTypeDaily => 'يومي';

  @override
  String get incentiveTypeWeekly => 'أسبوعي';

  @override
  String get incentiveTypeZone => 'منطقة';

  @override
  String get incentiveTypeOneTime => 'مرة واحدة';

  @override
  String get incentiveInProgress => 'جارٍ';

  @override
  String get incentiveAchieved => 'تحقق · يُصرف بعد نهاية الفترة';

  @override
  String get incentivePaid => 'تم الصرف';

  @override
  String get incentiveExpired => 'انتهى';

  @override
  String get incentiveVoided => 'أُلغي';

  @override
  String incentiveTripsProgress(int done, int target) {
    return '$done من $target رحلات';
  }

  @override
  String incentiveTripsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count رحلة',
      few: '$count رحلات',
      two: 'رحلتان',
      one: 'رحلة واحدة',
    );
    return '$_temp0';
  }

  @override
  String get incentiveJoinFirst => 'اشترك لتبدأ الاحتساب';

  @override
  String get incentiveJoined => 'أنت مشترك في هذا الحافز';

  @override
  String get incentiveOptIn => 'اشترك في الحافز';

  @override
  String get incentiveOptInClosedError => 'الاشتراك في هذا الحافز غير متاح';

  @override
  String incentiveEndsAt(String date) {
    return 'ينتهي $date';
  }

  @override
  String incentiveReducedNotice(String multiplier) {
    return 'مكافآتك مخفّضة (×$multiplier) بسبب مستوى الموثوقية، حسّن التزامك لاستعادتها كاملة.';
  }

  @override
  String get incentiveDetailTitle => 'تفاصيل الحافز';

  @override
  String get incentiveTarget => 'الهدف';

  @override
  String get incentiveWindow => 'الأيام والساعات';

  @override
  String get incentiveZones => 'المناطق';

  @override
  String get incentiveAllZones => 'كل المناطق';

  @override
  String get incentiveCategories => 'الفئات';

  @override
  String get incentiveAllCategories => 'كل الفئات';

  @override
  String get incentivePeriod => 'الفترة';

  @override
  String get daySun => 'الأحد';

  @override
  String get dayMon => 'الاثنين';

  @override
  String get dayTue => 'الثلاثاء';

  @override
  String get dayWed => 'الأربعاء';

  @override
  String get dayThu => 'الخميس';

  @override
  String get dayFri => 'الجمعة';

  @override
  String get daySat => 'السبت';

  @override
  String get favoriteDriversTitle => 'السائقون المفضلون';

  @override
  String get favoriteDriversCopy =>
      'الكباتن الذين أضفتهم بعد رحلات سابقة. اطلبهم مباشرة واحصل على خصم.';

  @override
  String get favoriteEmptyTitle => 'لا يوجد كباتن مفضلون بعد';

  @override
  String get favoriteEmptyCopy =>
      'بعد إكمال رحلة يمكنك إضافة الكابتن إلى مفضلتك من شاشة التقييم أو من الإيصال.';

  @override
  String favoriteTripsTogether(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count رحلة معاً',
      few: '$count رحلات معاً',
      two: 'رحلتان معاً',
      one: 'رحلة واحدة معاً',
    );
    return '$_temp0';
  }

  @override
  String favoriteLastTrip(String date) {
    return 'آخر رحلة $date';
  }

  @override
  String get favoriteAvailableNow => 'متاح الآن';

  @override
  String favoriteAvailableEta(String eta) {
    return 'متاح الآن · يصل خلال $eta';
  }

  @override
  String get favoriteRemove => 'إزالة من المفضلة';

  @override
  String favoriteRemoveTitle(String name) {
    return 'إزالة $name من المفضلة؟';
  }

  @override
  String get favoriteRemoveCopy =>
      'لن تتمكن من طلبه مباشرة ولن ينطبق خصم المفضل معه. لا يتأثر أي رحلة جارية.';

  @override
  String get favoriteRemoveConfirm => 'إزالة';

  @override
  String favoriteRemovedSnack(String name) {
    return 'تمت إزالة $name من المفضلة';
  }

  @override
  String get favoriteAdd => 'أضف إلى المفضلة';

  @override
  String get favoriteAdding => 'جارٍ الإضافة…';

  @override
  String get favoriteAdded => 'تمت الإضافة إلى المفضلة';

  @override
  String get favoriteAlready => 'الكابتن في مفضلتك';

  @override
  String favoriteAddOptionCopy(String name) {
    return 'اطلب $name مباشرة في رحلاتك القادمة';
  }

  @override
  String get favoriteAddOptionCopyAnon =>
      'اطلب هذا الكابتن مباشرة في رحلاتك القادمة';

  @override
  String get favoriteRatingAdded => 'تمت إضافة الكابتن إلى مفضلتك';

  @override
  String favoriteRatingFailed(String reason) {
    return 'تم إرسال التقييم، لكن تعذّرت إضافة الكابتن إلى المفضلة: $reason';
  }

  @override
  String get favoriteNotEligibleError =>
      'يمكنك إضافة الكابتن بعد إكمال رحلة معه';

  @override
  String get favoriteExistsError => 'الكابتن موجود في مفضلتك بالفعل';

  @override
  String get favoritesLimitError => 'وصلت إلى الحد الأقصى للكباتن المفضلين';

  @override
  String get favoriteNotFavoriteError => 'هذا الكابتن لم يعد في مفضلتك';

  @override
  String get favoriteRowTitle => 'السائقون المفضلون';

  @override
  String get favoriteRowManage => 'إدارة';

  @override
  String get favoriteRowHint => 'اختر كابتنك المفضل ليصله طلبك أولاً';

  @override
  String get favoriteRowNone => 'لا أحد من كباتنك المفضلين متاح الآن';

  @override
  String get favoriteRowLoading => 'جارٍ البحث عن كباتنك المفضلين…';

  @override
  String get favoriteRowNotWithOffer => 'غير متاح مع اقتراح السعر';

  @override
  String get favoriteChipUnavailable => 'غير متاح حالياً';

  @override
  String favoriteDiscountBadge(String percent) {
    return 'خصم $percent%';
  }

  @override
  String favoriteSelectedLine(String name) {
    return 'سيصل طلبك أولاً إلى $name';
  }

  @override
  String get favoriteFallbackNote => 'إن لم يكن متاحاً سنبحث عن أقرب كابتن';

  @override
  String favoriteDiscountSaved(String amount) {
    return 'خصم الكابتن المفضل · وفّرت $amount';
  }

  @override
  String favoriteDiscountConditional(String name) {
    return 'يُطبّق الخصم عند قبول $name للرحلة';
  }

  @override
  String get favoritePromoNotStacked =>
      'لم يُطبَّق كود الخصم: لا يُجمع مع خصم الكابتن المفضل الأكبر';

  @override
  String get favoriteLostToPromo =>
      'لم يُطبَّق خصم المفضل: كود الخصم أكبر ولا يُجمع معه';

  @override
  String get favoriteDeselect => 'إلغاء اختيار الكابتن المفضل';

  @override
  String get favoriteSearchingTitle => 'نتواصل مع كابتنك المفضل...';

  @override
  String favoriteSearchingCopy(String name) {
    return 'طلبك موجّه أولاً إلى $name. إن لم يرد سنبحث لك عن أقرب كابتن.';
  }

  @override
  String favoriteFallbackNotice(String name) {
    return 'لم يتمكن $name من الرد، نبحث لك عن كابتن آخر';
  }

  @override
  String favoriteUnavailableNotice(String name) {
    return '$name غير متاح حالياً، نبحث لك عن أقرب كابتن';
  }

  @override
  String get receiptFavoriteDiscount => 'خصم الكابتن المفضل';

  @override
  String get favoriteDiscountApplied => 'مطبّق';

  @override
  String get favoriteDriverBadge => 'مفضل';

  @override
  String get offerFavoriteRequest => 'من راكب يفضّلك';

  @override
  String get offerFavoriteExclusive => 'عرض حصري لك';

  @override
  String get airportCategoryNotApplicableError =>
      'فئة المطار متاحة لرحلات المطار فقط';

  @override
  String get airportChoose => 'اختر المطار واتجاه رحلتك';

  @override
  String get airportChooseTerminal => 'الصالة (اختياري)';

  @override
  String get airportChooseZone => 'اختر منطقة الالتقاط';

  @override
  String get airportDirectionDropoff => 'إلى المطار';

  @override
  String get airportDirectionPickup => 'من المطار';

  @override
  String get airportDone => 'تم';

  @override
  String get airportFlightHint => 'مثال: SV1020';

  @override
  String get airportFlightInvalid => 'رقم الرحلة غير صحيح، مثال: SV1020';

  @override
  String get airportFlightLabel => 'رقم الرحلة الجوية (اختياري)';

  @override
  String airportFlightLine(String flight) {
    return 'رحلة $flight';
  }

  @override
  String get airportNoAirports => 'لا توجد مطارات متاحة حالياً';

  @override
  String get airportNoDetails => 'لم تحدد الصالة بعد';

  @override
  String get airportPickupZoneRequiredError => 'اختر منطقة الالتقاط في المطار';

  @override
  String get airportQueueCopy =>
      'تُوزَّع رحلات المطار على الكباتن بترتيب الوصول: الأول فالأول.';

  @override
  String airportQueueEligible(String airport) {
    return 'أنت داخل منطقة انتظار $airport';
  }

  @override
  String get airportQueueExitNote =>
      'إذا غادرت منطقة الانتظار أو انقطع اتصالك لفترة تخرج من الطابور تلقائياً.';

  @override
  String get airportQueueEyebrow => 'المطار';

  @override
  String get airportQueueJoin => 'انضم للطابور';

  @override
  String get airportQueueJoinHint => 'انضم لاستلام رحلات المطار بالترتيب';

  @override
  String get airportQueueLeave => 'غادر الطابور';

  @override
  String get airportQueueNotNearby => 'أنت لست في منطقة انتظار مطار حالياً';

  @override
  String airportQueuePosition(int position, int total) {
    return 'ترتيبك $position من $total';
  }

  @override
  String get airportQueueTitle => 'طابور المطار';

  @override
  String airportQueueWait(int minutes) {
    return 'الانتظار المتوقع نحو $minutes دقيقة';
  }

  @override
  String get airportRemove => 'إزالة';

  @override
  String get airportRowCopy => 'اختر المطار والصالة ورقم الرحلة';

  @override
  String get airportRowTitle => 'رحلة من أو إلى مطار؟';

  @override
  String get airportSheetTitle => 'المطار';

  @override
  String airportTerminalLabel(String code) {
    return 'صالة $code';
  }

  @override
  String airportWaitingFree(int minutes) {
    return 'انتظار مجاني $minutes د';
  }

  @override
  String airportWaitingPolicyCopy(int minutes) {
    return 'تحصل على $minutes دقيقة انتظار مجاناً عند الالتقاط، وبعدها تُحتسب رسوم الانتظار.';
  }

  @override
  String get airportWaitingPolicyTitle => 'سياسة الانتظار';

  @override
  String countdownDaysHours(int days, int hours) {
    return '$days يوم و$hours ساعة';
  }

  @override
  String countdownHoursMinutes(int hours, int minutes) {
    return '$hours ساعة و$minutes دقيقة';
  }

  @override
  String get driverScheduledCopy =>
      'طلبات مجدولة قادمة في منطقتك. احجز ما يناسبك وأكّد قبل الموعد.';

  @override
  String get driverScheduledEyebrow => 'الحجوزات';

  @override
  String get driverScheduledLinkCopy => 'السوق وحجوزاتك القادمة';

  @override
  String driverScheduledNext(String time) {
    return 'أقرب حجز: $time';
  }

  @override
  String get driverScheduledTabMarket => 'السوق';

  @override
  String get driverScheduledTabMine => 'حجوزاتي';

  @override
  String get driverScheduledTitle => 'رحلاتي المجدولة';

  @override
  String get marketAirportBadge => 'مطار';

  @override
  String get marketApproxPickup => 'موقع الالتقاط تقريبي حتى الحجز';

  @override
  String get marketDayAll => 'الكل';

  @override
  String marketDistanceToPickup(String km) {
    return '$km كم عنك';
  }

  @override
  String get marketEmptyCopy => 'نحدّث القائمة تلقائياً كل دقيقة';

  @override
  String get marketEmptyTitle => 'لا توجد طلبات مجدولة الآن';

  @override
  String marketExclusiveUntil(String time) {
    return 'حصرية لك حتى $time';
  }

  @override
  String marketFare(String price) {
    return 'الأجرة $price';
  }

  @override
  String get marketLoadMore => 'عرض المزيد';

  @override
  String get marketNetEarnings => 'صافي أرباحك';

  @override
  String get marketReserve => 'احجز الرحلة';

  @override
  String get marketReserved => 'تم حجز الرحلة، ستجدها في حجوزاتي';

  @override
  String get notInAirportWaitingAreaError =>
      'يجب أن تكون داخل منطقة انتظار المطار';

  @override
  String get reservationConfirmAction => 'تأكيد';

  @override
  String reservationConfirmCopy(String time) {
    return 'رحلة مجدولة $time. أكّد قبل انتهاء المهلة وإلا سيُحرَّر الحجز.';
  }

  @override
  String get reservationConfirmFinalTitle => 'التأكيد النهائي: استعد للانطلاق';

  @override
  String get reservationConfirmFirstTitle => 'أكّد حجزك';

  @override
  String reservationConfirmLeft(String time) {
    return 'المتبقي $time';
  }

  @override
  String get reservationConfirmed => 'تم تأكيد الحجز';

  @override
  String get reservationConflictError => 'يتعارض الموعد مع رحلة محجوزة لديك';

  @override
  String get reservationDetailEyebrow => 'حجز مجدول';

  @override
  String get reservationFare => 'الأجرة التقديرية';

  @override
  String get reservationFinalConfirmed =>
      'تم التأكيد النهائي، ستبدأ الرحلة في موعدها';

  @override
  String get reservationFreeReleaseLabel => 'تحرير مجاني حتى';

  @override
  String get reservationLimitReachedError => 'وصلت للحد الأقصى من الحجوزات';

  @override
  String get reservationNotConfirmableError => 'لا يمكن التأكيد الآن';

  @override
  String get reservationNotConfirmableOfflineError =>
      'فعّل حالة «متصل» لتأكيد الحجز';

  @override
  String get reservationNotConfirmableOnTripError =>
      'لديك رحلة جارية، أنهِها أولاً لتأكيد الحجز';

  @override
  String get reservationNotFound => 'لم يتم العثور على هذا الحجز';

  @override
  String get reservationPassenger => 'الراكب';

  @override
  String get reservationReleaseAction => 'تحرير الحجز';

  @override
  String get reservationReleaseConfirm => 'نعم، حرّر';

  @override
  String reservationReleaseFreeCopy(String time) {
    return 'التحرير مجاني حتى $time.';
  }

  @override
  String get reservationReleaseLateCopy =>
      'انتهت مدة التحرير المجاني؛ سيتم خصم نقاط من موثوقيتك، وقد يؤثر ذلك على أولوية الطلبات والحوافز.';

  @override
  String get reservationReleaseTitle => 'تحرير الحجز؟';

  @override
  String get reservationReleased => 'تم تحرير الحجز';

  @override
  String reservationReleasedPoints(int points) {
    return 'تم تحرير الحجز وخُصمت $points نقاط من موثوقيتك';
  }

  @override
  String get reservationStatusAssigned => 'مسندة إليك';

  @override
  String get reservationStatusCancelled => 'ملغاة';

  @override
  String get reservationStatusCompleted => 'مكتملة';

  @override
  String get reservationStatusConfirmed => 'مؤكدة';

  @override
  String get reservationStatusNoShow => 'عدم حضور';

  @override
  String get reservationStatusReleased => 'مُحرَّرة';

  @override
  String get reservationStatusReserved => 'محجوزة';

  @override
  String get reservationTakenError => 'تم حجز هذه الرحلة من كابتن آخر';

  @override
  String get reservationsActive => 'القادمة';

  @override
  String get reservationsEmpty => 'لا توجد حجوزات هنا';

  @override
  String get reservationsHistory => 'السابقة';

  @override
  String get scheduleBooked => 'تم حجز رحلتك المجدولة';

  @override
  String get scheduleConfirm => 'تأكيد الموعد';

  @override
  String get scheduleConfirmedExpired =>
      'لم يعد الموعد ضمن المدة المتاحة، اختر موعداً جديداً';

  @override
  String get scheduleConfirmedLabel => 'موعد الرحلة';

  @override
  String get scheduleDayHeading => 'اليوم';

  @override
  String get scheduleEdit => 'تعديل';

  @override
  String scheduleFavoritePriority(String name) {
    return 'الكابتن المفضل $name يحصل على أولوية حجز رحلتك قبل بقية الكباتن';
  }

  @override
  String get scheduleFixedPrice => 'سعر ثابت للموعد · بدون رسوم ذروة';

  @override
  String get scheduleHourHeading => 'الساعة';

  @override
  String get scheduleLeadTooShortError =>
      'يجب أن يكون الموعد بعد 30 دقيقة على الأقل';

  @override
  String scheduleLeadTooShortUntilError(String time) {
    return 'أقرب موعد متاح $time';
  }

  @override
  String get scheduleMinuteHeading => 'الدقيقة';

  @override
  String get scheduleNothingPicked => 'اختر اليوم والوقت';

  @override
  String schedulePickerCopy(String lead, String window) {
    return 'احجز بعد $lead على الأقل وحتى $window من الآن';
  }

  @override
  String get schedulePickerTitle => 'متى تريد رحلتك؟';

  @override
  String scheduleRide(String name, String price) {
    return 'احجز $name · $price';
  }

  @override
  String get scheduleRulesFallback =>
      'تعذر تحميل شروط الجدولة، نستخدم القيم الافتراضية.';

  @override
  String get scheduleSelectedLabel => 'الموعد المختار';

  @override
  String scheduleTooFar(String time) {
    return 'أبعد موعد متاح $time';
  }

  @override
  String scheduleTooSoon(String time) {
    return 'أقرب موعد متاح $time';
  }

  @override
  String scheduleWindowDays(int days) {
    String _temp0 = intl.Intl.pluralLogic(
      days,
      locale: localeName,
      other: '$days يوماً',
      few: '$days أيام',
      two: 'يومين',
      one: 'يوم واحد',
    );
    return '$_temp0';
  }

  @override
  String get scheduleWindowExceededError =>
      'لا يمكن الجدولة لأكثر من 7 أيام من الآن';

  @override
  String scheduleWindowExceededUntilError(String time) {
    return 'لا يمكن الجدولة بعد $time';
  }

  @override
  String get scheduledBookNew => 'احجز رحلة';

  @override
  String get scheduledCancelButton => 'إلغاء الحجز';

  @override
  String get scheduledCancelledTitle => 'تم إلغاء الحجز';

  @override
  String get scheduledCountdownLabel => 'يبدأ بعد';

  @override
  String get scheduledDetailEyebrow => 'رحلة مجدولة';

  @override
  String get scheduledDriverConfirmedBadge => 'مؤكد';

  @override
  String get scheduledDriverReservedBadge => 'محجوز';

  @override
  String get scheduledDriverTitle => 'الكابتن المحجوز';

  @override
  String get scheduledEmptyCopy => 'احجز رحلتك من الرئيسية واختر «جدولة»';

  @override
  String get scheduledEmptyTitle => 'لا توجد رحلات مجدولة';

  @override
  String get scheduledFareLabel => 'الأجرة التقديرية';

  @override
  String get scheduledLateCancelNote =>
      'انتهت مدة الإلغاء المجاني، وقد تُطبَّق رسوم عند الإلغاء (ستراها قبل التأكيد).';

  @override
  String get scheduledLimitReachedError =>
      'وصلت للحد الأقصى من الرحلات المجدولة';

  @override
  String scheduledLimitReachedMaxError(int max) {
    return 'وصلت للحد الأقصى من الرحلات المجدولة ($max)';
  }

  @override
  String get scheduledLinkCopy => 'حجوزاتك القادمة وحالتها';

  @override
  String get scheduledNoDriverYet =>
      'لم يحجز أي كابتن رحلتك بعد، وسنبدأ البحث تلقائياً قبل الموعد.';

  @override
  String get scheduledPhaseConfirmed => 'الكابتن مؤكد';

  @override
  String get scheduledPhaseEnded => 'انتهت';

  @override
  String get scheduledPhaseInProgress => 'الرحلة جارية';

  @override
  String get scheduledPhaseReserved => 'تم حجزها من كابتن';

  @override
  String get scheduledPhaseSearching => 'جارٍ البحث عن كابتن';

  @override
  String get scheduledPhaseWaiting => 'بانتظار كابتن';

  @override
  String get scheduledSearchStartedCopy =>
      'بدأ البحث عن كابتن لرحلتك، تابعها من شاشة الرحلة.';

  @override
  String scheduledSearchStartsAt(String time) {
    return 'إن لم يحجزها كابتن سنبدأ البحث $time';
  }

  @override
  String get scheduledTrackTrip => 'تتبع الرحلة';

  @override
  String get scheduledTripTitle => 'رحلة مجدولة';

  @override
  String get scheduledTripsCopy => 'رحلاتك القادمة مرتبة حسب الموعد';

  @override
  String get scheduledTripsEyebrow => 'الحجوزات';

  @override
  String get scheduledTripsTitle => 'رحلاتي المجدولة';

  @override
  String tripAirportDropoff(String code) {
    return 'توصيل إلى مطار $code';
  }

  @override
  String tripAirportPickup(String code) {
    return 'استلام من مطار $code';
  }

  @override
  String get tripStatusScheduled => 'مجدولة';

  @override
  String get reservationOpenMarket => 'افتح السوق';

  @override
  String get reservationNotConfirmableNotDueError => 'لم يحن وقت التأكيد بعد';

  @override
  String get reservationNotConfirmableExpiredError => 'انتهت مهلة التأكيد';
}
