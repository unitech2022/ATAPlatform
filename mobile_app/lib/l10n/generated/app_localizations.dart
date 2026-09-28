import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'generated/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// No description provided for @appName.
  ///
  /// In ar, this message translates to:
  /// **'ATA'**
  String get appName;

  /// No description provided for @back.
  ///
  /// In ar, this message translates to:
  /// **'رجوع'**
  String get back;

  /// No description provided for @cancel.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء'**
  String get cancel;

  /// No description provided for @retry.
  ///
  /// In ar, this message translates to:
  /// **'إعادة المحاولة'**
  String get retry;

  /// No description provided for @currency.
  ///
  /// In ar, this message translates to:
  /// **'ر.س'**
  String get currency;

  /// No description provided for @priceWithCurrency.
  ///
  /// In ar, this message translates to:
  /// **'{amount} ر.س'**
  String priceWithCurrency(String amount);

  /// No description provided for @minutesLabel.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{الآن} =1{دقيقة واحدة} =2{دقيقتان} few{{count} دقائق} other{{count} دقيقة}}'**
  String minutesLabel(int count);

  /// No description provided for @errorNetwork.
  ///
  /// In ar, this message translates to:
  /// **'تعذر الاتصال بالخادم، تحقق من اتصالك بالإنترنت'**
  String get errorNetwork;

  /// No description provided for @errorUnexpected.
  ///
  /// In ar, this message translates to:
  /// **'حدث خطأ غير متوقع، حاول مرة أخرى'**
  String get errorUnexpected;

  /// No description provided for @errorUnauthorized.
  ///
  /// In ar, this message translates to:
  /// **'انتهت الجلسة، سجّل الدخول مجدداً'**
  String get errorUnauthorized;

  /// No description provided for @loading.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ التحميل…'**
  String get loading;

  /// No description provided for @comingSoon.
  ///
  /// In ar, this message translates to:
  /// **'قريباً'**
  String get comingSoon;

  /// No description provided for @keypadDelete.
  ///
  /// In ar, this message translates to:
  /// **'حذف'**
  String get keypadDelete;

  /// No description provided for @languageTitle.
  ///
  /// In ar, this message translates to:
  /// **'اختر لغتك · Choose your language'**
  String get languageTitle;

  /// No description provided for @languageCopy.
  ///
  /// In ar, this message translates to:
  /// **'يمكنك تغيير اللغة لاحقاً من الإعدادات · You can change it later in Settings'**
  String get languageCopy;

  /// No description provided for @arabicName.
  ///
  /// In ar, this message translates to:
  /// **'العربية'**
  String get arabicName;

  /// No description provided for @arabicRegion.
  ///
  /// In ar, this message translates to:
  /// **'المملكة العربية السعودية'**
  String get arabicRegion;

  /// No description provided for @arabicContinue.
  ///
  /// In ar, this message translates to:
  /// **'متابعة بالعربية'**
  String get arabicContinue;

  /// No description provided for @englishName.
  ///
  /// In ar, this message translates to:
  /// **'English'**
  String get englishName;

  /// No description provided for @englishRegion.
  ///
  /// In ar, this message translates to:
  /// **'United Kingdom'**
  String get englishRegion;

  /// No description provided for @englishContinue.
  ///
  /// In ar, this message translates to:
  /// **'Continue in English'**
  String get englishContinue;

  /// No description provided for @roleEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'ابدأ رحلتك مع ATA'**
  String get roleEyebrow;

  /// No description provided for @roleTitle.
  ///
  /// In ar, this message translates to:
  /// **'كيف تريد استخدام التطبيق؟'**
  String get roleTitle;

  /// No description provided for @roleCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر نوع الحساب للمتابعة باستخدام رقم جوالك فقط'**
  String get roleCopy;

  /// No description provided for @riderTag.
  ///
  /// In ar, this message translates to:
  /// **'للركاب'**
  String get riderTag;

  /// No description provided for @riderTitle.
  ///
  /// In ar, this message translates to:
  /// **'التسجيل كعميل'**
  String get riderTitle;

  /// No description provided for @riderCopy.
  ///
  /// In ar, this message translates to:
  /// **'اطلب رحلتك خلال دقائق، وتابع الكابتن حتى يصل إليك.'**
  String get riderCopy;

  /// No description provided for @riderCta.
  ///
  /// In ar, this message translates to:
  /// **'ابدأ الآن'**
  String get riderCta;

  /// No description provided for @driverTag.
  ///
  /// In ar, this message translates to:
  /// **'للسائقين'**
  String get driverTag;

  /// No description provided for @driverTitle.
  ///
  /// In ar, this message translates to:
  /// **'التسجيل كسائق'**
  String get driverTitle;

  /// No description provided for @driverCopy.
  ///
  /// In ar, this message translates to:
  /// **'سجّل رقمك، ثم ارفع مستنداتك عبر الموقع ليتم تفعيل حسابك.'**
  String get driverCopy;

  /// No description provided for @driverCta.
  ///
  /// In ar, this message translates to:
  /// **'انضم إلى ATA'**
  String get driverCta;

  /// No description provided for @roleTerms.
  ///
  /// In ar, this message translates to:
  /// **'بمتابعتك، أنت توافق على شروط الاستخدام وسياسة الخصوصية'**
  String get roleTerms;

  /// No description provided for @phoneTitle.
  ///
  /// In ar, this message translates to:
  /// **'أدخل رقم جوالك'**
  String get phoneTitle;

  /// No description provided for @phoneCopyRider.
  ///
  /// In ar, this message translates to:
  /// **'سنرسل لك رمز تحقق لتأكيد الرقم وإنشاء حسابك.'**
  String get phoneCopyRider;

  /// No description provided for @phoneCopyDriver.
  ///
  /// In ar, this message translates to:
  /// **'سنرسل لك رمز تحقق لتأكيد الرقم وبدء طلب الانضمام كسائق.'**
  String get phoneCopyDriver;

  /// No description provided for @phonePlaceholder.
  ///
  /// In ar, this message translates to:
  /// **'5X XXX XXXX'**
  String get phonePlaceholder;

  /// No description provided for @phoneCountryCode.
  ///
  /// In ar, this message translates to:
  /// **'+966'**
  String get phoneCountryCode;

  /// No description provided for @phoneSend.
  ///
  /// In ar, this message translates to:
  /// **'إرسال رمز التحقق'**
  String get phoneSend;

  /// No description provided for @phoneInvalid.
  ///
  /// In ar, this message translates to:
  /// **'أدخل رقم جوال سعودي صحيح يبدأ بـ 5'**
  String get phoneInvalid;

  /// No description provided for @rateLimited.
  ///
  /// In ar, this message translates to:
  /// **'تم تجاوز الحد المسموح، حاول بعد {seconds} ثانية'**
  String rateLimited(int seconds);

  /// No description provided for @otpTitle.
  ///
  /// In ar, this message translates to:
  /// **'تحقق من رقمك'**
  String get otpTitle;

  /// No description provided for @otpCopy.
  ///
  /// In ar, this message translates to:
  /// **'أرسلنا رمزاً من 4 أرقام إلى'**
  String get otpCopy;

  /// No description provided for @otpVerify.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد ومتابعة'**
  String get otpVerify;

  /// No description provided for @otpResend.
  ///
  /// In ar, this message translates to:
  /// **'إعادة إرسال الرمز'**
  String get otpResend;

  /// No description provided for @otpResendIn.
  ///
  /// In ar, this message translates to:
  /// **'إعادة الإرسال بعد {seconds} ث'**
  String otpResendIn(int seconds);

  /// No description provided for @otpDevHint.
  ///
  /// In ar, this message translates to:
  /// **'رمز التطوير: {code}'**
  String otpDevHint(String code);

  /// No description provided for @otpInvalid.
  ///
  /// In ar, this message translates to:
  /// **'رمز التحقق غير صحيح، المحاولات المتبقية: {attempts}'**
  String otpInvalid(int attempts);

  /// No description provided for @otpExpired.
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية الرمز، أعد الإرسال'**
  String get otpExpired;

  /// No description provided for @otpLocked.
  ///
  /// In ar, this message translates to:
  /// **'تم قفل التحقق مؤقتاً، حاول لاحقاً'**
  String get otpLocked;

  /// No description provided for @termsEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'خطوة أخيرة'**
  String get termsEyebrow;

  /// No description provided for @termsTitle.
  ///
  /// In ar, this message translates to:
  /// **'ما اسمك؟'**
  String get termsTitle;

  /// No description provided for @termsCopy.
  ///
  /// In ar, this message translates to:
  /// **'أدخل اسمك كما تحب أن يناديك الكابتن ووافق على الشروط للمتابعة.'**
  String get termsCopy;

  /// No description provided for @fullNameLabel.
  ///
  /// In ar, this message translates to:
  /// **'الاسم الكامل'**
  String get fullNameLabel;

  /// No description provided for @fullNameHint.
  ///
  /// In ar, this message translates to:
  /// **'مثال: عبدالله محمد'**
  String get fullNameHint;

  /// No description provided for @acceptTermsLabel.
  ///
  /// In ar, this message translates to:
  /// **'أوافق على شروط الاستخدام وسياسة الخصوصية'**
  String get acceptTermsLabel;

  /// No description provided for @termsContinue.
  ///
  /// In ar, this message translates to:
  /// **'ابدأ استخدام ATA'**
  String get termsContinue;

  /// No description provided for @pendingTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم إنشاء طلبك بنجاح'**
  String get pendingTitle;

  /// No description provided for @pendingCopy.
  ///
  /// In ar, this message translates to:
  /// **'حسابك كسائق غير نشط حالياً. أكمل رفع المستندات عبر موقع ATA ليقوم فريق الإدارة بمراجعتها وتفعيل حسابك.'**
  String get pendingCopy;

  /// No description provided for @pendingReviewTitle.
  ///
  /// In ar, this message translates to:
  /// **'طلبك قيد المراجعة'**
  String get pendingReviewTitle;

  /// No description provided for @pendingReviewCopy.
  ///
  /// In ar, this message translates to:
  /// **'يراجع فريق الإدارة مستنداتك الآن، وسنعلمك عبر الجوال فور تفعيل حسابك.'**
  String get pendingReviewCopy;

  /// No description provided for @pendingRejectedTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم رفض الطلب'**
  String get pendingRejectedTitle;

  /// No description provided for @pendingRejectedCopy.
  ///
  /// In ar, this message translates to:
  /// **'راجع سبب الرفض وحدّث مستنداتك عبر موقع ATA ثم أعد إرسال الطلب.'**
  String get pendingRejectedCopy;

  /// No description provided for @pendingSuspendedTitle.
  ///
  /// In ar, this message translates to:
  /// **'الحساب موقوف'**
  String get pendingSuspendedTitle;

  /// No description provided for @pendingSuspendedCopy.
  ///
  /// In ar, this message translates to:
  /// **'تم إيقاف حسابك مؤقتاً. تواصل مع فريق دعم السائقين لمزيد من التفاصيل.'**
  String get pendingSuspendedCopy;

  /// No description provided for @pendingApprovedTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم تفعيل حسابك'**
  String get pendingApprovedTitle;

  /// No description provided for @pendingApprovedCopy.
  ///
  /// In ar, this message translates to:
  /// **'حسابك كسائق نشط الآن. افتح بوابة السائق لبدء استقبال الرحلات.'**
  String get pendingApprovedCopy;

  /// No description provided for @applicationNumber.
  ///
  /// In ar, this message translates to:
  /// **'رقم الطلب'**
  String get applicationNumber;

  /// No description provided for @step1Title.
  ///
  /// In ar, this message translates to:
  /// **'رفع المستندات'**
  String get step1Title;

  /// No description provided for @step1Copy.
  ///
  /// In ar, this message translates to:
  /// **'عبر موقع ATA'**
  String get step1Copy;

  /// No description provided for @step2Title.
  ///
  /// In ar, this message translates to:
  /// **'مراجعة الإدارة'**
  String get step2Title;

  /// No description provided for @step2Copy.
  ///
  /// In ar, this message translates to:
  /// **'خلال 24–48 ساعة'**
  String get step2Copy;

  /// No description provided for @step3Title.
  ///
  /// In ar, this message translates to:
  /// **'تفعيل الحساب'**
  String get step3Title;

  /// No description provided for @step3Copy.
  ///
  /// In ar, this message translates to:
  /// **'إشعار عبر الجوال'**
  String get step3Copy;

  /// No description provided for @requiredDocuments.
  ///
  /// In ar, this message translates to:
  /// **'المستندات المطلوبة'**
  String get requiredDocuments;

  /// No description provided for @docStatusRequired.
  ///
  /// In ar, this message translates to:
  /// **'مطلوب'**
  String get docStatusRequired;

  /// No description provided for @docStatusPending.
  ///
  /// In ar, this message translates to:
  /// **'قيد المراجعة'**
  String get docStatusPending;

  /// No description provided for @docStatusVerified.
  ///
  /// In ar, this message translates to:
  /// **'مقبول'**
  String get docStatusVerified;

  /// No description provided for @docStatusRejected.
  ///
  /// In ar, this message translates to:
  /// **'مرفوض'**
  String get docStatusRejected;

  /// No description provided for @docNationalId.
  ///
  /// In ar, this message translates to:
  /// **'الهوية الوطنية أو الإقامة'**
  String get docNationalId;

  /// No description provided for @docDrivingLicense.
  ///
  /// In ar, this message translates to:
  /// **'رخصة قيادة سارية'**
  String get docDrivingLicense;

  /// No description provided for @docVehicleRegistration.
  ///
  /// In ar, this message translates to:
  /// **'استمارة المركبة'**
  String get docVehicleRegistration;

  /// No description provided for @docPersonalPhoto.
  ///
  /// In ar, this message translates to:
  /// **'صورة شخصية واضحة'**
  String get docPersonalPhoto;

  /// No description provided for @openUploadPortal.
  ///
  /// In ar, this message translates to:
  /// **'الانتقال لموقع رفع المستندات'**
  String get openUploadPortal;

  /// No description provided for @openDriverPortal.
  ///
  /// In ar, this message translates to:
  /// **'فتح بوابة السائق'**
  String get openDriverPortal;

  /// No description provided for @backToLogin.
  ///
  /// In ar, this message translates to:
  /// **'العودة إلى تسجيل الدخول'**
  String get backToLogin;

  /// No description provided for @refreshStatus.
  ///
  /// In ar, this message translates to:
  /// **'تحديث الحالة'**
  String get refreshStatus;

  /// No description provided for @rejectionReason.
  ///
  /// In ar, this message translates to:
  /// **'سبب الرفض: {reason}'**
  String rejectionReason(String reason);

  /// No description provided for @portalOpenFailed.
  ///
  /// In ar, this message translates to:
  /// **'تعذر فتح الرابط'**
  String get portalOpenFailed;

  /// No description provided for @navHome.
  ///
  /// In ar, this message translates to:
  /// **'الرئيسية'**
  String get navHome;

  /// No description provided for @navRides.
  ///
  /// In ar, this message translates to:
  /// **'رحلاتي'**
  String get navRides;

  /// No description provided for @navWallet.
  ///
  /// In ar, this message translates to:
  /// **'المحفظة'**
  String get navWallet;

  /// No description provided for @navAccount.
  ///
  /// In ar, this message translates to:
  /// **'حسابي'**
  String get navAccount;

  /// No description provided for @notificationsTitle.
  ///
  /// In ar, this message translates to:
  /// **'الإشعارات'**
  String get notificationsTitle;

  /// No description provided for @manageNotifications.
  ///
  /// In ar, this message translates to:
  /// **'إدارة الإشعارات'**
  String get manageNotifications;

  /// No description provided for @notificationsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد إشعارات جديدة'**
  String get notificationsEmpty;

  /// No description provided for @markAllRead.
  ///
  /// In ar, this message translates to:
  /// **'تحديد الكل كمقروء'**
  String get markAllRead;

  /// No description provided for @menuViewProfile.
  ///
  /// In ar, this message translates to:
  /// **'عرض الملف الشخصي'**
  String get menuViewProfile;

  /// No description provided for @menuSettings.
  ///
  /// In ar, this message translates to:
  /// **'الإعدادات'**
  String get menuSettings;

  /// No description provided for @menuAccountSettings.
  ///
  /// In ar, this message translates to:
  /// **'إعدادات الحساب'**
  String get menuAccountSettings;

  /// No description provided for @menuLanguage.
  ///
  /// In ar, this message translates to:
  /// **'اللغة'**
  String get menuLanguage;

  /// No description provided for @menuNotifications.
  ///
  /// In ar, this message translates to:
  /// **'الإشعارات'**
  String get menuNotifications;

  /// No description provided for @menuSafety.
  ///
  /// In ar, this message translates to:
  /// **'السلامة والخصوصية'**
  String get menuSafety;

  /// No description provided for @menuContact.
  ///
  /// In ar, this message translates to:
  /// **'اتصل بنا'**
  String get menuContact;

  /// No description provided for @guestName.
  ///
  /// In ar, this message translates to:
  /// **'عميل ATA'**
  String get guestName;

  /// No description provided for @homeEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'أهلاً بك في ATA'**
  String get homeEyebrow;

  /// No description provided for @homeTitle.
  ///
  /// In ar, this message translates to:
  /// **'إلى أين تود الذهاب؟'**
  String get homeTitle;

  /// No description provided for @pickupLabel.
  ///
  /// In ar, this message translates to:
  /// **'موقع الانطلاق'**
  String get pickupLabel;

  /// No description provided for @pickupCurrent.
  ///
  /// In ar, this message translates to:
  /// **'موقعك الحالي'**
  String get pickupCurrent;

  /// No description provided for @stopLabel.
  ///
  /// In ar, this message translates to:
  /// **'محطة إضافية'**
  String get stopLabel;

  /// No description provided for @removeStop.
  ///
  /// In ar, this message translates to:
  /// **'حذف'**
  String get removeStop;

  /// No description provided for @destinationLabel.
  ///
  /// In ar, this message translates to:
  /// **'الوجهة'**
  String get destinationLabel;

  /// No description provided for @destinationDefault.
  ///
  /// In ar, this message translates to:
  /// **'واجهة الرياض'**
  String get destinationDefault;

  /// No description provided for @addStop.
  ///
  /// In ar, this message translates to:
  /// **'إضافة محطة أخرى'**
  String get addStop;

  /// No description provided for @maxStopsReached.
  ///
  /// In ar, this message translates to:
  /// **'تمت إضافة الحد الأقصى للمحطات'**
  String get maxStopsReached;

  /// No description provided for @stopOption1.
  ///
  /// In ar, this message translates to:
  /// **'النخيل مول'**
  String get stopOption1;

  /// No description provided for @stopOption2.
  ///
  /// In ar, this message translates to:
  /// **'برج المملكة'**
  String get stopOption2;

  /// No description provided for @stopOption3.
  ///
  /// In ar, this message translates to:
  /// **'حديقة الملك عبدالله'**
  String get stopOption3;

  /// No description provided for @timeNow.
  ///
  /// In ar, this message translates to:
  /// **'الآن'**
  String get timeNow;

  /// No description provided for @timeSchedule.
  ///
  /// In ar, this message translates to:
  /// **'جدولة'**
  String get timeSchedule;

  /// No description provided for @scheduleComingSoon.
  ///
  /// In ar, this message translates to:
  /// **'الجدولة متاحة قريباً (حتى 7 أيام مسبقاً)'**
  String get scheduleComingSoon;

  /// No description provided for @femaleDriverTitle.
  ///
  /// In ar, this message translates to:
  /// **'أفضّل سائقة'**
  String get femaleDriverTitle;

  /// No description provided for @femaleDriverTag.
  ///
  /// In ar, this message translates to:
  /// **'للعميلات'**
  String get femaleDriverTag;

  /// No description provided for @femaleDriverCopy.
  ///
  /// In ar, this message translates to:
  /// **'خيار مخصص للنساء لطلب سائقة عند توفرها'**
  String get femaleDriverCopy;

  /// No description provided for @chooseRide.
  ///
  /// In ar, this message translates to:
  /// **'اختر رحلتك'**
  String get chooseRide;

  /// No description provided for @pricesEstimated.
  ///
  /// In ar, this message translates to:
  /// **'الأسعار تقديرية'**
  String get pricesEstimated;

  /// No description provided for @paymentMethod.
  ///
  /// In ar, this message translates to:
  /// **'طريقة الدفع'**
  String get paymentMethod;

  /// No description provided for @paymentCash.
  ///
  /// In ar, this message translates to:
  /// **'نقداً'**
  String get paymentCash;

  /// No description provided for @paymentWallet.
  ///
  /// In ar, this message translates to:
  /// **'محفظة ATA'**
  String get paymentWallet;

  /// No description provided for @paymentCard.
  ///
  /// In ar, this message translates to:
  /// **'بطاقة'**
  String get paymentCard;

  /// No description provided for @requestRide.
  ///
  /// In ar, this message translates to:
  /// **'اطلب {name} · {price}'**
  String requestRide(String name, String price);

  /// No description provided for @safeRide.
  ///
  /// In ar, this message translates to:
  /// **'رحلتك آمنة ومتابعة على مدار الساعة'**
  String get safeRide;

  /// No description provided for @searchingTitle.
  ///
  /// In ar, this message translates to:
  /// **'جاري البحث عن كابتن'**
  String get searchingTitle;

  /// No description provided for @searchingCopy.
  ///
  /// In ar, this message translates to:
  /// **'نبحث لك عن أقرب كابتن. سيصل إليك خلال {eta}.'**
  String searchingCopy(String eta);

  /// No description provided for @cashOnArrival.
  ///
  /// In ar, this message translates to:
  /// **'الدفع نقداً عند الوصول'**
  String get cashOnArrival;

  /// No description provided for @payWithWallet.
  ///
  /// In ar, this message translates to:
  /// **'الدفع من محفظة ATA'**
  String get payWithWallet;

  /// No description provided for @payWithCard.
  ///
  /// In ar, this message translates to:
  /// **'الدفع بالبطاقة'**
  String get payWithCard;

  /// No description provided for @extraStopsCount.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{محطة إضافية واحدة} =2{محطتان إضافيتان} few{{count} محطات إضافية} other{{count} محطة إضافية}}'**
  String extraStopsCount(int count);

  /// No description provided for @femaleRequestedTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم طلب سائقة'**
  String get femaleRequestedTitle;

  /// No description provided for @femaleRequestedCopy.
  ///
  /// In ar, this message translates to:
  /// **'سنبحث عن أقرب سائقة متاحة'**
  String get femaleRequestedCopy;

  /// No description provided for @cancelRequest.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الطلب'**
  String get cancelRequest;

  /// No description provided for @categoriesError.
  ///
  /// In ar, this message translates to:
  /// **'تعذر تحميل فئات الرحلات'**
  String get categoriesError;

  /// No description provided for @ridesEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'نشاطك'**
  String get ridesEyebrow;

  /// No description provided for @ridesTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلاتي'**
  String get ridesTitle;

  /// No description provided for @ridesCopy.
  ///
  /// In ar, this message translates to:
  /// **'راجع رحلاتك السابقة، تفاصيل الدفع، واطلب نفس الرحلة من جديد.'**
  String get ridesCopy;

  /// No description provided for @recentTrips.
  ///
  /// In ar, this message translates to:
  /// **'آخر الرحلات'**
  String get recentTrips;

  /// No description provided for @allTrips.
  ///
  /// In ar, this message translates to:
  /// **'الكل'**
  String get allTrips;

  /// No description provided for @tripStatusCompleted.
  ///
  /// In ar, this message translates to:
  /// **'مكتملة'**
  String get tripStatusCompleted;

  /// No description provided for @tripStatusCancelled.
  ///
  /// In ar, this message translates to:
  /// **'ملغاة'**
  String get tripStatusCancelled;

  /// No description provided for @tripStatusActive.
  ///
  /// In ar, this message translates to:
  /// **'جارية'**
  String get tripStatusActive;

  /// No description provided for @ridesEmptyTitle.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد رحلات بعد'**
  String get ridesEmptyTitle;

  /// No description provided for @ridesEmptyCopy.
  ///
  /// In ar, this message translates to:
  /// **'ستظهر رحلاتك هنا بعد أول طلب.'**
  String get ridesEmptyCopy;

  /// No description provided for @promoTitle.
  ///
  /// In ar, this message translates to:
  /// **'وجهتك المعتادة أقرب'**
  String get promoTitle;

  /// No description provided for @promoCopy.
  ///
  /// In ar, this message translates to:
  /// **'احفظ الأماكن التي تزورها كثيراً لطلب رحلتك بخطوة واحدة.'**
  String get promoCopy;

  /// No description provided for @promoCta.
  ///
  /// In ar, this message translates to:
  /// **'احجز رحلة الآن'**
  String get promoCta;

  /// No description provided for @tripRoute.
  ///
  /// In ar, this message translates to:
  /// **'{pickup} ← {destination}'**
  String tripRoute(String pickup, String destination);

  /// No description provided for @walletEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'مدفوعات آمنة'**
  String get walletEyebrow;

  /// No description provided for @walletTitle.
  ///
  /// In ar, this message translates to:
  /// **'محفظة ATA'**
  String get walletTitle;

  /// No description provided for @walletCopy.
  ///
  /// In ar, this message translates to:
  /// **'تحكم في رصيدك وطرق الدفع، واطّلع على كل معاملاتك من مكان واحد.'**
  String get walletCopy;

  /// No description provided for @currentBalance.
  ///
  /// In ar, this message translates to:
  /// **'رصيدك الحالي'**
  String get currentBalance;

  /// No description provided for @paymentMethods.
  ///
  /// In ar, this message translates to:
  /// **'طرق الدفع'**
  String get paymentMethods;

  /// No description provided for @topUp.
  ///
  /// In ar, this message translates to:
  /// **'شحن المحفظة'**
  String get topUp;

  /// No description provided for @walletBalanceLine.
  ///
  /// In ar, this message translates to:
  /// **'الرصيد: {amount}'**
  String walletBalanceLine(String amount);

  /// No description provided for @madaCard.
  ///
  /// In ar, this message translates to:
  /// **'بطاقة مدى'**
  String get madaCard;

  /// No description provided for @cardEnding.
  ///
  /// In ar, this message translates to:
  /// **'تنتهي بـ {digits}'**
  String cardEnding(String digits);

  /// No description provided for @payCash.
  ///
  /// In ar, this message translates to:
  /// **'الدفع نقداً'**
  String get payCash;

  /// No description provided for @payCashCopy.
  ///
  /// In ar, this message translates to:
  /// **'ادفع للكابتن بعد الرحلة'**
  String get payCashCopy;

  /// No description provided for @topUpCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر المبلغ الذي تريد إضافته إلى رصيد ATA.'**
  String get topUpCopy;

  /// No description provided for @topUpAmount.
  ///
  /// In ar, this message translates to:
  /// **'مبلغ الشحن'**
  String get topUpAmount;

  /// No description provided for @confirmTopUp.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد شحن {amount}'**
  String confirmTopUp(String amount);

  /// No description provided for @topUpSuccessTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم شحن المحفظة'**
  String get topUpSuccessTitle;

  /// No description provided for @topUpSuccessCopy.
  ///
  /// In ar, this message translates to:
  /// **'تمت إضافة {amount} إلى رصيد محفظتك بنجاح.'**
  String topUpSuccessCopy(String amount);

  /// No description provided for @newBalance.
  ///
  /// In ar, this message translates to:
  /// **'الرصيد الجديد'**
  String get newBalance;

  /// No description provided for @backToWallet.
  ///
  /// In ar, this message translates to:
  /// **'العودة إلى المحفظة'**
  String get backToWallet;

  /// No description provided for @sandboxMethod.
  ///
  /// In ar, this message translates to:
  /// **'الدفع التجريبي'**
  String get sandboxMethod;

  /// No description provided for @sandboxCopy.
  ///
  /// In ar, this message translates to:
  /// **'بيئة تجريبية، لا يتم خصم أي مبلغ'**
  String get sandboxCopy;

  /// No description provided for @walletError.
  ///
  /// In ar, this message translates to:
  /// **'تعذر تحميل المحفظة'**
  String get walletError;

  /// No description provided for @safetyEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'السلامة أولاً'**
  String get safetyEyebrow;

  /// No description provided for @safetyTitle.
  ///
  /// In ar, this message translates to:
  /// **'أمانك في كل رحلة'**
  String get safetyTitle;

  /// No description provided for @safetyCopy.
  ///
  /// In ar, this message translates to:
  /// **'أدوات ذكية وفريق دعم متاح دائماً ليمنحك تجربة مطمئنة من الانطلاق حتى الوصول.'**
  String get safetyCopy;

  /// No description provided for @shareTripTitle.
  ///
  /// In ar, this message translates to:
  /// **'مشاركة الرحلة'**
  String get shareTripTitle;

  /// No description provided for @shareTripCopy.
  ///
  /// In ar, this message translates to:
  /// **'أرسل مسارك ومعلومات الكابتن لأشخاص تثق بهم.'**
  String get shareTripCopy;

  /// No description provided for @helpCenterTitle.
  ///
  /// In ar, this message translates to:
  /// **'مركز المساعدة'**
  String get helpCenterTitle;

  /// No description provided for @helpCenterCopy.
  ///
  /// In ar, this message translates to:
  /// **'تواصل مباشرة مع فريق السلامة على مدار الساعة.'**
  String get helpCenterCopy;

  /// No description provided for @trustedContactsTitle.
  ///
  /// In ar, this message translates to:
  /// **'جهات موثوقة'**
  String get trustedContactsTitle;

  /// No description provided for @trustedContactsCopy.
  ///
  /// In ar, this message translates to:
  /// **'أضف أشخاصاً ليتم تنبيههم عند الحاجة.'**
  String get trustedContactsCopy;

  /// No description provided for @learnMore.
  ///
  /// In ar, this message translates to:
  /// **'معرفة المزيد'**
  String get learnMore;

  /// No description provided for @emergencyTitle.
  ///
  /// In ar, this message translates to:
  /// **'هل تحتاج مساعدة عاجلة؟'**
  String get emergencyTitle;

  /// No description provided for @emergencyCopy.
  ///
  /// In ar, this message translates to:
  /// **'فريق السلامة متاح الآن'**
  String get emergencyCopy;

  /// No description provided for @contactUs.
  ///
  /// In ar, this message translates to:
  /// **'تواصل معنا'**
  String get contactUs;

  /// No description provided for @accountEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'حسابي'**
  String get accountEyebrow;

  /// No description provided for @accountWelcome.
  ///
  /// In ar, this message translates to:
  /// **'مرحباً، {name}'**
  String accountWelcome(String name);

  /// No description provided for @accountCopy.
  ///
  /// In ar, this message translates to:
  /// **'أدر بياناتك، إعدادات رحلاتك، وخيارات الخصوصية.'**
  String get accountCopy;

  /// No description provided for @memberSince.
  ///
  /// In ar, this message translates to:
  /// **'عضو منذ {year}'**
  String memberSince(String year);

  /// No description provided for @passengerRating.
  ///
  /// In ar, this message translates to:
  /// **'تقييم الركاب'**
  String get passengerRating;

  /// No description provided for @settings.
  ///
  /// In ar, this message translates to:
  /// **'الإعدادات'**
  String get settings;

  /// No description provided for @personalInfo.
  ///
  /// In ar, this message translates to:
  /// **'البيانات الشخصية'**
  String get personalInfo;

  /// No description provided for @personalInfoCopy.
  ///
  /// In ar, this message translates to:
  /// **'الاسم، رقم الجوال والبريد'**
  String get personalInfoCopy;

  /// No description provided for @savedPlaces.
  ///
  /// In ar, this message translates to:
  /// **'الأماكن المحفوظة'**
  String get savedPlaces;

  /// No description provided for @savedPlacesCopy.
  ///
  /// In ar, this message translates to:
  /// **'المنزل والعمل'**
  String get savedPlacesCopy;

  /// No description provided for @privacySecurity.
  ///
  /// In ar, this message translates to:
  /// **'الخصوصية والأمان'**
  String get privacySecurity;

  /// No description provided for @privacySecurityCopy.
  ///
  /// In ar, this message translates to:
  /// **'إدارة بياناتك وصلاحياتك'**
  String get privacySecurityCopy;

  /// No description provided for @notificationsRow.
  ///
  /// In ar, this message translates to:
  /// **'الإشعارات'**
  String get notificationsRow;

  /// No description provided for @notificationsRowCopy.
  ///
  /// In ar, this message translates to:
  /// **'تحكم في التنبيهات والعروض'**
  String get notificationsRowCopy;

  /// No description provided for @languageRow.
  ///
  /// In ar, this message translates to:
  /// **'اللغة'**
  String get languageRow;

  /// No description provided for @languageArabic.
  ///
  /// In ar, this message translates to:
  /// **'العربية'**
  String get languageArabic;

  /// No description provided for @languageEnglish.
  ///
  /// In ar, this message translates to:
  /// **'English'**
  String get languageEnglish;

  /// No description provided for @contactRow.
  ///
  /// In ar, this message translates to:
  /// **'اتصل بنا'**
  String get contactRow;

  /// No description provided for @contactRowCopy.
  ///
  /// In ar, this message translates to:
  /// **'الدعم والمساعدة'**
  String get contactRowCopy;

  /// No description provided for @termsRow.
  ///
  /// In ar, this message translates to:
  /// **'الشروط والأحكام'**
  String get termsRow;

  /// No description provided for @termsRowCopy.
  ///
  /// In ar, this message translates to:
  /// **'شروط استخدام خدمات ATA'**
  String get termsRowCopy;

  /// No description provided for @logout.
  ///
  /// In ar, this message translates to:
  /// **'تسجيل الخروج'**
  String get logout;

  /// No description provided for @deleteApp.
  ///
  /// In ar, this message translates to:
  /// **'حذف التطبيق'**
  String get deleteApp;

  /// No description provided for @backToSettings.
  ///
  /// In ar, this message translates to:
  /// **'العودة إلى الإعدادات'**
  String get backToSettings;

  /// No description provided for @languagePanelEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'التفضيلات'**
  String get languagePanelEyebrow;

  /// No description provided for @languagePanelTitle.
  ///
  /// In ar, this message translates to:
  /// **'لغة التطبيق'**
  String get languagePanelTitle;

  /// No description provided for @languagePanelCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر اللغة التي تفضل استخدامها داخل تطبيق ATA.'**
  String get languagePanelCopy;

  /// No description provided for @languageArabicCopy.
  ///
  /// In ar, this message translates to:
  /// **'العربية — المملكة العربية السعودية'**
  String get languageArabicCopy;

  /// No description provided for @languageEnglishCopy.
  ///
  /// In ar, this message translates to:
  /// **'English — United Kingdom'**
  String get languageEnglishCopy;

  /// No description provided for @languageSavedNote.
  ///
  /// In ar, this message translates to:
  /// **'سيتم حفظ اختيار اللغة تلقائياً واستخدامه عند فتح التطبيق مرة أخرى.'**
  String get languageSavedNote;

  /// No description provided for @notifPrefsEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'ابقَ على اطلاع'**
  String get notifPrefsEyebrow;

  /// No description provided for @notifPrefsTitle.
  ///
  /// In ar, this message translates to:
  /// **'الإشعارات'**
  String get notifPrefsTitle;

  /// No description provided for @notifPrefsCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر التنبيهات التي ترغب في استقبالها من ATA.'**
  String get notifPrefsCopy;

  /// No description provided for @prefTripsTitle.
  ///
  /// In ar, this message translates to:
  /// **'تنبيهات الرحلات'**
  String get prefTripsTitle;

  /// No description provided for @prefTripsCopy.
  ///
  /// In ar, this message translates to:
  /// **'حالة الطلب، وصول السائق، وتحديثات الرحلة'**
  String get prefTripsCopy;

  /// No description provided for @prefWalletTitle.
  ///
  /// In ar, this message translates to:
  /// **'المحفظة والمدفوعات'**
  String get prefWalletTitle;

  /// No description provided for @prefWalletCopy.
  ///
  /// In ar, this message translates to:
  /// **'عمليات الشحن، الخصم، وإيصالات الرحلات'**
  String get prefWalletCopy;

  /// No description provided for @prefSafetyTitle.
  ///
  /// In ar, this message translates to:
  /// **'تنبيهات السلامة'**
  String get prefSafetyTitle;

  /// No description provided for @prefSafetyCopy.
  ///
  /// In ar, this message translates to:
  /// **'التنبيهات المهمة وتحديثات الأمان'**
  String get prefSafetyCopy;

  /// No description provided for @prefOffersTitle.
  ///
  /// In ar, this message translates to:
  /// **'العروض والأخبار'**
  String get prefOffersTitle;

  /// No description provided for @prefOffersCopy.
  ///
  /// In ar, this message translates to:
  /// **'الخصومات والعروض الحصرية من ATA'**
  String get prefOffersCopy;

  /// No description provided for @contactEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'نحن هنا لمساعدتك'**
  String get contactEyebrow;

  /// No description provided for @contactTitle.
  ///
  /// In ar, this message translates to:
  /// **'اتصل بنا'**
  String get contactTitle;

  /// No description provided for @contactCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر الطريقة الأنسب للتواصل مع فريق دعم ATA.'**
  String get contactCopy;

  /// No description provided for @liveChatTitle.
  ///
  /// In ar, this message translates to:
  /// **'المحادثة المباشرة'**
  String get liveChatTitle;

  /// No description provided for @liveChatValue.
  ///
  /// In ar, this message translates to:
  /// **'متاحون الآن'**
  String get liveChatValue;

  /// No description provided for @liveChatCopy.
  ///
  /// In ar, this message translates to:
  /// **'ابدأ المحادثة'**
  String get liveChatCopy;

  /// No description provided for @callTitle.
  ///
  /// In ar, this message translates to:
  /// **'اتصل بنا'**
  String get callTitle;

  /// No description provided for @callValue.
  ///
  /// In ar, this message translates to:
  /// **'9200 123 45'**
  String get callValue;

  /// No description provided for @callCopy.
  ///
  /// In ar, this message translates to:
  /// **'يومياً، على مدار الساعة'**
  String get callCopy;

  /// No description provided for @emailTitle.
  ///
  /// In ar, this message translates to:
  /// **'البريد الإلكتروني'**
  String get emailTitle;

  /// No description provided for @emailValue.
  ///
  /// In ar, this message translates to:
  /// **'help@ata.sa'**
  String get emailValue;

  /// No description provided for @emailCopy.
  ///
  /// In ar, this message translates to:
  /// **'نرد خلال 24 ساعة'**
  String get emailCopy;

  /// No description provided for @termsPanelEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'آخر تحديث: يناير 2025'**
  String get termsPanelEyebrow;

  /// No description provided for @termsPanelTitle.
  ///
  /// In ar, this message translates to:
  /// **'الشروط والأحكام'**
  String get termsPanelTitle;

  /// No description provided for @termsPanelCopy.
  ///
  /// In ar, this message translates to:
  /// **'يرجى قراءة شروط استخدام خدمات ATA بعناية.'**
  String get termsPanelCopy;

  /// No description provided for @terms1Title.
  ///
  /// In ar, this message translates to:
  /// **'1. استخدام الخدمة'**
  String get terms1Title;

  /// No description provided for @terms1Copy.
  ///
  /// In ar, this message translates to:
  /// **'يوفر تطبيق ATA منصة تقنية لطلب خدمات النقل. باستخدام التطبيق، يقر المستخدم بصحة البيانات المقدمة والتزامه بالأنظمة المعمول بها.'**
  String get terms1Copy;

  /// No description provided for @terms2Title.
  ///
  /// In ar, this message translates to:
  /// **'2. الحساب والمسؤولية'**
  String get terms2Title;

  /// No description provided for @terms2Copy.
  ///
  /// In ar, this message translates to:
  /// **'يتحمل المستخدم مسؤولية حماية رقم جواله وحسابه، وإبلاغ فريق الدعم فوراً عند الاشتباه في أي استخدام غير مصرح به.'**
  String get terms2Copy;

  /// No description provided for @terms3Title.
  ///
  /// In ar, this message translates to:
  /// **'3. الرحلات والمدفوعات'**
  String get terms3Title;

  /// No description provided for @terms3Copy.
  ///
  /// In ar, this message translates to:
  /// **'تظهر تكلفة الرحلة التقديرية قبل الطلب، وقد تتغير وفقاً للمسافة والوقت الفعليين أو الرسوم النظامية الإضافية.'**
  String get terms3Copy;

  /// No description provided for @terms4Title.
  ///
  /// In ar, this message translates to:
  /// **'4. الخصوصية'**
  String get terms4Title;

  /// No description provided for @terms4Copy.
  ///
  /// In ar, this message translates to:
  /// **'تُعالج بيانات الموقع والرحلات بهدف تقديم الخدمة وتحسينها، وفق سياسة الخصوصية ومعايير حماية البيانات المعتمدة.'**
  String get terms4Copy;

  /// No description provided for @deleteTitle.
  ///
  /// In ar, this message translates to:
  /// **'حذف التطبيق والبيانات؟'**
  String get deleteTitle;

  /// No description provided for @deleteCopy.
  ///
  /// In ar, this message translates to:
  /// **'سيتم حذف حسابك وسجل رحلاتك وبياناتك المحفوظة نهائياً. لا يمكن التراجع عن هذا الإجراء.'**
  String get deleteCopy;

  /// No description provided for @deleteWarning.
  ///
  /// In ar, this message translates to:
  /// **'لن تتمكن من استعادة بيانات الحساب بعد تأكيد الحذف.'**
  String get deleteWarning;

  /// No description provided for @confirmDelete.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد الحذف'**
  String get confirmDelete;

  /// No description provided for @driverPortal.
  ///
  /// In ar, this message translates to:
  /// **'بوابة السائق'**
  String get driverPortal;

  /// No description provided for @driverAccountEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'حساب السائق'**
  String get driverAccountEyebrow;

  /// No description provided for @driverWelcome.
  ///
  /// In ar, this message translates to:
  /// **'مرحباً، {name}'**
  String driverWelcome(String name);

  /// No description provided for @driverDashboardCopy.
  ///
  /// In ar, this message translates to:
  /// **'أدر رحلاتك وأرباحك وبيانات حسابك من مكان واحد.'**
  String get driverDashboardCopy;

  /// No description provided for @driverGuestName.
  ///
  /// In ar, this message translates to:
  /// **'كابتن ATA'**
  String get driverGuestName;

  /// No description provided for @onlineLabel.
  ///
  /// In ar, this message translates to:
  /// **'متاح لاستقبال الرحلات'**
  String get onlineLabel;

  /// No description provided for @offlineLabel.
  ///
  /// In ar, this message translates to:
  /// **'غير متصل'**
  String get offlineLabel;

  /// No description provided for @tabOverview.
  ///
  /// In ar, this message translates to:
  /// **'نظرة عامة'**
  String get tabOverview;

  /// No description provided for @tabDocuments.
  ///
  /// In ar, this message translates to:
  /// **'المستندات والمركبة'**
  String get tabDocuments;

  /// No description provided for @tabSettings.
  ///
  /// In ar, this message translates to:
  /// **'إعدادات الحساب'**
  String get tabSettings;

  /// No description provided for @statEarningsToday.
  ///
  /// In ar, this message translates to:
  /// **'أرباح اليوم'**
  String get statEarningsToday;

  /// No description provided for @statTrips.
  ///
  /// In ar, this message translates to:
  /// **'الرحلات'**
  String get statTrips;

  /// No description provided for @statTripsMeta.
  ///
  /// In ar, this message translates to:
  /// **'رحلة مكتملة'**
  String get statTripsMeta;

  /// No description provided for @statHours.
  ///
  /// In ar, this message translates to:
  /// **'ساعات العمل'**
  String get statHours;

  /// No description provided for @statHoursMeta.
  ///
  /// In ar, this message translates to:
  /// **'ساعة اليوم'**
  String get statHoursMeta;

  /// No description provided for @statRating.
  ///
  /// In ar, this message translates to:
  /// **'التقييم'**
  String get statRating;

  /// No description provided for @statRatingMeta.
  ///
  /// In ar, this message translates to:
  /// **'من 5.0'**
  String get statRatingMeta;

  /// No description provided for @viewAll.
  ///
  /// In ar, this message translates to:
  /// **'عرض الكل'**
  String get viewAll;

  /// No description provided for @driverTripsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد رحلات بعد، فعّل حالتك لاستقبال الطلبات.'**
  String get driverTripsEmpty;

  /// No description provided for @thisWeek.
  ///
  /// In ar, this message translates to:
  /// **'هذا الأسبوع'**
  String get thisWeek;

  /// No description provided for @totalEarnings.
  ///
  /// In ar, this message translates to:
  /// **'إجمالي الأرباح'**
  String get totalEarnings;

  /// No description provided for @weeklyTarget.
  ///
  /// In ar, this message translates to:
  /// **'هدف الأسبوع'**
  String get weeklyTarget;

  /// No description provided for @transferEarnings.
  ///
  /// In ar, this message translates to:
  /// **'تحويل الأرباح'**
  String get transferEarnings;

  /// No description provided for @documents.
  ///
  /// In ar, this message translates to:
  /// **'المستندات'**
  String get documents;

  /// No description provided for @accountVerified.
  ///
  /// In ar, this message translates to:
  /// **'الحساب موثّق'**
  String get accountVerified;

  /// No description provided for @expiresOn.
  ///
  /// In ar, this message translates to:
  /// **'تنتهي في {date}'**
  String expiresOn(String date);

  /// No description provided for @noExpiry.
  ///
  /// In ar, this message translates to:
  /// **'بدون تاريخ انتهاء'**
  String get noExpiry;

  /// No description provided for @docVerified.
  ///
  /// In ar, this message translates to:
  /// **'تم التحقق'**
  String get docVerified;

  /// No description provided for @docExpiringSoon.
  ///
  /// In ar, this message translates to:
  /// **'تحديث قريباً'**
  String get docExpiringSoon;

  /// No description provided for @docExpired.
  ///
  /// In ar, this message translates to:
  /// **'منتهي'**
  String get docExpired;

  /// No description provided for @documentsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لم يتم رفع أي مستند بعد'**
  String get documentsEmpty;

  /// No description provided for @plateNumber.
  ///
  /// In ar, this message translates to:
  /// **'رقم اللوحة'**
  String get plateNumber;

  /// No description provided for @vehicleMeta.
  ///
  /// In ar, this message translates to:
  /// **'{color} · موديل {year}'**
  String vehicleMeta(String color, String year);

  /// No description provided for @updateVehicle.
  ///
  /// In ar, this message translates to:
  /// **'تحديث بيانات المركبة'**
  String get updateVehicle;

  /// No description provided for @noVehicle.
  ///
  /// In ar, this message translates to:
  /// **'لم تتم إضافة مركبة بعد'**
  String get noVehicle;

  /// No description provided for @driverPersonalCopy.
  ///
  /// In ar, this message translates to:
  /// **'الاسم، الجوال والصورة الشخصية'**
  String get driverPersonalCopy;

  /// No description provided for @driverBank.
  ///
  /// In ar, this message translates to:
  /// **'الحساب البنكي'**
  String get driverBank;

  /// No description provided for @driverBankCopy.
  ///
  /// In ar, this message translates to:
  /// **'إدارة الآيبان وتحويل الأرباح'**
  String get driverBankCopy;

  /// No description provided for @driverTripSettings.
  ///
  /// In ar, this message translates to:
  /// **'إعدادات الرحلات'**
  String get driverTripSettings;

  /// No description provided for @driverTripSettingsCopy.
  ///
  /// In ar, this message translates to:
  /// **'نطاق العمل وتفضيلات الطلبات'**
  String get driverTripSettingsCopy;

  /// No description provided for @driverNotifCopy.
  ///
  /// In ar, this message translates to:
  /// **'تنبيهات الرحلات والأرباح'**
  String get driverNotifCopy;

  /// No description provided for @driverSupport.
  ///
  /// In ar, this message translates to:
  /// **'المساعدة والدعم'**
  String get driverSupport;

  /// No description provided for @driverSupportCopy.
  ///
  /// In ar, this message translates to:
  /// **'تواصل مع فريق دعم السائقين'**
  String get driverSupportCopy;

  /// No description provided for @driverNotApproved.
  ///
  /// In ar, this message translates to:
  /// **'لم يتم اعتماد حسابك بعد'**
  String get driverNotApproved;

  /// No description provided for @tripEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'رحلتك الحالية'**
  String get tripEyebrow;

  /// No description provided for @tripNumberLabel.
  ///
  /// In ar, this message translates to:
  /// **'رحلة {number}'**
  String tripNumberLabel(String number);

  /// No description provided for @etaChip.
  ///
  /// In ar, this message translates to:
  /// **'يصل خلال {eta}'**
  String etaChip(String eta);

  /// No description provided for @etaUnknown.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ حساب وقت الوصول'**
  String get etaUnknown;

  /// No description provided for @driverAssignedTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم تعيين كابتن لرحلتك'**
  String get driverAssignedTitle;

  /// No description provided for @driverEnRouteTitle.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن في طريقه إليك'**
  String get driverEnRouteTitle;

  /// No description provided for @driverArrivedTitle.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن وصل'**
  String get driverArrivedTitle;

  /// No description provided for @waitingCopy.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن بانتظارك عند نقطة الالتقاط'**
  String get waitingCopy;

  /// No description provided for @waitingTimerLabel.
  ///
  /// In ar, this message translates to:
  /// **'مدة الانتظار'**
  String get waitingTimerLabel;

  /// No description provided for @pinTitle.
  ///
  /// In ar, this message translates to:
  /// **'رمز بدء الرحلة'**
  String get pinTitle;

  /// No description provided for @pinCopy.
  ///
  /// In ar, this message translates to:
  /// **'أخبر الكابتن بهذا الرمز عند صعودك'**
  String get pinCopy;

  /// No description provided for @callDriver.
  ///
  /// In ar, this message translates to:
  /// **'اتصال'**
  String get callDriver;

  /// No description provided for @shareTrip.
  ///
  /// In ar, this message translates to:
  /// **'مشاركة'**
  String get shareTrip;

  /// No description provided for @shareTripText.
  ///
  /// In ar, this message translates to:
  /// **'رحلتي مع ATA رقم {number}. الكابتن: {driver}، المركبة: {vehicle} ({plate}).'**
  String shareTripText(
    String number,
    String driver,
    String vehicle,
    String plate,
  );

  /// No description provided for @ratingValue.
  ///
  /// In ar, this message translates to:
  /// **'تقييم {rating}'**
  String ratingValue(String rating);

  /// No description provided for @vehicleLine.
  ///
  /// In ar, this message translates to:
  /// **'{make} {model} · {color}'**
  String vehicleLine(String make, String model, String color);

  /// No description provided for @inTripTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلتك جارية'**
  String get inTripTitle;

  /// No description provided for @inTripCopy.
  ///
  /// In ar, this message translates to:
  /// **'استرخِ، سنعلمك عند الوصول إلى وجهتك.'**
  String get inTripCopy;

  /// No description provided for @readyToStartTitle.
  ///
  /// In ar, this message translates to:
  /// **'جاهز للانطلاق'**
  String get readyToStartTitle;

  /// No description provided for @readyToStartCopy.
  ///
  /// In ar, this message translates to:
  /// **'تم التحقق من الرمز، ستنطلق الرحلة الآن.'**
  String get readyToStartCopy;

  /// No description provided for @receiptTitle.
  ///
  /// In ar, this message translates to:
  /// **'وصلت بسلامة'**
  String get receiptTitle;

  /// No description provided for @receiptCopy.
  ///
  /// In ar, this message translates to:
  /// **'شكراً لاستخدامك ATA، إليك ملخص رحلتك.'**
  String get receiptCopy;

  /// No description provided for @receiptFare.
  ///
  /// In ar, this message translates to:
  /// **'الأجرة'**
  String get receiptFare;

  /// No description provided for @receiptDistance.
  ///
  /// In ar, this message translates to:
  /// **'المسافة'**
  String get receiptDistance;

  /// No description provided for @receiptDuration.
  ///
  /// In ar, this message translates to:
  /// **'المدة'**
  String get receiptDuration;

  /// No description provided for @receiptPayment.
  ///
  /// In ar, this message translates to:
  /// **'طريقة الدفع'**
  String get receiptPayment;

  /// No description provided for @kmValue.
  ///
  /// In ar, this message translates to:
  /// **'{km} كم'**
  String kmValue(String km);

  /// No description provided for @metersValue.
  ///
  /// In ar, this message translates to:
  /// **'{meters} م'**
  String metersValue(String meters);

  /// No description provided for @rateTrip.
  ///
  /// In ar, this message translates to:
  /// **'قيّم الرحلة'**
  String get rateTrip;

  /// No description provided for @done.
  ///
  /// In ar, this message translates to:
  /// **'تم'**
  String get done;

  /// No description provided for @cancelledTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم إلغاء الرحلة'**
  String get cancelledTitle;

  /// No description provided for @cancelledCopy.
  ///
  /// In ar, this message translates to:
  /// **'يمكنك طلب رحلة جديدة في أي وقت.'**
  String get cancelledCopy;

  /// No description provided for @noDriversTitle.
  ///
  /// In ar, this message translates to:
  /// **'لم نجد كابتن قريباً'**
  String get noDriversTitle;

  /// No description provided for @noDriversCopy.
  ///
  /// In ar, this message translates to:
  /// **'كل الكباتن مشغولون حالياً، حاول مرة أخرى بعد قليل.'**
  String get noDriversCopy;

  /// No description provided for @retryRequest.
  ///
  /// In ar, this message translates to:
  /// **'طلب رحلة جديدة'**
  String get retryRequest;

  /// No description provided for @cancelTrip.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الرحلة'**
  String get cancelTrip;

  /// No description provided for @cancelReasonTitle.
  ///
  /// In ar, this message translates to:
  /// **'لماذا تريد الإلغاء؟'**
  String get cancelReasonTitle;

  /// No description provided for @cancelReasonCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر السبب لإلغاء الرحلة مباشرة.'**
  String get cancelReasonCopy;

  /// No description provided for @reasonChangedMind.
  ///
  /// In ar, this message translates to:
  /// **'غيّرت رأيي'**
  String get reasonChangedMind;

  /// No description provided for @reasonDriverLate.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن تأخر'**
  String get reasonDriverLate;

  /// No description provided for @reasonWrongPickup.
  ///
  /// In ar, this message translates to:
  /// **'موقع الانطلاق غير صحيح'**
  String get reasonWrongPickup;

  /// No description provided for @reasonOther.
  ///
  /// In ar, this message translates to:
  /// **'سبب آخر'**
  String get reasonOther;

  /// No description provided for @keepTrip.
  ///
  /// In ar, this message translates to:
  /// **'الاستمرار في الرحلة'**
  String get keepTrip;

  /// No description provided for @offeredPriceLabel.
  ///
  /// In ar, this message translates to:
  /// **'اقتراح سعر'**
  String get offeredPriceLabel;

  /// No description provided for @offeredPriceHint.
  ///
  /// In ar, this message translates to:
  /// **'اختياري: اقترح السعر الذي يناسبك'**
  String get offeredPriceHint;

  /// No description provided for @offeredPriceActive.
  ///
  /// In ar, this message translates to:
  /// **'سعرك المقترح: {price}'**
  String offeredPriceActive(String price);

  /// No description provided for @offeredPriceClear.
  ///
  /// In ar, this message translates to:
  /// **'إزالة'**
  String get offeredPriceClear;

  /// No description provided for @tripActiveExists.
  ///
  /// In ar, this message translates to:
  /// **'لديك رحلة نشطة بالفعل'**
  String get tripActiveExists;

  /// No description provided for @offerExpiredError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية هذا العرض'**
  String get offerExpiredError;

  /// No description provided for @pinInvalid.
  ///
  /// In ar, this message translates to:
  /// **'الرمز غير صحيح، المحاولات المتبقية: {attempts}'**
  String pinInvalid(int attempts);

  /// No description provided for @pinLocked.
  ///
  /// In ar, this message translates to:
  /// **'تم قفل التحقق من الرمز، تواصل مع الدعم'**
  String get pinLocked;

  /// No description provided for @callFailed.
  ///
  /// In ar, this message translates to:
  /// **'تعذر بدء الاتصال'**
  String get callFailed;

  /// No description provided for @offerEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'طلب جديد'**
  String get offerEyebrow;

  /// No description provided for @offerTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلة جديدة بالقرب منك'**
  String get offerTitle;

  /// No description provided for @offerSecondsLeft.
  ///
  /// In ar, this message translates to:
  /// **'{seconds} ث'**
  String offerSecondsLeft(int seconds);

  /// No description provided for @offerDistanceToPickup.
  ///
  /// In ar, this message translates to:
  /// **'المسافة إليك'**
  String get offerDistanceToPickup;

  /// No description provided for @offerEta.
  ///
  /// In ar, this message translates to:
  /// **'الوصول للراكب'**
  String get offerEta;

  /// No description provided for @offerTripDistance.
  ///
  /// In ar, this message translates to:
  /// **'مسافة الرحلة'**
  String get offerTripDistance;

  /// No description provided for @offerPassengerPrice.
  ///
  /// In ar, this message translates to:
  /// **'سعر الراكب'**
  String get offerPassengerPrice;

  /// No description provided for @offerNetEarnings.
  ///
  /// In ar, this message translates to:
  /// **'صافي أرباحك'**
  String get offerNetEarnings;

  /// No description provided for @offerPassenger.
  ///
  /// In ar, this message translates to:
  /// **'الراكب'**
  String get offerPassenger;

  /// No description provided for @acceptOffer.
  ///
  /// In ar, this message translates to:
  /// **'قبول الرحلة'**
  String get acceptOffer;

  /// No description provided for @rejectOffer.
  ///
  /// In ar, this message translates to:
  /// **'رفض'**
  String get rejectOffer;

  /// No description provided for @offerExpiredTitle.
  ///
  /// In ar, this message translates to:
  /// **'انتهى العرض'**
  String get offerExpiredTitle;

  /// No description provided for @offerExpiredCopy.
  ///
  /// In ar, this message translates to:
  /// **'سيصلك الطلب التالي فور توفره.'**
  String get offerExpiredCopy;

  /// No description provided for @pickupTitle.
  ///
  /// In ar, this message translates to:
  /// **'نقطة الالتقاط'**
  String get pickupTitle;

  /// No description provided for @dropoffTitle.
  ///
  /// In ar, this message translates to:
  /// **'الوجهة'**
  String get dropoffTitle;

  /// No description provided for @stopN.
  ///
  /// In ar, this message translates to:
  /// **'محطة {n}'**
  String stopN(int n);

  /// No description provided for @driverTripEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'الرحلة الحالية'**
  String get driverTripEyebrow;

  /// No description provided for @actionEnRoute.
  ///
  /// In ar, this message translates to:
  /// **'انطلقت إلى الراكب'**
  String get actionEnRoute;

  /// No description provided for @actionArrived.
  ///
  /// In ar, this message translates to:
  /// **'وصلت'**
  String get actionArrived;

  /// No description provided for @actionStart.
  ///
  /// In ar, this message translates to:
  /// **'ابدأ الرحلة'**
  String get actionStart;

  /// No description provided for @actionComplete.
  ///
  /// In ar, this message translates to:
  /// **'إنهاء الرحلة'**
  String get actionComplete;

  /// No description provided for @enterPinTitle.
  ///
  /// In ar, this message translates to:
  /// **'أدخل رمز الراكب'**
  String get enterPinTitle;

  /// No description provided for @enterPinCopy.
  ///
  /// In ar, this message translates to:
  /// **'اطلب من الراكب رمز بدء الرحلة المكوّن من 4 أرقام'**
  String get enterPinCopy;

  /// No description provided for @verifyPinAction.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد الرمز'**
  String get verifyPinAction;

  /// No description provided for @stageDriverAssigned.
  ///
  /// In ar, this message translates to:
  /// **'تم قبول الرحلة'**
  String get stageDriverAssigned;

  /// No description provided for @stageEnRoute.
  ///
  /// In ar, this message translates to:
  /// **'في الطريق إلى الراكب'**
  String get stageEnRoute;

  /// No description provided for @stageArrived.
  ///
  /// In ar, this message translates to:
  /// **'وصلت إلى نقطة الالتقاط'**
  String get stageArrived;

  /// No description provided for @stageWaiting.
  ///
  /// In ar, this message translates to:
  /// **'بانتظار الراكب'**
  String get stageWaiting;

  /// No description provided for @stagePinVerified.
  ///
  /// In ar, this message translates to:
  /// **'جاهز للانطلاق'**
  String get stagePinVerified;

  /// No description provided for @stageInTrip.
  ///
  /// In ar, this message translates to:
  /// **'الرحلة جارية'**
  String get stageInTrip;

  /// No description provided for @stageCompleted.
  ///
  /// In ar, this message translates to:
  /// **'اكتملت الرحلة'**
  String get stageCompleted;

  /// No description provided for @callPassenger.
  ///
  /// In ar, this message translates to:
  /// **'اتصال بالراكب'**
  String get callPassenger;

  /// No description provided for @backToDashboard.
  ///
  /// In ar, this message translates to:
  /// **'العودة إلى لوحة السائق'**
  String get backToDashboard;

  /// No description provided for @earningsLine.
  ///
  /// In ar, this message translates to:
  /// **'أرباحك من الرحلة'**
  String get earningsLine;

  /// No description provided for @driverTripCompletedCopy.
  ///
  /// In ar, this message translates to:
  /// **'أحسنت! تمت إضافة أرباح الرحلة إلى محفظتك.'**
  String get driverTripCompletedCopy;

  /// No description provided for @driverTripCancelledCopy.
  ///
  /// In ar, this message translates to:
  /// **'تم إلغاء هذه الرحلة، ستصلك طلبات جديدة قريباً.'**
  String get driverTripCancelledCopy;

  /// No description provided for @locationDenied.
  ///
  /// In ar, this message translates to:
  /// **'يحتاج التطبيق إذن الموقع لاستقبال الرحلات'**
  String get locationDenied;

  /// No description provided for @locationDeniedForever.
  ///
  /// In ar, this message translates to:
  /// **'فعّل إذن الموقع لتطبيق ATA من إعدادات الجهاز'**
  String get locationDeniedForever;

  /// No description provided for @locationServiceDisabled.
  ///
  /// In ar, this message translates to:
  /// **'فعّل خدمة الموقع (GPS) لاستقبال الرحلات'**
  String get locationServiceDisabled;

  /// No description provided for @locationStreaming.
  ///
  /// In ar, this message translates to:
  /// **'موقعك يُشارك مع الركاب'**
  String get locationStreaming;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
