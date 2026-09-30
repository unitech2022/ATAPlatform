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
  /// **'نسخ'**
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

  /// No description provided for @offeredPriceMin.
  ///
  /// In ar, this message translates to:
  /// **'الحد الأدنى {price}'**
  String offeredPriceMin(String price);

  /// No description provided for @offeredPriceMax.
  ///
  /// In ar, this message translates to:
  /// **'الحد الأقصى {price}'**
  String offeredPriceMax(String price);

  /// No description provided for @offeredPriceDecrease.
  ///
  /// In ar, this message translates to:
  /// **'خفض السعر بريال'**
  String get offeredPriceDecrease;

  /// No description provided for @offeredPriceIncrease.
  ///
  /// In ar, this message translates to:
  /// **'رفع السعر بريال'**
  String get offeredPriceIncrease;

  /// No description provided for @offerOutOfRange.
  ///
  /// In ar, this message translates to:
  /// **'السعر المقترح خارج النطاق المسموح ({min} – {max} ر.س)، تم تعديله'**
  String offerOutOfRange(String min, String max);

  /// No description provided for @quoteExpiredError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية السعر وتم تحديثه، اضغط للتأكيد مرة أخرى'**
  String get quoteExpiredError;

  /// No description provided for @quoteLoading.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ حساب السعر…'**
  String get quoteLoading;

  /// No description provided for @quoteFailed.
  ///
  /// In ar, this message translates to:
  /// **'تعذر حساب السعر، الأسعار المعروضة تقديرية'**
  String get quoteFailed;

  /// No description provided for @quoteExpiredHint.
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية السعر'**
  String get quoteExpiredHint;

  /// No description provided for @refreshQuote.
  ///
  /// In ar, this message translates to:
  /// **'تحديث السعر'**
  String get refreshQuote;

  /// No description provided for @fareDetails.
  ///
  /// In ar, this message translates to:
  /// **'تفاصيل السعر'**
  String get fareDetails;

  /// No description provided for @fareDetailsCopy.
  ///
  /// In ar, this message translates to:
  /// **'كيف حُسب سعر {category}'**
  String fareDetailsCopy(String category);

  /// No description provided for @fareBaseFare.
  ///
  /// In ar, this message translates to:
  /// **'التعرفة الأساسية'**
  String get fareBaseFare;

  /// No description provided for @fareDistance.
  ///
  /// In ar, this message translates to:
  /// **'المسافة ({distance})'**
  String fareDistance(String distance);

  /// No description provided for @fareTime.
  ///
  /// In ar, this message translates to:
  /// **'الوقت ({duration})'**
  String fareTime(String duration);

  /// No description provided for @fareMinApplied.
  ///
  /// In ar, this message translates to:
  /// **'تم تطبيق الحد الأدنى للتعرفة'**
  String get fareMinApplied;

  /// No description provided for @fareTimeMultiplier.
  ///
  /// In ar, this message translates to:
  /// **'مضاعِف الوقت'**
  String get fareTimeMultiplier;

  /// No description provided for @fareDemandMultiplier.
  ///
  /// In ar, this message translates to:
  /// **'مضاعِف الطلب'**
  String get fareDemandMultiplier;

  /// No description provided for @fareBookingFee.
  ///
  /// In ar, this message translates to:
  /// **'رسوم الحجز'**
  String get fareBookingFee;

  /// No description provided for @fareServiceFee.
  ///
  /// In ar, this message translates to:
  /// **'رسوم الخدمة'**
  String get fareServiceFee;

  /// No description provided for @fareDiscount.
  ///
  /// In ar, this message translates to:
  /// **'الخصم'**
  String get fareDiscount;

  /// No description provided for @fareTotal.
  ///
  /// In ar, this message translates to:
  /// **'الإجمالي'**
  String get fareTotal;

  /// No description provided for @multiplierValue.
  ///
  /// In ar, this message translates to:
  /// **'×{value}'**
  String multiplierValue(String value);

  /// No description provided for @demandBadge.
  ///
  /// In ar, this message translates to:
  /// **'{name} ×{multiplier}'**
  String demandBadge(String name, String multiplier);

  /// No description provided for @demandNormal.
  ///
  /// In ar, this message translates to:
  /// **'الطلب طبيعي'**
  String get demandNormal;

  /// No description provided for @demandModerate.
  ///
  /// In ar, this message translates to:
  /// **'الطلب متوسط الآن'**
  String get demandModerate;

  /// No description provided for @demandHigh.
  ///
  /// In ar, this message translates to:
  /// **'الطلب مرتفع الآن'**
  String get demandHigh;

  /// No description provided for @demandVeryHigh.
  ///
  /// In ar, this message translates to:
  /// **'الطلب مرتفع جداً الآن'**
  String get demandVeryHigh;

  /// No description provided for @offerPassengerOffered.
  ///
  /// In ar, this message translates to:
  /// **'اقتراح من الراكب'**
  String get offerPassengerOffered;

  /// No description provided for @offerRound.
  ///
  /// In ar, this message translates to:
  /// **'الجولة {round}'**
  String offerRound(int round);

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

  /// No description provided for @noDriversNearby.
  ///
  /// In ar, this message translates to:
  /// **'لا يوجد كباتن قريبون الآن'**
  String get noDriversNearby;

  /// No description provided for @cardBrandMada.
  ///
  /// In ar, this message translates to:
  /// **'مدى'**
  String get cardBrandMada;

  /// No description provided for @cardBrandVisa.
  ///
  /// In ar, this message translates to:
  /// **'فيزا'**
  String get cardBrandVisa;

  /// No description provided for @cardBrandMastercard.
  ///
  /// In ar, this message translates to:
  /// **'ماستركارد'**
  String get cardBrandMastercard;

  /// No description provided for @cardMasked.
  ///
  /// In ar, this message translates to:
  /// **'{brand} •••• {last4}'**
  String cardMasked(String brand, String last4);

  /// No description provided for @cardExpiry.
  ///
  /// In ar, this message translates to:
  /// **'تنتهي {expiry}'**
  String cardExpiry(String expiry);

  /// No description provided for @cardExpired.
  ///
  /// In ar, this message translates to:
  /// **'البطاقة منتهية الصلاحية'**
  String get cardExpired;

  /// No description provided for @cardPendingVerification.
  ///
  /// In ar, this message translates to:
  /// **'بانتظار التحقق من البنك'**
  String get cardPendingVerification;

  /// No description provided for @cardDefault.
  ///
  /// In ar, this message translates to:
  /// **'الافتراضية'**
  String get cardDefault;

  /// No description provided for @cardMakeDefault.
  ///
  /// In ar, this message translates to:
  /// **'تعيين كافتراضية'**
  String get cardMakeDefault;

  /// No description provided for @cardRemove.
  ///
  /// In ar, this message translates to:
  /// **'حذف'**
  String get cardRemove;

  /// No description provided for @cardRemoveTitle.
  ///
  /// In ar, this message translates to:
  /// **'حذف البطاقة؟'**
  String get cardRemoveTitle;

  /// No description provided for @cardRemoveCopy.
  ///
  /// In ar, this message translates to:
  /// **'سيتم حذف {card} من حسابك.'**
  String cardRemoveCopy(String card);

  /// No description provided for @savedCardsTitle.
  ///
  /// In ar, this message translates to:
  /// **'بطاقاتي'**
  String get savedCardsTitle;

  /// No description provided for @savedCardsCopy.
  ///
  /// In ar, this message translates to:
  /// **'بطاقات الدفع المحفوظة للرحلات وشحن المحفظة.'**
  String get savedCardsCopy;

  /// No description provided for @savedCardsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد بطاقات محفوظة بعد'**
  String get savedCardsEmpty;

  /// No description provided for @manageCards.
  ///
  /// In ar, this message translates to:
  /// **'إدارة البطاقات'**
  String get manageCards;

  /// No description provided for @addCard.
  ///
  /// In ar, this message translates to:
  /// **'إضافة بطاقة'**
  String get addCard;

  /// No description provided for @addCardTitle.
  ///
  /// In ar, this message translates to:
  /// **'إضافة بطاقة جديدة'**
  String get addCardTitle;

  /// No description provided for @addCardCopy.
  ///
  /// In ar, this message translates to:
  /// **'يتم ترميز بطاقتك على جهازك، ولا يصل رقمها إلى خوادمنا.'**
  String get addCardCopy;

  /// No description provided for @cardNumberLabel.
  ///
  /// In ar, this message translates to:
  /// **'رقم البطاقة'**
  String get cardNumberLabel;

  /// No description provided for @cardNumberHint.
  ///
  /// In ar, this message translates to:
  /// **'0000 0000 0000 0000'**
  String get cardNumberHint;

  /// No description provided for @cardNumberError.
  ///
  /// In ar, this message translates to:
  /// **'رقم البطاقة غير صحيح'**
  String get cardNumberError;

  /// No description provided for @cardExpiryLabel.
  ///
  /// In ar, this message translates to:
  /// **'تاريخ الانتهاء'**
  String get cardExpiryLabel;

  /// No description provided for @cardExpiryHint.
  ///
  /// In ar, this message translates to:
  /// **'MM/YY'**
  String get cardExpiryHint;

  /// No description provided for @cardExpiryError.
  ///
  /// In ar, this message translates to:
  /// **'تاريخ غير صالح'**
  String get cardExpiryError;

  /// No description provided for @cardCvcLabel.
  ///
  /// In ar, this message translates to:
  /// **'رمز الأمان'**
  String get cardCvcLabel;

  /// No description provided for @cardCvcHint.
  ///
  /// In ar, this message translates to:
  /// **'123'**
  String get cardCvcHint;

  /// No description provided for @cardCvcError.
  ///
  /// In ar, this message translates to:
  /// **'رمز غير صحيح'**
  String get cardCvcError;

  /// No description provided for @cardHolderLabel.
  ///
  /// In ar, this message translates to:
  /// **'اسم حامل البطاقة'**
  String get cardHolderLabel;

  /// No description provided for @cardHolderHint.
  ///
  /// In ar, this message translates to:
  /// **'كما يظهر على البطاقة'**
  String get cardHolderHint;

  /// No description provided for @cardSetDefault.
  ///
  /// In ar, this message translates to:
  /// **'استخدامها كبطاقة افتراضية'**
  String get cardSetDefault;

  /// No description provided for @saveCard.
  ///
  /// In ar, this message translates to:
  /// **'حفظ البطاقة'**
  String get saveCard;

  /// No description provided for @sandboxCardsHint.
  ///
  /// In ar, this message translates to:
  /// **'بيئة تجريبية: 4000 0000 0000 0002 مرفوضة، 4000 0000 0000 3220 تتطلب تحققاً، وبطاقة مدى التجريبية 4406 4700 0000 0007.'**
  String get sandboxCardsHint;

  /// No description provided for @paymentActionTitle.
  ///
  /// In ar, this message translates to:
  /// **'مطلوب تحقق من البنك'**
  String get paymentActionTitle;

  /// No description provided for @paymentActionCopy.
  ///
  /// In ar, this message translates to:
  /// **'أكمل التحقق في صفحة البنك، وسيتم تحديث الحالة تلقائياً بعد التأكيد.'**
  String get paymentActionCopy;

  /// No description provided for @paymentActionOpen.
  ///
  /// In ar, this message translates to:
  /// **'فتح صفحة التحقق'**
  String get paymentActionOpen;

  /// No description provided for @topUpSource.
  ///
  /// In ar, this message translates to:
  /// **'طريقة الدفع'**
  String get topUpSource;

  /// No description provided for @topUpToContinue.
  ///
  /// In ar, this message translates to:
  /// **'اشحن المحفظة للمتابعة'**
  String get topUpToContinue;

  /// No description provided for @outstandingBalanceTitle.
  ///
  /// In ar, this message translates to:
  /// **'يوجد مبلغ مستحق على حسابك'**
  String get outstandingBalanceTitle;

  /// No description provided for @outstandingBalanceCopy.
  ///
  /// In ar, this message translates to:
  /// **'رصيد محفظتك سالب. اشحن المحفظة لتتمكن من طلب رحلات جديدة.'**
  String get outstandingBalanceCopy;

  /// No description provided for @outstandingBalanceError.
  ///
  /// In ar, this message translates to:
  /// **'يوجد مبلغ مستحق {amount} ر.س على حسابك، اشحن المحفظة للمتابعة'**
  String outstandingBalanceError(String amount);

  /// No description provided for @paymentFailedError.
  ///
  /// In ar, this message translates to:
  /// **'تعذّر إتمام الدفع، جرّب بطاقة أخرى أو ادفع نقداً'**
  String get paymentFailedError;

  /// No description provided for @paymentMethodExpiredError.
  ///
  /// In ar, this message translates to:
  /// **'البطاقة منتهية الصلاحية'**
  String get paymentMethodExpiredError;

  /// No description provided for @paymentMethodInUseError.
  ///
  /// In ar, this message translates to:
  /// **'البطاقة مرتبطة برحلة جارية'**
  String get paymentMethodInUseError;

  /// No description provided for @paymentProviderUnavailableError.
  ///
  /// In ar, this message translates to:
  /// **'خدمة الدفع غير متاحة حالياً، حاول لاحقاً'**
  String get paymentProviderUnavailableError;

  /// No description provided for @paymentFallbackCash.
  ///
  /// In ar, this message translates to:
  /// **'تعذّر الدفع بالبطاقة، فتم تحويل الرحلة إلى الدفع نقداً'**
  String get paymentFallbackCash;

  /// No description provided for @collectCashLine.
  ///
  /// In ar, this message translates to:
  /// **'حصّل نقداً من الراكب: {amount}'**
  String collectCashLine(String amount);

  /// No description provided for @viewReceipt.
  ///
  /// In ar, this message translates to:
  /// **'عرض الإيصال'**
  String get viewReceipt;

  /// No description provided for @receiptEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'إيصال الرحلة'**
  String get receiptEyebrow;

  /// No description provided for @receiptBreakdown.
  ///
  /// In ar, this message translates to:
  /// **'تفاصيل الأجرة'**
  String get receiptBreakdown;

  /// No description provided for @receiptSubtotal.
  ///
  /// In ar, this message translates to:
  /// **'المجموع الفرعي'**
  String get receiptSubtotal;

  /// No description provided for @receiptDiscountTotal.
  ///
  /// In ar, this message translates to:
  /// **'إجمالي الخصم'**
  String get receiptDiscountTotal;

  /// No description provided for @receiptVat.
  ///
  /// In ar, this message translates to:
  /// **'شامل ضريبة القيمة المضافة {rate}٪: {amount}'**
  String receiptVat(String rate, String amount);

  /// No description provided for @receiptPaid.
  ///
  /// In ar, this message translates to:
  /// **'المبلغ المدفوع'**
  String get receiptPaid;

  /// No description provided for @receiptRefunded.
  ///
  /// In ar, this message translates to:
  /// **'المسترد'**
  String get receiptRefunded;

  /// No description provided for @receiptNetPaid.
  ///
  /// In ar, this message translates to:
  /// **'صافي المدفوع'**
  String get receiptNetPaid;

  /// No description provided for @receiptUnavailable.
  ///
  /// In ar, this message translates to:
  /// **'لا يتوفر إيصال لهذه الرحلة'**
  String get receiptUnavailable;

  /// No description provided for @discountSourcePromotion.
  ///
  /// In ar, this message translates to:
  /// **'عرض ترويجي'**
  String get discountSourcePromotion;

  /// No description provided for @discountSourceFavoriteDriver.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن المفضل'**
  String get discountSourceFavoriteDriver;

  /// No description provided for @cashDebtLimitError.
  ///
  /// In ar, this message translates to:
  /// **'تجاوزت مستحقات النقد الحد المسموح، سدّدها للاتصال'**
  String get cashDebtLimitError;

  /// No description provided for @cantGoOnlineTitle.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكنك الاتصال الآن'**
  String get cantGoOnlineTitle;

  /// No description provided for @cashDebtTitle.
  ///
  /// In ar, this message translates to:
  /// **'مستحقات النقد'**
  String get cashDebtTitle;

  /// No description provided for @cashDebtCopy.
  ///
  /// In ar, this message translates to:
  /// **'أجور الرحلات النقدية المستحقة للمنصة. سدّدها قبل الوصول إلى الحد.'**
  String get cashDebtCopy;

  /// No description provided for @cashDebtLimit.
  ///
  /// In ar, this message translates to:
  /// **'الحد المسموح: {limit}'**
  String cashDebtLimit(String limit);

  /// No description provided for @settleDebt.
  ///
  /// In ar, this message translates to:
  /// **'سداد'**
  String get settleDebt;

  /// No description provided for @settleDebtTitle.
  ///
  /// In ar, this message translates to:
  /// **'سداد مستحقات النقد'**
  String get settleDebtTitle;

  /// No description provided for @settleDebtCopy.
  ///
  /// In ar, this message translates to:
  /// **'اشحن محفظة الكابتن لتسديد مستحقات الرحلات النقدية.'**
  String get settleDebtCopy;

  /// No description provided for @driverWalletEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'المحفظة والأرباح'**
  String get driverWalletEyebrow;

  /// No description provided for @earningsStatementTitle.
  ///
  /// In ar, this message translates to:
  /// **'كشف الأرباح'**
  String get earningsStatementTitle;

  /// No description provided for @earningsStatementCopy.
  ///
  /// In ar, this message translates to:
  /// **'أرباحك وعمولة المنصة والنقد المحصّل للفترة.'**
  String get earningsStatementCopy;

  /// No description provided for @periodToday.
  ///
  /// In ar, this message translates to:
  /// **'اليوم'**
  String get periodToday;

  /// No description provided for @periodWeek.
  ///
  /// In ar, this message translates to:
  /// **'الأسبوع'**
  String get periodWeek;

  /// No description provided for @periodMonth.
  ///
  /// In ar, this message translates to:
  /// **'الشهر'**
  String get periodMonth;

  /// No description provided for @statementNet.
  ///
  /// In ar, this message translates to:
  /// **'الصافي'**
  String get statementNet;

  /// No description provided for @statementTrips.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا رحلات} =1{رحلة واحدة} =2{رحلتان} few{{count} رحلات} other{{count} رحلة}}'**
  String statementTrips(int count);

  /// No description provided for @statementGross.
  ///
  /// In ar, this message translates to:
  /// **'إجمالي الأجور'**
  String get statementGross;

  /// No description provided for @statementCommission.
  ///
  /// In ar, this message translates to:
  /// **'عمولة المنصة'**
  String get statementCommission;

  /// No description provided for @statementEarnings.
  ///
  /// In ar, this message translates to:
  /// **'أرباحك'**
  String get statementEarnings;

  /// No description provided for @statementIncentives.
  ///
  /// In ar, this message translates to:
  /// **'الحوافز'**
  String get statementIncentives;

  /// No description provided for @statementCompensation.
  ///
  /// In ar, this message translates to:
  /// **'تعويضات الإلغاء'**
  String get statementCompensation;

  /// No description provided for @statementAdjustments.
  ///
  /// In ar, this message translates to:
  /// **'التسويات'**
  String get statementAdjustments;

  /// No description provided for @statementCashCollected.
  ///
  /// In ar, this message translates to:
  /// **'النقد المحصّل'**
  String get statementCashCollected;

  /// No description provided for @statementPayouts.
  ///
  /// In ar, this message translates to:
  /// **'التحويلات'**
  String get statementPayouts;

  /// No description provided for @statementDaily.
  ///
  /// In ar, this message translates to:
  /// **'حسب اليوم'**
  String get statementDaily;

  /// No description provided for @payoutsTitle.
  ///
  /// In ar, this message translates to:
  /// **'تحويل الأرباح'**
  String get payoutsTitle;

  /// No description provided for @payoutsCopy.
  ///
  /// In ar, this message translates to:
  /// **'اطلب تحويل رصيدك إلى حسابك البنكي وتابع الحالة.'**
  String get payoutsCopy;

  /// No description provided for @payoutsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد طلبات تحويل بعد'**
  String get payoutsEmpty;

  /// No description provided for @payoutHistory.
  ///
  /// In ar, this message translates to:
  /// **'سجل التحويلات'**
  String get payoutHistory;

  /// No description provided for @requestPayout.
  ///
  /// In ar, this message translates to:
  /// **'طلب تحويل'**
  String get requestPayout;

  /// No description provided for @payoutRequestCopy.
  ///
  /// In ar, this message translates to:
  /// **'يُحوَّل المبلغ إلى الآيبان المسجل بعد اعتماد الطلب.'**
  String get payoutRequestCopy;

  /// No description provided for @payoutAvailable.
  ///
  /// In ar, this message translates to:
  /// **'المتاح للتحويل'**
  String get payoutAvailable;

  /// No description provided for @payoutAvailableLine.
  ///
  /// In ar, this message translates to:
  /// **'المتاح للتحويل: {amount}'**
  String payoutAvailableLine(String amount);

  /// No description provided for @payoutIban.
  ///
  /// In ar, this message translates to:
  /// **'الآيبان'**
  String get payoutIban;

  /// No description provided for @payoutIbanMissing.
  ///
  /// In ar, this message translates to:
  /// **'غير مضاف'**
  String get payoutIbanMissing;

  /// No description provided for @payoutAmountLabel.
  ///
  /// In ar, this message translates to:
  /// **'المبلغ'**
  String get payoutAmountLabel;

  /// No description provided for @payoutAmountInvalid.
  ///
  /// In ar, this message translates to:
  /// **'أدخل مبلغاً صحيحاً'**
  String get payoutAmountInvalid;

  /// No description provided for @payoutMinimumHint.
  ///
  /// In ar, this message translates to:
  /// **'الحد الأدنى للتحويل {amount}'**
  String payoutMinimumHint(String amount);

  /// No description provided for @confirmPayout.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد تحويل {amount}'**
  String confirmPayout(String amount);

  /// No description provided for @payoutRequestedTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم إرسال طلب التحويل'**
  String get payoutRequestedTitle;

  /// No description provided for @payoutRequestedCopy.
  ///
  /// In ar, this message translates to:
  /// **'سنبلغك عند اعتماد الطلب وتحويل المبلغ.'**
  String get payoutRequestedCopy;

  /// No description provided for @payoutUnavailable.
  ///
  /// In ar, this message translates to:
  /// **'طلب التحويل غير متاح حالياً'**
  String get payoutUnavailable;

  /// No description provided for @payoutRequested.
  ///
  /// In ar, this message translates to:
  /// **'قيد المراجعة'**
  String get payoutRequested;

  /// No description provided for @payoutApproved.
  ///
  /// In ar, this message translates to:
  /// **'معتمد'**
  String get payoutApproved;

  /// No description provided for @payoutPaid.
  ///
  /// In ar, this message translates to:
  /// **'مدفوع'**
  String get payoutPaid;

  /// No description provided for @payoutRejected.
  ///
  /// In ar, this message translates to:
  /// **'مرفوض'**
  String get payoutRejected;

  /// No description provided for @payoutCancelled.
  ///
  /// In ar, this message translates to:
  /// **'ملغى'**
  String get payoutCancelled;

  /// No description provided for @payoutRejectedReason.
  ///
  /// In ar, this message translates to:
  /// **'السبب: {reason}'**
  String payoutRejectedReason(String reason);

  /// No description provided for @payoutBelowMinimumError.
  ///
  /// In ar, this message translates to:
  /// **'المبلغ أقل من الحد الأدنى للسحب ({amount} ر.س)'**
  String payoutBelowMinimumError(String amount);

  /// No description provided for @payoutBelowMinimumReason.
  ///
  /// In ar, this message translates to:
  /// **'رصيدك أقل من الحد الأدنى للسحب'**
  String get payoutBelowMinimumReason;

  /// No description provided for @payoutCashDebtReason.
  ///
  /// In ar, this message translates to:
  /// **'سدّد مستحقات النقد أولاً'**
  String get payoutCashDebtReason;

  /// No description provided for @payoutPendingExistsError.
  ///
  /// In ar, this message translates to:
  /// **'لديك طلب سحب قيد المعالجة'**
  String get payoutPendingExistsError;

  /// No description provided for @ibanMissingError.
  ///
  /// In ar, this message translates to:
  /// **'أضف رقم الآيبان أولاً'**
  String get ibanMissingError;

  /// No description provided for @insufficientBalanceError.
  ///
  /// In ar, this message translates to:
  /// **'المبلغ أكبر من الرصيد المتاح'**
  String get insufficientBalanceError;

  /// No description provided for @shareNotFoundError.
  ///
  /// In ar, this message translates to:
  /// **'رابط التتبع غير موجود'**
  String get shareNotFoundError;

  /// No description provided for @shareExpiredError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية رابط التتبع'**
  String get shareExpiredError;

  /// No description provided for @trustedContactsLimitError.
  ///
  /// In ar, this message translates to:
  /// **'الحد الأقصى 5 جهات موثوقة'**
  String get trustedContactsLimitError;

  /// No description provided for @trustedContactExistsError.
  ///
  /// In ar, this message translates to:
  /// **'الجهة مضافة مسبقاً'**
  String get trustedContactExistsError;

  /// No description provided for @chatClosedError.
  ///
  /// In ar, this message translates to:
  /// **'المحادثة مغلقة لهذه الرحلة'**
  String get chatClosedError;

  /// No description provided for @lostItemWindowClosedError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت مدة الإبلاغ عن المفقودات'**
  String get lostItemWindowClosedError;

  /// No description provided for @cancellationReasonInvalidError.
  ///
  /// In ar, this message translates to:
  /// **'سبب الإلغاء غير صالح'**
  String get cancellationReasonInvalidError;

  /// No description provided for @cancellationFeeChangedError.
  ///
  /// In ar, this message translates to:
  /// **'تغيّرت رسوم الإلغاء، راجعها وأعد المحاولة'**
  String get cancellationFeeChangedError;

  /// No description provided for @noShowTooEarlyError.
  ///
  /// In ar, this message translates to:
  /// **'لم تنتهِ مدة الانتظار المطلوبة بعد (متبقٍ {minutes} د)'**
  String noShowTooEarlyError(int minutes);

  /// No description provided for @accountRestrictedError.
  ///
  /// In ar, this message translates to:
  /// **'حسابك مقيّد مؤقتاً بسبب تكرار الإلغاء'**
  String get accountRestrictedError;

  /// No description provided for @accountRestrictedUntil.
  ///
  /// In ar, this message translates to:
  /// **'حسابك مقيّد مؤقتاً بسبب تكرار الإلغاء حتى {date}'**
  String accountRestrictedUntil(String date);

  /// No description provided for @cancelNoteHint.
  ///
  /// In ar, this message translates to:
  /// **'اكتب سبب الإلغاء'**
  String get cancelNoteHint;

  /// No description provided for @cancelNoteRequired.
  ///
  /// In ar, this message translates to:
  /// **'هذا السبب يتطلب ملاحظة'**
  String get cancelNoteRequired;

  /// No description provided for @confirmCancel.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد الإلغاء'**
  String get confirmCancel;

  /// No description provided for @reasonEmergencyBadge.
  ///
  /// In ar, this message translates to:
  /// **'طارئ'**
  String get reasonEmergencyBadge;

  /// No description provided for @reasonExcusableBadge.
  ///
  /// In ar, this message translates to:
  /// **'يخضع للمراجعة'**
  String get reasonExcusableBadge;

  /// No description provided for @cancelEmergencyNote.
  ///
  /// In ar, this message translates to:
  /// **'سيُنشأ بلاغ سلامة ويتواصل معك فريق السلامة، ولن تُحتسب رسوم أو نقاط حتى المراجعة.'**
  String get cancelEmergencyNote;

  /// No description provided for @cancelExcusableNote.
  ///
  /// In ar, this message translates to:
  /// **'سيراجع فريق العمليات هذا العذر، ولن تُخصم رسوم أو نقاط حتى تتم المراجعة.'**
  String get cancelExcusableNote;

  /// No description provided for @cancelFree.
  ///
  /// In ar, this message translates to:
  /// **'مجاني'**
  String get cancelFree;

  /// No description provided for @penaltyPointsValue.
  ///
  /// In ar, this message translates to:
  /// **'+{count} نقاط'**
  String penaltyPointsValue(int count);

  /// No description provided for @scheduledCancelFeeLabel.
  ///
  /// In ar, this message translates to:
  /// **'رسوم إلغاء الحجز المجدول'**
  String get scheduledCancelFeeLabel;

  /// No description provided for @cancelPointsLabel.
  ///
  /// In ar, this message translates to:
  /// **'أثر الإلغاء على موثوقيتك'**
  String get cancelPointsLabel;

  /// No description provided for @cancelFeeLabel.
  ///
  /// In ar, this message translates to:
  /// **'رسوم الإلغاء'**
  String get cancelFeeLabel;

  /// No description provided for @cancelFreeUntil.
  ///
  /// In ar, this message translates to:
  /// **'الإلغاء مجاني حتى {time}'**
  String cancelFreeUntil(String time);

  /// No description provided for @cancelRequiresReview.
  ///
  /// In ar, this message translates to:
  /// **'الرسوم محتملة وتُحسم بعد مراجعة العذر'**
  String get cancelRequiresReview;

  /// No description provided for @noShowAvailableIn.
  ///
  /// In ar, this message translates to:
  /// **'يمكنك تسجيل عدم حضور الراكب بعد {time}'**
  String noShowAvailableIn(String time);

  /// No description provided for @noShowAvailableNow.
  ///
  /// In ar, this message translates to:
  /// **'انتهت مدة الانتظار، يمكنك تسجيل عدم حضور الراكب'**
  String get noShowAvailableNow;

  /// No description provided for @noShowAction.
  ///
  /// In ar, this message translates to:
  /// **'لم يحضر الراكب'**
  String get noShowAction;

  /// No description provided for @noShowConfirmTitle.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد عدم حضور الراكب؟'**
  String get noShowConfirmTitle;

  /// No description provided for @noShowConfirmCopy.
  ///
  /// In ar, this message translates to:
  /// **'سيتم إلغاء الرحلة واحتساب رسوم عدم الحضور على الراكب مع تعويضك حسب السياسة.'**
  String get noShowConfirmCopy;

  /// No description provided for @keepWaiting.
  ///
  /// In ar, this message translates to:
  /// **'متابعة الانتظار'**
  String get keepWaiting;

  /// No description provided for @noShowRecorded.
  ///
  /// In ar, this message translates to:
  /// **'تم تسجيل عدم حضور الراكب وإلغاء الرحلة.'**
  String get noShowRecorded;

  /// No description provided for @compensationLine.
  ///
  /// In ar, this message translates to:
  /// **'تعويضك: {amount}'**
  String compensationLine(String amount);

  /// No description provided for @cancelFeePendingReview.
  ///
  /// In ar, this message translates to:
  /// **'رسوم الإلغاء قيد مراجعة فريق العمليات.'**
  String get cancelFeePendingReview;

  /// No description provided for @cancelFeeCharged.
  ///
  /// In ar, this message translates to:
  /// **'تم خصم {amount} رسوم إلغاء.'**
  String cancelFeeCharged(String amount);

  /// No description provided for @levelNone.
  ///
  /// In ar, this message translates to:
  /// **'ممتاز'**
  String get levelNone;

  /// No description provided for @levelWarning.
  ///
  /// In ar, this message translates to:
  /// **'تنبيه'**
  String get levelWarning;

  /// No description provided for @levelDeprioritized.
  ///
  /// In ar, this message translates to:
  /// **'أولوية أقل في المطابقة'**
  String get levelDeprioritized;

  /// No description provided for @levelIncentivesReduced.
  ///
  /// In ar, this message translates to:
  /// **'مكافآت مخفضة'**
  String get levelIncentivesReduced;

  /// No description provided for @levelRestricted.
  ///
  /// In ar, this message translates to:
  /// **'مقيّد مؤقتاً'**
  String get levelRestricted;

  /// No description provided for @levelSuspended.
  ///
  /// In ar, this message translates to:
  /// **'موقوف'**
  String get levelSuspended;

  /// No description provided for @levelNoneCopy.
  ///
  /// In ar, this message translates to:
  /// **'سجلك جيد، استمر في إكمال رحلاتك.'**
  String get levelNoneCopy;

  /// No description provided for @levelWarningCopy.
  ///
  /// In ar, this message translates to:
  /// **'تكرار الإلغاء يرفع نقاطك وقد يؤدي إلى تقييد حسابك.'**
  String get levelWarningCopy;

  /// No description provided for @levelDeprioritizedCopy.
  ///
  /// In ar, this message translates to:
  /// **'ستصلك عروض أقل مؤقتاً بسبب ارتفاع نسبة الإلغاء.'**
  String get levelDeprioritizedCopy;

  /// No description provided for @levelIncentivesReducedCopy.
  ///
  /// In ar, this message translates to:
  /// **'تصلك عروض أقل وتُخفَّض مكافآتك حتى تتحسن موثوقيتك.'**
  String get levelIncentivesReducedCopy;

  /// No description provided for @levelRestrictedDriverCopy.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكنك الاتصال واستقبال الطلبات حتى انتهاء التقييد.'**
  String get levelRestrictedDriverCopy;

  /// No description provided for @levelRestrictedRiderCopy.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكنك طلب رحلات جديدة حتى انتهاء التقييد.'**
  String get levelRestrictedRiderCopy;

  /// No description provided for @levelSuspendedCopy.
  ///
  /// In ar, this message translates to:
  /// **'الحساب موقوف حتى يراجعه فريق العمليات.'**
  String get levelSuspendedCopy;

  /// No description provided for @excusePending.
  ///
  /// In ar, this message translates to:
  /// **'العذر قيد المراجعة'**
  String get excusePending;

  /// No description provided for @excuseApproved.
  ///
  /// In ar, this message translates to:
  /// **'تم قبول العذر'**
  String get excuseApproved;

  /// No description provided for @excuseRejected.
  ///
  /// In ar, this message translates to:
  /// **'تم رفض العذر'**
  String get excuseRejected;

  /// No description provided for @reliabilityTitle.
  ///
  /// In ar, this message translates to:
  /// **'موثوقيتك'**
  String get reliabilityTitle;

  /// No description provided for @reliabilityCopy.
  ///
  /// In ar, this message translates to:
  /// **'نسبة الإلغاء والنقاط وأثرها على حسابك.'**
  String get reliabilityCopy;

  /// No description provided for @cancellationRateLabel.
  ///
  /// In ar, this message translates to:
  /// **'نسبة الإلغاء'**
  String get cancellationRateLabel;

  /// No description provided for @penaltyPointsLabel.
  ///
  /// In ar, this message translates to:
  /// **'النقاط'**
  String get penaltyPointsLabel;

  /// No description provided for @acceptanceRateLabel.
  ///
  /// In ar, this message translates to:
  /// **'نسبة القبول'**
  String get acceptanceRateLabel;

  /// No description provided for @restrictedUntilLine.
  ///
  /// In ar, this message translates to:
  /// **'مقيّد حتى {date}'**
  String restrictedUntilLine(String date);

  /// No description provided for @reliabilityDetails.
  ///
  /// In ar, this message translates to:
  /// **'عرض التفاصيل'**
  String get reliabilityDetails;

  /// No description provided for @tripsAcceptedLabel.
  ///
  /// In ar, this message translates to:
  /// **'رحلات مُسندة'**
  String get tripsAcceptedLabel;

  /// No description provided for @tripsCompletedLabel.
  ///
  /// In ar, this message translates to:
  /// **'رحلات مكتملة'**
  String get tripsCompletedLabel;

  /// No description provided for @cancellationsAtFaultLabel.
  ///
  /// In ar, this message translates to:
  /// **'إلغاءات محتسبة'**
  String get cancellationsAtFaultLabel;

  /// No description provided for @reliabilityRateLabel.
  ///
  /// In ar, this message translates to:
  /// **'نسبة الإكمال'**
  String get reliabilityRateLabel;

  /// No description provided for @noShowCountLabel.
  ///
  /// In ar, this message translates to:
  /// **'عدم الحضور'**
  String get noShowCountLabel;

  /// No description provided for @matchingFactorLabel.
  ///
  /// In ar, this message translates to:
  /// **'معامل المطابقة'**
  String get matchingFactorLabel;

  /// No description provided for @incentiveMultiplierLabel.
  ///
  /// In ar, this message translates to:
  /// **'معامل المكافآت'**
  String get incentiveMultiplierLabel;

  /// No description provided for @reliabilityWindow.
  ///
  /// In ar, this message translates to:
  /// **'آخر {days} يوماً'**
  String reliabilityWindow(int days);

  /// No description provided for @nextLevelLine.
  ///
  /// In ar, this message translates to:
  /// **'المستوى التالي «{level}» عند {points} نقطة أو نسبة إلغاء {rate}'**
  String nextLevelLine(String level, String points, String rate);

  /// No description provided for @recentCancellations.
  ///
  /// In ar, this message translates to:
  /// **'آخر الإلغاءات'**
  String get recentCancellations;

  /// No description provided for @noRecentCancellations.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد إلغاءات محتسبة.'**
  String get noRecentCancellations;

  /// No description provided for @caseTypeSos.
  ///
  /// In ar, this message translates to:
  /// **'نداء طوارئ'**
  String get caseTypeSos;

  /// No description provided for @caseTypeReport.
  ///
  /// In ar, this message translates to:
  /// **'بلاغ سلامة'**
  String get caseTypeReport;

  /// No description provided for @alertUnexpectedStop.
  ///
  /// In ar, this message translates to:
  /// **'توقف غير متوقع'**
  String get alertUnexpectedStop;

  /// No description provided for @alertRouteDeviation.
  ///
  /// In ar, this message translates to:
  /// **'انحراف عن المسار'**
  String get alertRouteDeviation;

  /// No description provided for @alertTripOverrun.
  ///
  /// In ar, this message translates to:
  /// **'تأخر كبير في الرحلة'**
  String get alertTripOverrun;

  /// No description provided for @caseStatusOpen.
  ///
  /// In ar, this message translates to:
  /// **'مفتوحة'**
  String get caseStatusOpen;

  /// No description provided for @caseStatusInProgress.
  ///
  /// In ar, this message translates to:
  /// **'قيد المعالجة'**
  String get caseStatusInProgress;

  /// No description provided for @caseStatusEscalated.
  ///
  /// In ar, this message translates to:
  /// **'مُصعَّدة'**
  String get caseStatusEscalated;

  /// No description provided for @caseStatusResolved.
  ///
  /// In ar, this message translates to:
  /// **'مغلقة'**
  String get caseStatusResolved;

  /// No description provided for @alertUnexpectedStopCopy.
  ///
  /// In ar, this message translates to:
  /// **'لاحظنا أن الرحلة متوقفة منذ مدة في مكان غير متوقع.'**
  String get alertUnexpectedStopCopy;

  /// No description provided for @alertRouteDeviationCopy.
  ///
  /// In ar, this message translates to:
  /// **'لاحظنا أن الرحلة ابتعدت عن المسار المتوقع.'**
  String get alertRouteDeviationCopy;

  /// No description provided for @alertTripOverrunCopy.
  ///
  /// In ar, this message translates to:
  /// **'الرحلة تستغرق وقتاً أطول بكثير من المتوقع.'**
  String get alertTripOverrunCopy;

  /// No description provided for @safetyCheckCopy.
  ///
  /// In ar, this message translates to:
  /// **'نريد الاطمئنان عليك أثناء الرحلة.'**
  String get safetyCheckCopy;

  /// No description provided for @reportUnsafeDriving.
  ///
  /// In ar, this message translates to:
  /// **'قيادة غير آمنة'**
  String get reportUnsafeDriving;

  /// No description provided for @reportHarassment.
  ///
  /// In ar, this message translates to:
  /// **'تحرش أو إساءة'**
  String get reportHarassment;

  /// No description provided for @reportVehicleMismatch.
  ///
  /// In ar, this message translates to:
  /// **'المركبة لا تطابق التطبيق'**
  String get reportVehicleMismatch;

  /// No description provided for @reportDriverMismatch.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن لا يطابق التطبيق'**
  String get reportDriverMismatch;

  /// No description provided for @reportPassengerMisconduct.
  ///
  /// In ar, this message translates to:
  /// **'سلوك غير لائق من الراكب'**
  String get reportPassengerMisconduct;

  /// No description provided for @reportOther.
  ///
  /// In ar, this message translates to:
  /// **'أخرى'**
  String get reportOther;

  /// No description provided for @lostPhone.
  ///
  /// In ar, this message translates to:
  /// **'جوال'**
  String get lostPhone;

  /// No description provided for @lostWallet.
  ///
  /// In ar, this message translates to:
  /// **'محفظة'**
  String get lostWallet;

  /// No description provided for @lostBag.
  ///
  /// In ar, this message translates to:
  /// **'حقيبة'**
  String get lostBag;

  /// No description provided for @lostKeys.
  ///
  /// In ar, this message translates to:
  /// **'مفاتيح'**
  String get lostKeys;

  /// No description provided for @lostDocuments.
  ///
  /// In ar, this message translates to:
  /// **'مستندات'**
  String get lostDocuments;

  /// No description provided for @lostOther.
  ///
  /// In ar, this message translates to:
  /// **'أخرى'**
  String get lostOther;

  /// No description provided for @lostStatusOpen.
  ///
  /// In ar, this message translates to:
  /// **'بانتظار رد الكابتن'**
  String get lostStatusOpen;

  /// No description provided for @lostStatusDriverContacted.
  ///
  /// In ar, this message translates to:
  /// **'تم التواصل مع الكابتن'**
  String get lostStatusDriverContacted;

  /// No description provided for @lostStatusFound.
  ///
  /// In ar, this message translates to:
  /// **'تم العثور عليه'**
  String get lostStatusFound;

  /// No description provided for @lostStatusReturned.
  ///
  /// In ar, this message translates to:
  /// **'تمت الإعادة'**
  String get lostStatusReturned;

  /// No description provided for @lostStatusNotFound.
  ///
  /// In ar, this message translates to:
  /// **'لم يُعثر عليه'**
  String get lostStatusNotFound;

  /// No description provided for @lostStatusClosed.
  ///
  /// In ar, this message translates to:
  /// **'مغلق'**
  String get lostStatusClosed;

  /// No description provided for @sosSemantics.
  ///
  /// In ar, this message translates to:
  /// **'زر الطوارئ، اضغط مطولاً للتأكيد'**
  String get sosSemantics;

  /// No description provided for @sosLabel.
  ///
  /// In ar, this message translates to:
  /// **'SOS'**
  String get sosLabel;

  /// No description provided for @sosHoldHint.
  ///
  /// In ar, this message translates to:
  /// **'اضغط مطولاً للطوارئ'**
  String get sosHoldHint;

  /// No description provided for @sosKeepHolding.
  ///
  /// In ar, this message translates to:
  /// **'استمر بالضغط…'**
  String get sosKeepHolding;

  /// No description provided for @sosSending.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ إرسال نداء الطوارئ…'**
  String get sosSending;

  /// No description provided for @sosActiveTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم إرسال نداء الطوارئ لفريق السلامة'**
  String get sosActiveTitle;

  /// No description provided for @sosCancelledTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم إلغاء نداء الطوارئ'**
  String get sosCancelledTitle;

  /// No description provided for @sosFailedTitle.
  ///
  /// In ar, this message translates to:
  /// **'تعذّر إرسال نداء الطوارئ'**
  String get sosFailedTitle;

  /// No description provided for @sosCaseLine.
  ///
  /// In ar, this message translates to:
  /// **'الحالة {number} · {status}'**
  String sosCaseLine(String number, String status);

  /// No description provided for @sosContactsNotified.
  ///
  /// In ar, this message translates to:
  /// **'تم إبلاغ {count} من جهاتك الموثوقة'**
  String sosContactsNotified(int count);

  /// No description provided for @sosSharingLocation.
  ///
  /// In ar, this message translates to:
  /// **'نشارك موقعك مع فريق السلامة كل 10 ثوانٍ'**
  String get sosSharingLocation;

  /// No description provided for @sosCancelledCopy.
  ///
  /// In ar, this message translates to:
  /// **'سيتواصل معك فريق السلامة للتأكد من سلامتك.'**
  String get sosCancelledCopy;

  /// No description provided for @callEmergency.
  ///
  /// In ar, this message translates to:
  /// **'اتصل بالطوارئ {number}'**
  String callEmergency(String number);

  /// No description provided for @sosPressedByMistake.
  ///
  /// In ar, this message translates to:
  /// **'ضغطت بالخطأ'**
  String get sosPressedByMistake;

  /// No description provided for @close.
  ///
  /// In ar, this message translates to:
  /// **'إغلاق'**
  String get close;

  /// No description provided for @safetyCheckTitle.
  ///
  /// In ar, this message translates to:
  /// **'هل أنت بخير؟'**
  String get safetyCheckTitle;

  /// No description provided for @safetyCheckHelpSent.
  ///
  /// In ar, this message translates to:
  /// **'تم إبلاغ فريق السلامة وسيتواصل معك فوراً.'**
  String get safetyCheckHelpSent;

  /// No description provided for @safetyCheckOkThanks.
  ///
  /// In ar, this message translates to:
  /// **'شكراً لك، سعداء أنك بخير.'**
  String get safetyCheckOkThanks;

  /// No description provided for @safetyCheckExpired.
  ///
  /// In ar, this message translates to:
  /// **'لم نتلقَّ ردك، سيتواصل معك فريق السلامة.'**
  String get safetyCheckExpired;

  /// No description provided for @safetyCheckCountdown.
  ///
  /// In ar, this message translates to:
  /// **'يرجى الرد خلال {seconds} ث'**
  String safetyCheckCountdown(int seconds);

  /// No description provided for @safetyCheckOk.
  ///
  /// In ar, this message translates to:
  /// **'أنا بخير'**
  String get safetyCheckOk;

  /// No description provided for @safetyCheckHelp.
  ///
  /// In ar, this message translates to:
  /// **'أحتاج مساعدة'**
  String get safetyCheckHelp;

  /// No description provided for @manageSharing.
  ///
  /// In ar, this message translates to:
  /// **'إدارة'**
  String get manageSharing;

  /// No description provided for @shareTripLinkText.
  ///
  /// In ar, this message translates to:
  /// **'تابع رحلتي مع ATA مباشرة: {url}'**
  String shareTripLinkText(String url);

  /// No description provided for @shareSheetCopy.
  ///
  /// In ar, this message translates to:
  /// **'أرسل رابط تتبع مباشر، ويمكنك إيقافه في أي وقت.'**
  String get shareSheetCopy;

  /// No description provided for @shareToContacts.
  ///
  /// In ar, this message translates to:
  /// **'إرسال لجهاتك الموثوقة'**
  String get shareToContacts;

  /// No description provided for @addTrustedContact.
  ///
  /// In ar, this message translates to:
  /// **'إضافة جهة موثوقة'**
  String get addTrustedContact;

  /// No description provided for @sendBySms.
  ///
  /// In ar, this message translates to:
  /// **'إرسال عبر رسالة نصية'**
  String get sendBySms;

  /// No description provided for @smsSentTo.
  ///
  /// In ar, this message translates to:
  /// **'تم الإرسال إلى {count}'**
  String smsSentTo(int count);

  /// No description provided for @activeLinks.
  ///
  /// In ar, this message translates to:
  /// **'الروابط النشطة'**
  String get activeLinks;

  /// No description provided for @noActiveLinks.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد روابط نشطة.'**
  String get noActiveLinks;

  /// No description provided for @shareViews.
  ///
  /// In ar, this message translates to:
  /// **'{count} مشاهدة'**
  String shareViews(int count);

  /// No description provided for @revokeLink.
  ///
  /// In ar, this message translates to:
  /// **'إيقاف'**
  String get revokeLink;

  /// No description provided for @chatAction.
  ///
  /// In ar, this message translates to:
  /// **'محادثة'**
  String get chatAction;

  /// No description provided for @maskedCallPin.
  ///
  /// In ar, this message translates to:
  /// **'رمز الاتصال: {pin}'**
  String maskedCallPin(String pin);

  /// No description provided for @callUnavailable.
  ///
  /// In ar, this message translates to:
  /// **'الاتصال غير متاح حالياً، تواصل عبر المحادثة.'**
  String get callUnavailable;

  /// No description provided for @chatWithDriver.
  ///
  /// In ar, this message translates to:
  /// **'المحادثة مع الكابتن'**
  String get chatWithDriver;

  /// No description provided for @chatWithPassenger.
  ///
  /// In ar, this message translates to:
  /// **'المحادثة مع الراكب'**
  String get chatWithPassenger;

  /// No description provided for @chatMaskedNote.
  ///
  /// In ar, this message translates to:
  /// **'لا تتم مشاركة أرقام الهواتف، وتُخفى الأرقام داخل الرسائل.'**
  String get chatMaskedNote;

  /// No description provided for @chatEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد رسائل بعد.'**
  String get chatEmpty;

  /// No description provided for @chatInputHint.
  ///
  /// In ar, this message translates to:
  /// **'اكتب رسالة…'**
  String get chatInputHint;

  /// No description provided for @chatSend.
  ///
  /// In ar, this message translates to:
  /// **'إرسال'**
  String get chatSend;

  /// No description provided for @chatClosedBanner.
  ///
  /// In ar, this message translates to:
  /// **'انتهت الرحلة، المحادثة للقراءة فقط.'**
  String get chatClosedBanner;

  /// No description provided for @messageSending.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ الإرسال…'**
  String get messageSending;

  /// No description provided for @messageFailed.
  ///
  /// In ar, this message translates to:
  /// **'تعذّر الإرسال، اضغط لإعادة المحاولة'**
  String get messageFailed;

  /// No description provided for @messageRead.
  ///
  /// In ar, this message translates to:
  /// **'مقروءة'**
  String get messageRead;

  /// No description provided for @sosHoldCopy.
  ///
  /// In ar, this message translates to:
  /// **'اضغط مطولاً على SOS لإبلاغ فريق السلامة بموقعك فوراً'**
  String get sosHoldCopy;

  /// No description provided for @shareTripOnTripOnly.
  ///
  /// In ar, this message translates to:
  /// **'متاحة أثناء الرحلة من بطاقة الكابتن، ويمكن إرسالها تلقائياً لجهاتك الموثوقة.'**
  String get shareTripOnTripOnly;

  /// No description provided for @myReportsTitle.
  ///
  /// In ar, this message translates to:
  /// **'بلاغاتي'**
  String get myReportsTitle;

  /// No description provided for @myReportsCopy.
  ///
  /// In ar, this message translates to:
  /// **'تابع حالة بلاغات السلامة ونداءات الطوارئ.'**
  String get myReportsCopy;

  /// No description provided for @lostItemsTitle.
  ///
  /// In ar, this message translates to:
  /// **'المفقودات'**
  String get lostItemsTitle;

  /// No description provided for @lostItemsCopy.
  ///
  /// In ar, this message translates to:
  /// **'تابع بلاغات الأغراض المفقودة في رحلاتك.'**
  String get lostItemsCopy;

  /// No description provided for @trustedContactsPageCopy.
  ///
  /// In ar, this message translates to:
  /// **'يمكنك إضافة حتى 5 جهات تصلها رسالة عند الطوارئ، وتلقائياً رابط تتبع رحلاتك إن فعّلت المشاركة التلقائية.'**
  String get trustedContactsPageCopy;

  /// No description provided for @noTrustedContacts.
  ///
  /// In ar, this message translates to:
  /// **'لم تضف أي جهة موثوقة بعد.'**
  String get noTrustedContacts;

  /// No description provided for @trustedContactsCount.
  ///
  /// In ar, this message translates to:
  /// **'{count} من {max}'**
  String trustedContactsCount(int count, int max);

  /// No description provided for @edit.
  ///
  /// In ar, this message translates to:
  /// **'تعديل'**
  String get edit;

  /// No description provided for @delete.
  ///
  /// In ar, this message translates to:
  /// **'حذف'**
  String get delete;

  /// No description provided for @autoShareLabel.
  ///
  /// In ar, this message translates to:
  /// **'مشاركة رحلاتي تلقائياً'**
  String get autoShareLabel;

  /// No description provided for @notifyOnSosLabel.
  ///
  /// In ar, this message translates to:
  /// **'إبلاغها عند الطوارئ'**
  String get notifyOnSosLabel;

  /// No description provided for @contactPhoneSelf.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكنك إضافة رقمك'**
  String get contactPhoneSelf;

  /// No description provided for @editTrustedContact.
  ///
  /// In ar, this message translates to:
  /// **'تعديل الجهة الموثوقة'**
  String get editTrustedContact;

  /// No description provided for @contactNameLabel.
  ///
  /// In ar, this message translates to:
  /// **'الاسم'**
  String get contactNameLabel;

  /// No description provided for @contactNameRequired.
  ///
  /// In ar, this message translates to:
  /// **'الاسم مطلوب'**
  String get contactNameRequired;

  /// No description provided for @contactPhoneLabel.
  ///
  /// In ar, this message translates to:
  /// **'رقم الجوال'**
  String get contactPhoneLabel;

  /// No description provided for @contactPhoneHint.
  ///
  /// In ar, this message translates to:
  /// **'05XXXXXXXX'**
  String get contactPhoneHint;

  /// No description provided for @contactRelationshipLabel.
  ///
  /// In ar, this message translates to:
  /// **'صلة القرابة (اختياري)'**
  String get contactRelationshipLabel;

  /// No description provided for @save.
  ///
  /// In ar, this message translates to:
  /// **'حفظ'**
  String get save;

  /// No description provided for @noReports.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد بلاغات.'**
  String get noReports;

  /// No description provided for @caseNoUpdates.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد تحديثات بعد، سيتواصل معك فريق السلامة.'**
  String get caseNoUpdates;

  /// No description provided for @safetyReportTitle.
  ///
  /// In ar, this message translates to:
  /// **'الإبلاغ عن مشكلة سلامة'**
  String get safetyReportTitle;

  /// No description provided for @safetyReportCopy.
  ///
  /// In ar, this message translates to:
  /// **'أخبرنا بما حدث خلال رحلتك (خلال 7 أيام)، وسيراجعه فريق السلامة.'**
  String get safetyReportCopy;

  /// No description provided for @reportSubmitted.
  ///
  /// In ar, this message translates to:
  /// **'تم استلام بلاغك'**
  String get reportSubmitted;

  /// No description provided for @reportNumberLine.
  ///
  /// In ar, this message translates to:
  /// **'رقم البلاغ: {number}'**
  String reportNumberLine(String number);

  /// No description provided for @chooseCategory.
  ///
  /// In ar, this message translates to:
  /// **'اختر نوع المشكلة'**
  String get chooseCategory;

  /// No description provided for @describeWhatHappened.
  ///
  /// In ar, this message translates to:
  /// **'صف ما حدث…'**
  String get describeWhatHappened;

  /// No description provided for @descriptionRequired.
  ///
  /// In ar, this message translates to:
  /// **'الوصف مطلوب'**
  String get descriptionRequired;

  /// No description provided for @submitReport.
  ///
  /// In ar, this message translates to:
  /// **'إرسال البلاغ'**
  String get submitReport;

  /// No description provided for @noPendingSafetyCheck.
  ///
  /// In ar, this message translates to:
  /// **'لا يوجد سؤال سلامة معلّق.'**
  String get noPendingSafetyCheck;

  /// No description provided for @lostItemTitle.
  ///
  /// In ar, this message translates to:
  /// **'الإبلاغ عن غرض مفقود'**
  String get lostItemTitle;

  /// No description provided for @lostItemCopy.
  ///
  /// In ar, this message translates to:
  /// **'صف الغرض وسنبلغ الكابتن ونتابع معك عبر الدعم (خلال 7 أيام من الرحلة).'**
  String get lostItemCopy;

  /// No description provided for @lostItemSubmitted.
  ///
  /// In ar, this message translates to:
  /// **'تم استلام بلاغ المفقودات'**
  String get lostItemSubmitted;

  /// No description provided for @lostItemDescribe.
  ///
  /// In ar, this message translates to:
  /// **'صف الغرض (اللون، العلامة، مكانه في السيارة…)'**
  String get lostItemDescribe;

  /// No description provided for @lostItemContactPhone.
  ///
  /// In ar, this message translates to:
  /// **'رقم للتواصل (اختياري)'**
  String get lostItemContactPhone;

  /// No description provided for @lostItemsPageCopy.
  ///
  /// In ar, this message translates to:
  /// **'حالة بلاغات الأغراض المفقودة.'**
  String get lostItemsPageCopy;

  /// No description provided for @noLostItems.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد بلاغات مفقودات.'**
  String get noLostItems;

  /// No description provided for @driverLostItemsTitle.
  ///
  /// In ar, this message translates to:
  /// **'المفقودات'**
  String get driverLostItemsTitle;

  /// No description provided for @driverLostItemsCopy.
  ///
  /// In ar, this message translates to:
  /// **'أغراض أبلغ عنها الركاب في رحلاتك.'**
  String get driverLostItemsCopy;

  /// No description provided for @lostItemFound.
  ///
  /// In ar, this message translates to:
  /// **'وجدته'**
  String get lostItemFound;

  /// No description provided for @lostItemNotFound.
  ///
  /// In ar, this message translates to:
  /// **'لم أجده'**
  String get lostItemNotFound;

  /// No description provided for @tripHelpTitle.
  ///
  /// In ar, this message translates to:
  /// **'هل تحتاج مساعدة بخصوص هذه الرحلة؟'**
  String get tripHelpTitle;

  /// No description provided for @lostItemRowCopy.
  ///
  /// In ar, this message translates to:
  /// **'نسيت شيئاً في السيارة؟'**
  String get lostItemRowCopy;

  /// No description provided for @safetyReportRowCopy.
  ///
  /// In ar, this message translates to:
  /// **'أبلغ عن قيادة غير آمنة أو سلوك غير لائق'**
  String get safetyReportRowCopy;

  /// No description provided for @later.
  ///
  /// In ar, this message translates to:
  /// **'لاحقاً'**
  String get later;

  /// No description provided for @rateShort.
  ///
  /// In ar, this message translates to:
  /// **'قيّم'**
  String get rateShort;

  /// No description provided for @rateNow.
  ///
  /// In ar, this message translates to:
  /// **'قيّم الآن'**
  String get rateNow;

  /// No description provided for @ratePassenger.
  ///
  /// In ar, this message translates to:
  /// **'قيّم الراكب'**
  String get ratePassenger;

  /// No description provided for @tripRated.
  ///
  /// In ar, this message translates to:
  /// **'تم تقييم الرحلة'**
  String get tripRated;

  /// No description provided for @rateTripCopy.
  ///
  /// In ar, this message translates to:
  /// **'تقييمك يساعدنا على تحسين الخدمة ويبقى مجهول الهوية.'**
  String get rateTripCopy;

  /// No description provided for @rateDriverQuestion.
  ///
  /// In ar, this message translates to:
  /// **'كيف كانت رحلتك مع الكابتن؟'**
  String get rateDriverQuestion;

  /// No description provided for @rateDriverQuestionNamed.
  ///
  /// In ar, this message translates to:
  /// **'كيف كانت رحلتك مع {name}؟'**
  String rateDriverQuestionNamed(String name);

  /// No description provided for @ratePassengerQuestion.
  ///
  /// In ar, this message translates to:
  /// **'كيف كان الراكب؟'**
  String get ratePassengerQuestion;

  /// No description provided for @ratePassengerQuestionNamed.
  ///
  /// In ar, this message translates to:
  /// **'كيف كان الراكب {name}؟'**
  String ratePassengerQuestionNamed(String name);

  /// No description provided for @ratingStarsNone.
  ///
  /// In ar, this message translates to:
  /// **'اختر عدد النجوم'**
  String get ratingStarsNone;

  /// No description provided for @ratingStars1.
  ///
  /// In ar, this message translates to:
  /// **'سيئة'**
  String get ratingStars1;

  /// No description provided for @ratingStars2.
  ///
  /// In ar, this message translates to:
  /// **'دون المتوقع'**
  String get ratingStars2;

  /// No description provided for @ratingStars3.
  ///
  /// In ar, this message translates to:
  /// **'مقبولة'**
  String get ratingStars3;

  /// No description provided for @ratingStars4.
  ///
  /// In ar, this message translates to:
  /// **'جيدة'**
  String get ratingStars4;

  /// No description provided for @ratingStars5.
  ///
  /// In ar, this message translates to:
  /// **'ممتازة'**
  String get ratingStars5;

  /// No description provided for @ratingTagsPositive.
  ///
  /// In ar, this message translates to:
  /// **'ما الذي أعجبك؟'**
  String get ratingTagsPositive;

  /// No description provided for @ratingTagsNegative.
  ///
  /// In ar, this message translates to:
  /// **'ما الذي لم يعجبك؟'**
  String get ratingTagsNegative;

  /// No description provided for @ratingTagDriving.
  ///
  /// In ar, this message translates to:
  /// **'القيادة'**
  String get ratingTagDriving;

  /// No description provided for @ratingTagCleanliness.
  ///
  /// In ar, this message translates to:
  /// **'النظافة'**
  String get ratingTagCleanliness;

  /// No description provided for @ratingTagBehaviour.
  ///
  /// In ar, this message translates to:
  /// **'التعامل'**
  String get ratingTagBehaviour;

  /// No description provided for @ratingTagNavigation.
  ///
  /// In ar, this message translates to:
  /// **'معرفة الطريق'**
  String get ratingTagNavigation;

  /// No description provided for @ratingTagVehicleCondition.
  ///
  /// In ar, this message translates to:
  /// **'حالة المركبة'**
  String get ratingTagVehicleCondition;

  /// No description provided for @ratingTagPunctuality.
  ///
  /// In ar, this message translates to:
  /// **'الالتزام بالوقت'**
  String get ratingTagPunctuality;

  /// No description provided for @ratingCommentHint.
  ///
  /// In ar, this message translates to:
  /// **'أضف تعليقاً (اختياري)'**
  String get ratingCommentHint;

  /// No description provided for @ratingSubmit.
  ///
  /// In ar, this message translates to:
  /// **'إرسال التقييم'**
  String get ratingSubmit;

  /// No description provided for @ratingThanks.
  ///
  /// In ar, this message translates to:
  /// **'شكراً لتقييمك'**
  String get ratingThanks;

  /// No description provided for @ratingThanksCopy.
  ///
  /// In ar, this message translates to:
  /// **'ملاحظاتك تساعدنا على تقديم رحلات أفضل.'**
  String get ratingThanksCopy;

  /// No description provided for @ratingWindowClosedError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت مدة التقييم'**
  String get ratingWindowClosedError;

  /// No description provided for @ratingExistsError.
  ///
  /// In ar, this message translates to:
  /// **'تم تقييم هذه الرحلة مسبقاً'**
  String get ratingExistsError;

  /// No description provided for @pendingRatingTitle.
  ///
  /// In ar, this message translates to:
  /// **'قيّم رحلتك الأخيرة'**
  String get pendingRatingTitle;

  /// No description provided for @pendingRatingCopy.
  ///
  /// In ar, this message translates to:
  /// **'أخبرنا كيف كانت الرحلة.'**
  String get pendingRatingCopy;

  /// No description provided for @pendingRatingCopyNamed.
  ///
  /// In ar, this message translates to:
  /// **'كيف كانت رحلتك مع {name}؟'**
  String pendingRatingCopyNamed(String name);

  /// No description provided for @driverRatingsTitle.
  ///
  /// In ar, this message translates to:
  /// **'تقييماتي'**
  String get driverRatingsTitle;

  /// No description provided for @driverRatingsCopy.
  ///
  /// In ar, this message translates to:
  /// **'متوسط تقييمك وأكثر ما يذكره الركاب، دون أسماء.'**
  String get driverRatingsCopy;

  /// No description provided for @ratingCountLine.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =0{لا تقييمات بعد} =1{تقييم واحد} =2{تقييمان} few{{count} تقييمات} other{{count} تقييم}}'**
  String ratingCountLine(int count);

  /// No description provided for @ratingTopTags.
  ///
  /// In ar, this message translates to:
  /// **'الأكثر ذكراً'**
  String get ratingTopTags;

  /// No description provided for @ratingRecentComments.
  ///
  /// In ar, this message translates to:
  /// **'آخر التعليقات'**
  String get ratingRecentComments;

  /// No description provided for @ratingNoComments.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد تعليقات بعد.'**
  String get ratingNoComments;

  /// No description provided for @fareTotalBeforeDiscount.
  ///
  /// In ar, this message translates to:
  /// **'قبل الخصم'**
  String get fareTotalBeforeDiscount;

  /// No description provided for @receiptPromo.
  ///
  /// In ar, this message translates to:
  /// **'كود الخصم {code}'**
  String receiptPromo(String code);

  /// No description provided for @promoReserved.
  ///
  /// In ar, this message translates to:
  /// **'محجوز'**
  String get promoReserved;

  /// No description provided for @promoCodeLabel.
  ///
  /// In ar, this message translates to:
  /// **'كود خصم'**
  String get promoCodeLabel;

  /// No description provided for @promoAddHint.
  ///
  /// In ar, this message translates to:
  /// **'أضف كود الخصم إن كان لديك'**
  String get promoAddHint;

  /// No description provided for @promoNotWithOffer.
  ///
  /// In ar, this message translates to:
  /// **'لا ينطبق الخصم مع اقتراح السعر'**
  String get promoNotWithOffer;

  /// No description provided for @promoAppliedLine.
  ///
  /// In ar, this message translates to:
  /// **'{code} مطبّق'**
  String promoAppliedLine(String code);

  /// No description provided for @promoAppliedDiscount.
  ///
  /// In ar, this message translates to:
  /// **'{code} مطبّق · وفّرت {amount}'**
  String promoAppliedDiscount(String code, String amount);

  /// No description provided for @promoRemove.
  ///
  /// In ar, this message translates to:
  /// **'إزالة'**
  String get promoRemove;

  /// No description provided for @promoCodeTitle.
  ///
  /// In ar, this message translates to:
  /// **'كود الخصم'**
  String get promoCodeTitle;

  /// No description provided for @promoCodeCopy.
  ///
  /// In ar, this message translates to:
  /// **'أدخل الكود وسنتحقق منه على سعر رحلتك الحالية.'**
  String get promoCodeCopy;

  /// No description provided for @promoCodeHint.
  ///
  /// In ar, this message translates to:
  /// **'مثال: ATA10'**
  String get promoCodeHint;

  /// No description provided for @promoApply.
  ///
  /// In ar, this message translates to:
  /// **'تطبيق'**
  String get promoApply;

  /// No description provided for @promoNotFoundError.
  ///
  /// In ar, this message translates to:
  /// **'كود الخصم غير صحيح'**
  String get promoNotFoundError;

  /// No description provided for @promoExpiredError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت صلاحية كود الخصم'**
  String get promoExpiredError;

  /// No description provided for @promoNotEligibleError.
  ///
  /// In ar, this message translates to:
  /// **'كود الخصم لا ينطبق على هذه الرحلة'**
  String get promoNotEligibleError;

  /// No description provided for @promoUsageLimitError.
  ///
  /// In ar, this message translates to:
  /// **'تم استنفاد كود الخصم'**
  String get promoUsageLimitError;

  /// No description provided for @promoUserLimitError.
  ///
  /// In ar, this message translates to:
  /// **'استخدمت هذا الكود الحد الأقصى من المرات'**
  String get promoUserLimitError;

  /// No description provided for @promoReasonFirstTrip.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود لرحلتك الأولى فقط'**
  String get promoReasonFirstTrip;

  /// No description provided for @promoReasonNewUsers.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود للمستخدمين الجدد فقط'**
  String get promoReasonNewUsers;

  /// No description provided for @promoReasonCity.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود غير متاح في مدينتك'**
  String get promoReasonCity;

  /// No description provided for @promoReasonCategory.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود لا ينطبق على فئة الرحلة المختارة'**
  String get promoReasonCategory;

  /// No description provided for @promoReasonZone.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود لا ينطبق على منطقة الالتقاط'**
  String get promoReasonZone;

  /// No description provided for @promoReasonPaymentMethod.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود لا ينطبق على طريقة الدفع المختارة'**
  String get promoReasonPaymentMethod;

  /// No description provided for @promoReasonBookingType.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكود لا ينطبق على نوع الحجز'**
  String get promoReasonBookingType;

  /// No description provided for @promoReasonMinFare.
  ///
  /// In ar, this message translates to:
  /// **'أجرة الرحلة أقل من الحد الأدنى لهذا الكود'**
  String get promoReasonMinFare;

  /// No description provided for @promoReasonPricingMode.
  ///
  /// In ar, this message translates to:
  /// **'لا ينطبق الخصم مع اقتراح السعر'**
  String get promoReasonPricingMode;

  /// No description provided for @promotionsTitle.
  ///
  /// In ar, this message translates to:
  /// **'العروض'**
  String get promotionsTitle;

  /// No description provided for @promotionsCopy.
  ///
  /// In ar, this message translates to:
  /// **'أكواد الخصم المتاحة لك وما استخدمته أو انتهت صلاحيته.'**
  String get promotionsCopy;

  /// No description provided for @promotionsLinkCopy.
  ///
  /// In ar, this message translates to:
  /// **'أكواد الخصم المتاحة لك'**
  String get promotionsLinkCopy;

  /// No description provided for @promotionsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد عروض هنا حالياً.'**
  String get promotionsEmpty;

  /// No description provided for @promoTabAvailable.
  ///
  /// In ar, this message translates to:
  /// **'المتاحة'**
  String get promoTabAvailable;

  /// No description provided for @promoTabUsed.
  ///
  /// In ar, this message translates to:
  /// **'المستخدمة'**
  String get promoTabUsed;

  /// No description provided for @promoTabExpired.
  ///
  /// In ar, this message translates to:
  /// **'المنتهية'**
  String get promoTabExpired;

  /// No description provided for @promoPercentOff.
  ///
  /// In ar, this message translates to:
  /// **'خصم {percent}%'**
  String promoPercentOff(String percent);

  /// No description provided for @promoUpTo.
  ///
  /// In ar, this message translates to:
  /// **'{discount} حتى {cap}'**
  String promoUpTo(String discount, String cap);

  /// No description provided for @promoAmountOff.
  ///
  /// In ar, this message translates to:
  /// **'خصم {amount}'**
  String promoAmountOff(String amount);

  /// No description provided for @promoFreeBookingFee.
  ///
  /// In ar, this message translates to:
  /// **'رسوم حجز مجانية'**
  String get promoFreeBookingFee;

  /// No description provided for @promoMinFare.
  ///
  /// In ar, this message translates to:
  /// **'الحد الأدنى للأجرة {amount}'**
  String promoMinFare(String amount);

  /// No description provided for @promoFirstTripOnly.
  ///
  /// In ar, this message translates to:
  /// **'للرحلة الأولى فقط'**
  String get promoFirstTripOnly;

  /// No description provided for @promoValidTo.
  ///
  /// In ar, this message translates to:
  /// **'صالح حتى {date}'**
  String promoValidTo(String date);

  /// No description provided for @promoUse.
  ///
  /// In ar, this message translates to:
  /// **'استخدم'**
  String get promoUse;

  /// No description provided for @promoCopied.
  ///
  /// In ar, this message translates to:
  /// **'تم نسخ الكود {code}'**
  String promoCopied(String code);

  /// No description provided for @tierTitle.
  ///
  /// In ar, this message translates to:
  /// **'مستوى الكابتن'**
  String get tierTitle;

  /// No description provided for @tierCopy.
  ///
  /// In ar, this message translates to:
  /// **'يُحسب أسبوعياً من أدائك في آخر 28 يوماً.'**
  String get tierCopy;

  /// No description provided for @tierBronze.
  ///
  /// In ar, this message translates to:
  /// **'برونزي'**
  String get tierBronze;

  /// No description provided for @tierSilver.
  ///
  /// In ar, this message translates to:
  /// **'فضي'**
  String get tierSilver;

  /// No description provided for @tierGold.
  ///
  /// In ar, this message translates to:
  /// **'ذهبي'**
  String get tierGold;

  /// No description provided for @tierPlatinum.
  ///
  /// In ar, this message translates to:
  /// **'بلاتيني'**
  String get tierPlatinum;

  /// No description provided for @tierTopReached.
  ///
  /// In ar, this message translates to:
  /// **'وصلت إلى أعلى مستوى، حافظ على أدائك.'**
  String get tierTopReached;

  /// No description provided for @tierTripsToNext.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{تبقّت رحلة واحدة للوصول إلى {tier}} =2{تبقّت رحلتان للوصول إلى {tier}} few{تبقّت {count} رحلات للوصول إلى {tier}} other{تبقّت {count} رحلة للوصول إلى {tier}}}'**
  String tierTripsToNext(int count, String tier);

  /// No description provided for @tierProgressTo.
  ///
  /// In ar, this message translates to:
  /// **'تقدّمك نحو {tier}'**
  String tierProgressTo(String tier);

  /// No description provided for @tierChecksMet.
  ///
  /// In ar, this message translates to:
  /// **'{met} من {total} شروط محققة'**
  String tierChecksMet(int met, int total);

  /// No description provided for @tierCommissionDiscount.
  ///
  /// In ar, this message translates to:
  /// **'خصم العمولة {percent}%'**
  String tierCommissionDiscount(String percent);

  /// No description provided for @tierRequirementsTitle.
  ///
  /// In ar, this message translates to:
  /// **'شروط {tier} (آخر {days} يوماً)'**
  String tierRequirementsTitle(String tier, int days);

  /// No description provided for @tierCriterionTrips.
  ///
  /// In ar, this message translates to:
  /// **'الرحلات المكتملة'**
  String get tierCriterionTrips;

  /// No description provided for @tierCriterionRating.
  ///
  /// In ar, this message translates to:
  /// **'متوسط التقييم'**
  String get tierCriterionRating;

  /// No description provided for @tierCriterionAcceptance.
  ///
  /// In ar, this message translates to:
  /// **'نسبة القبول'**
  String get tierCriterionAcceptance;

  /// No description provided for @tierCriterionCancellation.
  ///
  /// In ar, this message translates to:
  /// **'نسبة الإلغاء'**
  String get tierCriterionCancellation;

  /// No description provided for @tierRecalcAt.
  ///
  /// In ar, this message translates to:
  /// **'إعادة الحساب القادمة: {date}'**
  String tierRecalcAt(String date);

  /// No description provided for @tierRecalcWeekly.
  ///
  /// In ar, this message translates to:
  /// **'يُعاد حساب المستوى كل أحد.'**
  String get tierRecalcWeekly;

  /// No description provided for @incentivesTitle.
  ///
  /// In ar, this message translates to:
  /// **'الحوافز'**
  String get incentivesTitle;

  /// No description provided for @incentivesCopy.
  ///
  /// In ar, this message translates to:
  /// **'أكمل الرحلات المطلوبة خلال الفترة واحصل على المكافأة.'**
  String get incentivesCopy;

  /// No description provided for @incentivesLinkCopy.
  ///
  /// In ar, this message translates to:
  /// **'التحديات النشطة والقادمة ومكافآتها'**
  String get incentivesLinkCopy;

  /// No description provided for @incentivesEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد حوافز هنا حالياً.'**
  String get incentivesEmpty;

  /// No description provided for @incentiveNearest.
  ///
  /// In ar, this message translates to:
  /// **'أقرب حافز'**
  String get incentiveNearest;

  /// No description provided for @incentiveTabActive.
  ///
  /// In ar, this message translates to:
  /// **'النشطة'**
  String get incentiveTabActive;

  /// No description provided for @incentiveTabUpcoming.
  ///
  /// In ar, this message translates to:
  /// **'القادمة'**
  String get incentiveTabUpcoming;

  /// No description provided for @incentiveTabCompleted.
  ///
  /// In ar, this message translates to:
  /// **'المنتهية'**
  String get incentiveTabCompleted;

  /// No description provided for @incentiveTypeDaily.
  ///
  /// In ar, this message translates to:
  /// **'يومي'**
  String get incentiveTypeDaily;

  /// No description provided for @incentiveTypeWeekly.
  ///
  /// In ar, this message translates to:
  /// **'أسبوعي'**
  String get incentiveTypeWeekly;

  /// No description provided for @incentiveTypeZone.
  ///
  /// In ar, this message translates to:
  /// **'منطقة'**
  String get incentiveTypeZone;

  /// No description provided for @incentiveTypeOneTime.
  ///
  /// In ar, this message translates to:
  /// **'مرة واحدة'**
  String get incentiveTypeOneTime;

  /// No description provided for @incentiveInProgress.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ'**
  String get incentiveInProgress;

  /// No description provided for @incentiveAchieved.
  ///
  /// In ar, this message translates to:
  /// **'تحقق · يُصرف بعد نهاية الفترة'**
  String get incentiveAchieved;

  /// No description provided for @incentivePaid.
  ///
  /// In ar, this message translates to:
  /// **'تم الصرف'**
  String get incentivePaid;

  /// No description provided for @incentiveExpired.
  ///
  /// In ar, this message translates to:
  /// **'انتهى'**
  String get incentiveExpired;

  /// No description provided for @incentiveVoided.
  ///
  /// In ar, this message translates to:
  /// **'أُلغي'**
  String get incentiveVoided;

  /// No description provided for @incentiveTripsProgress.
  ///
  /// In ar, this message translates to:
  /// **'{done} من {target} رحلات'**
  String incentiveTripsProgress(int done, int target);

  /// No description provided for @incentiveTripsCount.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{رحلة واحدة} =2{رحلتان} few{{count} رحلات} other{{count} رحلة}}'**
  String incentiveTripsCount(int count);

  /// No description provided for @incentiveJoinFirst.
  ///
  /// In ar, this message translates to:
  /// **'اشترك لتبدأ الاحتساب'**
  String get incentiveJoinFirst;

  /// No description provided for @incentiveJoined.
  ///
  /// In ar, this message translates to:
  /// **'أنت مشترك في هذا الحافز'**
  String get incentiveJoined;

  /// No description provided for @incentiveOptIn.
  ///
  /// In ar, this message translates to:
  /// **'اشترك في الحافز'**
  String get incentiveOptIn;

  /// No description provided for @incentiveOptInClosedError.
  ///
  /// In ar, this message translates to:
  /// **'الاشتراك في هذا الحافز غير متاح'**
  String get incentiveOptInClosedError;

  /// No description provided for @incentiveEndsAt.
  ///
  /// In ar, this message translates to:
  /// **'ينتهي {date}'**
  String incentiveEndsAt(String date);

  /// No description provided for @incentiveReducedNotice.
  ///
  /// In ar, this message translates to:
  /// **'مكافآتك مخفّضة (×{multiplier}) بسبب مستوى الموثوقية، حسّن التزامك لاستعادتها كاملة.'**
  String incentiveReducedNotice(String multiplier);

  /// No description provided for @incentiveDetailTitle.
  ///
  /// In ar, this message translates to:
  /// **'تفاصيل الحافز'**
  String get incentiveDetailTitle;

  /// No description provided for @incentiveTarget.
  ///
  /// In ar, this message translates to:
  /// **'الهدف'**
  String get incentiveTarget;

  /// No description provided for @incentiveWindow.
  ///
  /// In ar, this message translates to:
  /// **'الأيام والساعات'**
  String get incentiveWindow;

  /// No description provided for @incentiveZones.
  ///
  /// In ar, this message translates to:
  /// **'المناطق'**
  String get incentiveZones;

  /// No description provided for @incentiveAllZones.
  ///
  /// In ar, this message translates to:
  /// **'كل المناطق'**
  String get incentiveAllZones;

  /// No description provided for @incentiveCategories.
  ///
  /// In ar, this message translates to:
  /// **'الفئات'**
  String get incentiveCategories;

  /// No description provided for @incentiveAllCategories.
  ///
  /// In ar, this message translates to:
  /// **'كل الفئات'**
  String get incentiveAllCategories;

  /// No description provided for @incentivePeriod.
  ///
  /// In ar, this message translates to:
  /// **'الفترة'**
  String get incentivePeriod;

  /// No description provided for @daySun.
  ///
  /// In ar, this message translates to:
  /// **'الأحد'**
  String get daySun;

  /// No description provided for @dayMon.
  ///
  /// In ar, this message translates to:
  /// **'الاثنين'**
  String get dayMon;

  /// No description provided for @dayTue.
  ///
  /// In ar, this message translates to:
  /// **'الثلاثاء'**
  String get dayTue;

  /// No description provided for @dayWed.
  ///
  /// In ar, this message translates to:
  /// **'الأربعاء'**
  String get dayWed;

  /// No description provided for @dayThu.
  ///
  /// In ar, this message translates to:
  /// **'الخميس'**
  String get dayThu;

  /// No description provided for @dayFri.
  ///
  /// In ar, this message translates to:
  /// **'الجمعة'**
  String get dayFri;

  /// No description provided for @daySat.
  ///
  /// In ar, this message translates to:
  /// **'السبت'**
  String get daySat;

  /// No description provided for @favoriteDriversTitle.
  ///
  /// In ar, this message translates to:
  /// **'السائقون المفضلون'**
  String get favoriteDriversTitle;

  /// No description provided for @favoriteDriversCopy.
  ///
  /// In ar, this message translates to:
  /// **'الكباتن الذين أضفتهم بعد رحلات سابقة. اطلبهم مباشرة واحصل على خصم.'**
  String get favoriteDriversCopy;

  /// No description provided for @favoriteEmptyTitle.
  ///
  /// In ar, this message translates to:
  /// **'لا يوجد كباتن مفضلون بعد'**
  String get favoriteEmptyTitle;

  /// No description provided for @favoriteEmptyCopy.
  ///
  /// In ar, this message translates to:
  /// **'بعد إكمال رحلة يمكنك إضافة الكابتن إلى مفضلتك من شاشة التقييم أو من الإيصال.'**
  String get favoriteEmptyCopy;

  /// No description provided for @favoriteTripsTogether.
  ///
  /// In ar, this message translates to:
  /// **'{count, plural, =1{رحلة واحدة معاً} =2{رحلتان معاً} few{{count} رحلات معاً} other{{count} رحلة معاً}}'**
  String favoriteTripsTogether(int count);

  /// No description provided for @favoriteLastTrip.
  ///
  /// In ar, this message translates to:
  /// **'آخر رحلة {date}'**
  String favoriteLastTrip(String date);

  /// No description provided for @favoriteAvailableNow.
  ///
  /// In ar, this message translates to:
  /// **'متاح الآن'**
  String get favoriteAvailableNow;

  /// No description provided for @favoriteAvailableEta.
  ///
  /// In ar, this message translates to:
  /// **'متاح الآن · يصل خلال {eta}'**
  String favoriteAvailableEta(String eta);

  /// No description provided for @favoriteRemove.
  ///
  /// In ar, this message translates to:
  /// **'إزالة من المفضلة'**
  String get favoriteRemove;

  /// No description provided for @favoriteRemoveTitle.
  ///
  /// In ar, this message translates to:
  /// **'إزالة {name} من المفضلة؟'**
  String favoriteRemoveTitle(String name);

  /// No description provided for @favoriteRemoveCopy.
  ///
  /// In ar, this message translates to:
  /// **'لن تتمكن من طلبه مباشرة ولن ينطبق خصم المفضل معه. لا يتأثر أي رحلة جارية.'**
  String get favoriteRemoveCopy;

  /// No description provided for @favoriteRemoveConfirm.
  ///
  /// In ar, this message translates to:
  /// **'إزالة'**
  String get favoriteRemoveConfirm;

  /// No description provided for @favoriteRemovedSnack.
  ///
  /// In ar, this message translates to:
  /// **'تمت إزالة {name} من المفضلة'**
  String favoriteRemovedSnack(String name);

  /// No description provided for @favoriteAdd.
  ///
  /// In ar, this message translates to:
  /// **'أضف إلى المفضلة'**
  String get favoriteAdd;

  /// No description provided for @favoriteAdding.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ الإضافة…'**
  String get favoriteAdding;

  /// No description provided for @favoriteAdded.
  ///
  /// In ar, this message translates to:
  /// **'تمت الإضافة إلى المفضلة'**
  String get favoriteAdded;

  /// No description provided for @favoriteAlready.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن في مفضلتك'**
  String get favoriteAlready;

  /// No description provided for @favoriteAddOptionCopy.
  ///
  /// In ar, this message translates to:
  /// **'اطلب {name} مباشرة في رحلاتك القادمة'**
  String favoriteAddOptionCopy(String name);

  /// No description provided for @favoriteAddOptionCopyAnon.
  ///
  /// In ar, this message translates to:
  /// **'اطلب هذا الكابتن مباشرة في رحلاتك القادمة'**
  String get favoriteAddOptionCopyAnon;

  /// No description provided for @favoriteRatingAdded.
  ///
  /// In ar, this message translates to:
  /// **'تمت إضافة الكابتن إلى مفضلتك'**
  String get favoriteRatingAdded;

  /// No description provided for @favoriteRatingFailed.
  ///
  /// In ar, this message translates to:
  /// **'تم إرسال التقييم، لكن تعذّرت إضافة الكابتن إلى المفضلة: {reason}'**
  String favoriteRatingFailed(String reason);

  /// No description provided for @favoriteNotEligibleError.
  ///
  /// In ar, this message translates to:
  /// **'يمكنك إضافة الكابتن بعد إكمال رحلة معه'**
  String get favoriteNotEligibleError;

  /// No description provided for @favoriteExistsError.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن موجود في مفضلتك بالفعل'**
  String get favoriteExistsError;

  /// No description provided for @favoritesLimitError.
  ///
  /// In ar, this message translates to:
  /// **'وصلت إلى الحد الأقصى للكباتن المفضلين'**
  String get favoritesLimitError;

  /// No description provided for @favoriteNotFavoriteError.
  ///
  /// In ar, this message translates to:
  /// **'هذا الكابتن لم يعد في مفضلتك'**
  String get favoriteNotFavoriteError;

  /// No description provided for @favoriteRowTitle.
  ///
  /// In ar, this message translates to:
  /// **'السائقون المفضلون'**
  String get favoriteRowTitle;

  /// No description provided for @favoriteRowManage.
  ///
  /// In ar, this message translates to:
  /// **'إدارة'**
  String get favoriteRowManage;

  /// No description provided for @favoriteRowHint.
  ///
  /// In ar, this message translates to:
  /// **'اختر كابتنك المفضل ليصله طلبك أولاً'**
  String get favoriteRowHint;

  /// No description provided for @favoriteRowNone.
  ///
  /// In ar, this message translates to:
  /// **'لا أحد من كباتنك المفضلين متاح الآن'**
  String get favoriteRowNone;

  /// No description provided for @favoriteRowLoading.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ البحث عن كباتنك المفضلين…'**
  String get favoriteRowLoading;

  /// No description provided for @favoriteRowNotWithOffer.
  ///
  /// In ar, this message translates to:
  /// **'غير متاح مع اقتراح السعر'**
  String get favoriteRowNotWithOffer;

  /// No description provided for @favoriteChipUnavailable.
  ///
  /// In ar, this message translates to:
  /// **'غير متاح حالياً'**
  String get favoriteChipUnavailable;

  /// No description provided for @favoriteDiscountBadge.
  ///
  /// In ar, this message translates to:
  /// **'خصم {percent}%'**
  String favoriteDiscountBadge(String percent);

  /// No description provided for @favoriteSelectedLine.
  ///
  /// In ar, this message translates to:
  /// **'سيصل طلبك أولاً إلى {name}'**
  String favoriteSelectedLine(String name);

  /// No description provided for @favoriteFallbackNote.
  ///
  /// In ar, this message translates to:
  /// **'إن لم يكن متاحاً سنبحث عن أقرب كابتن'**
  String get favoriteFallbackNote;

  /// No description provided for @favoriteDiscountSaved.
  ///
  /// In ar, this message translates to:
  /// **'خصم الكابتن المفضل · وفّرت {amount}'**
  String favoriteDiscountSaved(String amount);

  /// No description provided for @favoriteDiscountConditional.
  ///
  /// In ar, this message translates to:
  /// **'يُطبّق الخصم عند قبول {name} للرحلة'**
  String favoriteDiscountConditional(String name);

  /// No description provided for @favoritePromoNotStacked.
  ///
  /// In ar, this message translates to:
  /// **'لم يُطبَّق كود الخصم: لا يُجمع مع خصم الكابتن المفضل الأكبر'**
  String get favoritePromoNotStacked;

  /// No description provided for @favoriteLostToPromo.
  ///
  /// In ar, this message translates to:
  /// **'لم يُطبَّق خصم المفضل: كود الخصم أكبر ولا يُجمع معه'**
  String get favoriteLostToPromo;

  /// No description provided for @favoriteDeselect.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء اختيار الكابتن المفضل'**
  String get favoriteDeselect;

  /// No description provided for @favoriteSearchingTitle.
  ///
  /// In ar, this message translates to:
  /// **'نتواصل مع كابتنك المفضل...'**
  String get favoriteSearchingTitle;

  /// No description provided for @favoriteSearchingCopy.
  ///
  /// In ar, this message translates to:
  /// **'طلبك موجّه أولاً إلى {name}. إن لم يرد سنبحث لك عن أقرب كابتن.'**
  String favoriteSearchingCopy(String name);

  /// No description provided for @favoriteFallbackNotice.
  ///
  /// In ar, this message translates to:
  /// **'لم يتمكن {name} من الرد، نبحث لك عن كابتن آخر'**
  String favoriteFallbackNotice(String name);

  /// No description provided for @favoriteUnavailableNotice.
  ///
  /// In ar, this message translates to:
  /// **'{name} غير متاح حالياً، نبحث لك عن أقرب كابتن'**
  String favoriteUnavailableNotice(String name);

  /// No description provided for @receiptFavoriteDiscount.
  ///
  /// In ar, this message translates to:
  /// **'خصم الكابتن المفضل'**
  String get receiptFavoriteDiscount;

  /// No description provided for @favoriteDiscountApplied.
  ///
  /// In ar, this message translates to:
  /// **'مطبّق'**
  String get favoriteDiscountApplied;

  /// No description provided for @favoriteDriverBadge.
  ///
  /// In ar, this message translates to:
  /// **'مفضل'**
  String get favoriteDriverBadge;

  /// No description provided for @offerFavoriteRequest.
  ///
  /// In ar, this message translates to:
  /// **'من راكب يفضّلك'**
  String get offerFavoriteRequest;

  /// No description provided for @offerFavoriteExclusive.
  ///
  /// In ar, this message translates to:
  /// **'عرض حصري لك'**
  String get offerFavoriteExclusive;

  /// No description provided for @airportCategoryNotApplicableError.
  ///
  /// In ar, this message translates to:
  /// **'فئة المطار متاحة لرحلات المطار فقط'**
  String get airportCategoryNotApplicableError;

  /// No description provided for @airportChoose.
  ///
  /// In ar, this message translates to:
  /// **'اختر المطار واتجاه رحلتك'**
  String get airportChoose;

  /// No description provided for @airportChooseTerminal.
  ///
  /// In ar, this message translates to:
  /// **'الصالة (اختياري)'**
  String get airportChooseTerminal;

  /// No description provided for @airportChooseZone.
  ///
  /// In ar, this message translates to:
  /// **'اختر منطقة الالتقاط'**
  String get airportChooseZone;

  /// No description provided for @airportDirectionDropoff.
  ///
  /// In ar, this message translates to:
  /// **'إلى المطار'**
  String get airportDirectionDropoff;

  /// No description provided for @airportDirectionPickup.
  ///
  /// In ar, this message translates to:
  /// **'من المطار'**
  String get airportDirectionPickup;

  /// No description provided for @airportDone.
  ///
  /// In ar, this message translates to:
  /// **'تم'**
  String get airportDone;

  /// No description provided for @airportFlightHint.
  ///
  /// In ar, this message translates to:
  /// **'مثال: SV1020'**
  String get airportFlightHint;

  /// No description provided for @airportFlightInvalid.
  ///
  /// In ar, this message translates to:
  /// **'رقم الرحلة غير صحيح، مثال: SV1020'**
  String get airportFlightInvalid;

  /// No description provided for @airportFlightLabel.
  ///
  /// In ar, this message translates to:
  /// **'رقم الرحلة الجوية (اختياري)'**
  String get airportFlightLabel;

  /// No description provided for @airportFlightLine.
  ///
  /// In ar, this message translates to:
  /// **'رحلة {flight}'**
  String airportFlightLine(String flight);

  /// No description provided for @airportNoAirports.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد مطارات متاحة حالياً'**
  String get airportNoAirports;

  /// No description provided for @airportNoDetails.
  ///
  /// In ar, this message translates to:
  /// **'لم تحدد الصالة بعد'**
  String get airportNoDetails;

  /// No description provided for @airportPickupZoneRequiredError.
  ///
  /// In ar, this message translates to:
  /// **'اختر منطقة الالتقاط في المطار'**
  String get airportPickupZoneRequiredError;

  /// No description provided for @airportQueueCopy.
  ///
  /// In ar, this message translates to:
  /// **'تُوزَّع رحلات المطار على الكباتن بترتيب الوصول: الأول فالأول.'**
  String get airportQueueCopy;

  /// No description provided for @airportQueueEligible.
  ///
  /// In ar, this message translates to:
  /// **'أنت داخل منطقة انتظار {airport}'**
  String airportQueueEligible(String airport);

  /// No description provided for @airportQueueExitNote.
  ///
  /// In ar, this message translates to:
  /// **'إذا غادرت منطقة الانتظار أو انقطع اتصالك لفترة تخرج من الطابور تلقائياً.'**
  String get airportQueueExitNote;

  /// No description provided for @airportQueueEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'المطار'**
  String get airportQueueEyebrow;

  /// No description provided for @airportQueueJoin.
  ///
  /// In ar, this message translates to:
  /// **'انضم للطابور'**
  String get airportQueueJoin;

  /// No description provided for @airportQueueJoinHint.
  ///
  /// In ar, this message translates to:
  /// **'انضم لاستلام رحلات المطار بالترتيب'**
  String get airportQueueJoinHint;

  /// No description provided for @airportQueueLeave.
  ///
  /// In ar, this message translates to:
  /// **'غادر الطابور'**
  String get airportQueueLeave;

  /// No description provided for @airportQueueNotNearby.
  ///
  /// In ar, this message translates to:
  /// **'أنت لست في منطقة انتظار مطار حالياً'**
  String get airportQueueNotNearby;

  /// No description provided for @airportQueuePosition.
  ///
  /// In ar, this message translates to:
  /// **'ترتيبك {position} من {total}'**
  String airportQueuePosition(int position, int total);

  /// No description provided for @airportQueueTitle.
  ///
  /// In ar, this message translates to:
  /// **'طابور المطار'**
  String get airportQueueTitle;

  /// No description provided for @airportQueueWait.
  ///
  /// In ar, this message translates to:
  /// **'الانتظار المتوقع نحو {minutes} دقيقة'**
  String airportQueueWait(int minutes);

  /// No description provided for @airportRemove.
  ///
  /// In ar, this message translates to:
  /// **'إزالة'**
  String get airportRemove;

  /// No description provided for @airportRowCopy.
  ///
  /// In ar, this message translates to:
  /// **'اختر المطار والصالة ورقم الرحلة'**
  String get airportRowCopy;

  /// No description provided for @airportRowTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلة من أو إلى مطار؟'**
  String get airportRowTitle;

  /// No description provided for @airportSheetTitle.
  ///
  /// In ar, this message translates to:
  /// **'المطار'**
  String get airportSheetTitle;

  /// No description provided for @airportTerminalLabel.
  ///
  /// In ar, this message translates to:
  /// **'صالة {code}'**
  String airportTerminalLabel(String code);

  /// No description provided for @airportWaitingFree.
  ///
  /// In ar, this message translates to:
  /// **'انتظار مجاني {minutes} د'**
  String airportWaitingFree(int minutes);

  /// No description provided for @airportWaitingPolicyCopy.
  ///
  /// In ar, this message translates to:
  /// **'تحصل على {minutes} دقيقة انتظار مجاناً عند الالتقاط، وبعدها تُحتسب رسوم الانتظار.'**
  String airportWaitingPolicyCopy(int minutes);

  /// No description provided for @airportWaitingPolicyTitle.
  ///
  /// In ar, this message translates to:
  /// **'سياسة الانتظار'**
  String get airportWaitingPolicyTitle;

  /// No description provided for @countdownDaysHours.
  ///
  /// In ar, this message translates to:
  /// **'{days} يوم و{hours} ساعة'**
  String countdownDaysHours(int days, int hours);

  /// No description provided for @countdownHoursMinutes.
  ///
  /// In ar, this message translates to:
  /// **'{hours} ساعة و{minutes} دقيقة'**
  String countdownHoursMinutes(int hours, int minutes);

  /// No description provided for @driverScheduledCopy.
  ///
  /// In ar, this message translates to:
  /// **'طلبات مجدولة قادمة في منطقتك. احجز ما يناسبك وأكّد قبل الموعد.'**
  String get driverScheduledCopy;

  /// No description provided for @driverScheduledEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'الحجوزات'**
  String get driverScheduledEyebrow;

  /// No description provided for @driverScheduledLinkCopy.
  ///
  /// In ar, this message translates to:
  /// **'السوق وحجوزاتك القادمة'**
  String get driverScheduledLinkCopy;

  /// No description provided for @driverScheduledNext.
  ///
  /// In ar, this message translates to:
  /// **'أقرب حجز: {time}'**
  String driverScheduledNext(String time);

  /// No description provided for @driverScheduledTabMarket.
  ///
  /// In ar, this message translates to:
  /// **'السوق'**
  String get driverScheduledTabMarket;

  /// No description provided for @driverScheduledTabMine.
  ///
  /// In ar, this message translates to:
  /// **'حجوزاتي'**
  String get driverScheduledTabMine;

  /// No description provided for @driverScheduledTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلاتي المجدولة'**
  String get driverScheduledTitle;

  /// No description provided for @marketAirportBadge.
  ///
  /// In ar, this message translates to:
  /// **'مطار'**
  String get marketAirportBadge;

  /// No description provided for @marketApproxPickup.
  ///
  /// In ar, this message translates to:
  /// **'موقع الالتقاط تقريبي حتى الحجز'**
  String get marketApproxPickup;

  /// No description provided for @marketDayAll.
  ///
  /// In ar, this message translates to:
  /// **'الكل'**
  String get marketDayAll;

  /// No description provided for @marketDistanceToPickup.
  ///
  /// In ar, this message translates to:
  /// **'{km} كم عنك'**
  String marketDistanceToPickup(String km);

  /// No description provided for @marketEmptyCopy.
  ///
  /// In ar, this message translates to:
  /// **'نحدّث القائمة تلقائياً كل دقيقة'**
  String get marketEmptyCopy;

  /// No description provided for @marketEmptyTitle.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد طلبات مجدولة الآن'**
  String get marketEmptyTitle;

  /// No description provided for @marketExclusiveUntil.
  ///
  /// In ar, this message translates to:
  /// **'حصرية لك حتى {time}'**
  String marketExclusiveUntil(String time);

  /// No description provided for @marketFare.
  ///
  /// In ar, this message translates to:
  /// **'الأجرة {price}'**
  String marketFare(String price);

  /// No description provided for @marketLoadMore.
  ///
  /// In ar, this message translates to:
  /// **'عرض المزيد'**
  String get marketLoadMore;

  /// No description provided for @marketNetEarnings.
  ///
  /// In ar, this message translates to:
  /// **'صافي أرباحك'**
  String get marketNetEarnings;

  /// No description provided for @marketReserve.
  ///
  /// In ar, this message translates to:
  /// **'احجز الرحلة'**
  String get marketReserve;

  /// No description provided for @marketReserved.
  ///
  /// In ar, this message translates to:
  /// **'تم حجز الرحلة، ستجدها في حجوزاتي'**
  String get marketReserved;

  /// No description provided for @notInAirportWaitingAreaError.
  ///
  /// In ar, this message translates to:
  /// **'يجب أن تكون داخل منطقة انتظار المطار'**
  String get notInAirportWaitingAreaError;

  /// No description provided for @reservationConfirmAction.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد'**
  String get reservationConfirmAction;

  /// No description provided for @reservationConfirmCopy.
  ///
  /// In ar, this message translates to:
  /// **'رحلة مجدولة {time}. أكّد قبل انتهاء المهلة وإلا سيُحرَّر الحجز.'**
  String reservationConfirmCopy(String time);

  /// No description provided for @reservationConfirmFinalTitle.
  ///
  /// In ar, this message translates to:
  /// **'التأكيد النهائي: استعد للانطلاق'**
  String get reservationConfirmFinalTitle;

  /// No description provided for @reservationConfirmFirstTitle.
  ///
  /// In ar, this message translates to:
  /// **'أكّد حجزك'**
  String get reservationConfirmFirstTitle;

  /// No description provided for @reservationConfirmLeft.
  ///
  /// In ar, this message translates to:
  /// **'المتبقي {time}'**
  String reservationConfirmLeft(String time);

  /// No description provided for @reservationConfirmed.
  ///
  /// In ar, this message translates to:
  /// **'تم تأكيد الحجز'**
  String get reservationConfirmed;

  /// No description provided for @reservationConflictError.
  ///
  /// In ar, this message translates to:
  /// **'يتعارض الموعد مع رحلة محجوزة لديك'**
  String get reservationConflictError;

  /// No description provided for @reservationDetailEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'حجز مجدول'**
  String get reservationDetailEyebrow;

  /// No description provided for @reservationFare.
  ///
  /// In ar, this message translates to:
  /// **'الأجرة التقديرية'**
  String get reservationFare;

  /// No description provided for @reservationFinalConfirmed.
  ///
  /// In ar, this message translates to:
  /// **'تم التأكيد النهائي، ستبدأ الرحلة في موعدها'**
  String get reservationFinalConfirmed;

  /// No description provided for @reservationFreeReleaseLabel.
  ///
  /// In ar, this message translates to:
  /// **'تحرير مجاني حتى'**
  String get reservationFreeReleaseLabel;

  /// No description provided for @reservationLimitReachedError.
  ///
  /// In ar, this message translates to:
  /// **'وصلت للحد الأقصى من الحجوزات'**
  String get reservationLimitReachedError;

  /// No description provided for @reservationNotConfirmableError.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكن التأكيد الآن'**
  String get reservationNotConfirmableError;

  /// No description provided for @reservationNotConfirmableOfflineError.
  ///
  /// In ar, this message translates to:
  /// **'فعّل حالة «متصل» لتأكيد الحجز'**
  String get reservationNotConfirmableOfflineError;

  /// No description provided for @reservationNotConfirmableOnTripError.
  ///
  /// In ar, this message translates to:
  /// **'لديك رحلة جارية، أنهِها أولاً لتأكيد الحجز'**
  String get reservationNotConfirmableOnTripError;

  /// No description provided for @reservationNotFound.
  ///
  /// In ar, this message translates to:
  /// **'لم يتم العثور على هذا الحجز'**
  String get reservationNotFound;

  /// No description provided for @reservationPassenger.
  ///
  /// In ar, this message translates to:
  /// **'الراكب'**
  String get reservationPassenger;

  /// No description provided for @reservationReleaseAction.
  ///
  /// In ar, this message translates to:
  /// **'تحرير الحجز'**
  String get reservationReleaseAction;

  /// No description provided for @reservationReleaseConfirm.
  ///
  /// In ar, this message translates to:
  /// **'نعم، حرّر'**
  String get reservationReleaseConfirm;

  /// No description provided for @reservationReleaseFreeCopy.
  ///
  /// In ar, this message translates to:
  /// **'التحرير مجاني حتى {time}.'**
  String reservationReleaseFreeCopy(String time);

  /// No description provided for @reservationReleaseLateCopy.
  ///
  /// In ar, this message translates to:
  /// **'انتهت مدة التحرير المجاني؛ سيتم خصم نقاط من موثوقيتك، وقد يؤثر ذلك على أولوية الطلبات والحوافز.'**
  String get reservationReleaseLateCopy;

  /// No description provided for @reservationReleaseTitle.
  ///
  /// In ar, this message translates to:
  /// **'تحرير الحجز؟'**
  String get reservationReleaseTitle;

  /// No description provided for @reservationReleased.
  ///
  /// In ar, this message translates to:
  /// **'تم تحرير الحجز'**
  String get reservationReleased;

  /// No description provided for @reservationReleasedPoints.
  ///
  /// In ar, this message translates to:
  /// **'تم تحرير الحجز وخُصمت {points} نقاط من موثوقيتك'**
  String reservationReleasedPoints(int points);

  /// No description provided for @reservationStatusAssigned.
  ///
  /// In ar, this message translates to:
  /// **'مسندة إليك'**
  String get reservationStatusAssigned;

  /// No description provided for @reservationStatusCancelled.
  ///
  /// In ar, this message translates to:
  /// **'ملغاة'**
  String get reservationStatusCancelled;

  /// No description provided for @reservationStatusCompleted.
  ///
  /// In ar, this message translates to:
  /// **'مكتملة'**
  String get reservationStatusCompleted;

  /// No description provided for @reservationStatusConfirmed.
  ///
  /// In ar, this message translates to:
  /// **'مؤكدة'**
  String get reservationStatusConfirmed;

  /// No description provided for @reservationStatusNoShow.
  ///
  /// In ar, this message translates to:
  /// **'عدم حضور'**
  String get reservationStatusNoShow;

  /// No description provided for @reservationStatusReleased.
  ///
  /// In ar, this message translates to:
  /// **'مُحرَّرة'**
  String get reservationStatusReleased;

  /// No description provided for @reservationStatusReserved.
  ///
  /// In ar, this message translates to:
  /// **'محجوزة'**
  String get reservationStatusReserved;

  /// No description provided for @reservationTakenError.
  ///
  /// In ar, this message translates to:
  /// **'تم حجز هذه الرحلة من كابتن آخر'**
  String get reservationTakenError;

  /// No description provided for @reservationsActive.
  ///
  /// In ar, this message translates to:
  /// **'القادمة'**
  String get reservationsActive;

  /// No description provided for @reservationsEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد حجوزات هنا'**
  String get reservationsEmpty;

  /// No description provided for @reservationsHistory.
  ///
  /// In ar, this message translates to:
  /// **'السابقة'**
  String get reservationsHistory;

  /// No description provided for @scheduleBooked.
  ///
  /// In ar, this message translates to:
  /// **'تم حجز رحلتك المجدولة'**
  String get scheduleBooked;

  /// No description provided for @scheduleConfirm.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد الموعد'**
  String get scheduleConfirm;

  /// No description provided for @scheduleConfirmedExpired.
  ///
  /// In ar, this message translates to:
  /// **'لم يعد الموعد ضمن المدة المتاحة، اختر موعداً جديداً'**
  String get scheduleConfirmedExpired;

  /// No description provided for @scheduleConfirmedLabel.
  ///
  /// In ar, this message translates to:
  /// **'موعد الرحلة'**
  String get scheduleConfirmedLabel;

  /// No description provided for @scheduleDayHeading.
  ///
  /// In ar, this message translates to:
  /// **'اليوم'**
  String get scheduleDayHeading;

  /// No description provided for @scheduleEdit.
  ///
  /// In ar, this message translates to:
  /// **'تعديل'**
  String get scheduleEdit;

  /// No description provided for @scheduleFavoritePriority.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن المفضل {name} يحصل على أولوية حجز رحلتك قبل بقية الكباتن'**
  String scheduleFavoritePriority(String name);

  /// No description provided for @scheduleFixedPrice.
  ///
  /// In ar, this message translates to:
  /// **'سعر ثابت للموعد · بدون رسوم ذروة'**
  String get scheduleFixedPrice;

  /// No description provided for @scheduleHourHeading.
  ///
  /// In ar, this message translates to:
  /// **'الساعة'**
  String get scheduleHourHeading;

  /// No description provided for @scheduleLeadTooShortError.
  ///
  /// In ar, this message translates to:
  /// **'يجب أن يكون الموعد بعد 30 دقيقة على الأقل'**
  String get scheduleLeadTooShortError;

  /// No description provided for @scheduleLeadTooShortUntilError.
  ///
  /// In ar, this message translates to:
  /// **'أقرب موعد متاح {time}'**
  String scheduleLeadTooShortUntilError(String time);

  /// No description provided for @scheduleMinuteHeading.
  ///
  /// In ar, this message translates to:
  /// **'الدقيقة'**
  String get scheduleMinuteHeading;

  /// No description provided for @scheduleNothingPicked.
  ///
  /// In ar, this message translates to:
  /// **'اختر اليوم والوقت'**
  String get scheduleNothingPicked;

  /// No description provided for @schedulePickerCopy.
  ///
  /// In ar, this message translates to:
  /// **'احجز بعد {lead} على الأقل وحتى {window} من الآن'**
  String schedulePickerCopy(String lead, String window);

  /// No description provided for @schedulePickerTitle.
  ///
  /// In ar, this message translates to:
  /// **'متى تريد رحلتك؟'**
  String get schedulePickerTitle;

  /// No description provided for @scheduleRide.
  ///
  /// In ar, this message translates to:
  /// **'احجز {name} · {price}'**
  String scheduleRide(String name, String price);

  /// No description provided for @scheduleRulesFallback.
  ///
  /// In ar, this message translates to:
  /// **'تعذر تحميل شروط الجدولة، نستخدم القيم الافتراضية.'**
  String get scheduleRulesFallback;

  /// No description provided for @scheduleSelectedLabel.
  ///
  /// In ar, this message translates to:
  /// **'الموعد المختار'**
  String get scheduleSelectedLabel;

  /// No description provided for @scheduleTooFar.
  ///
  /// In ar, this message translates to:
  /// **'أبعد موعد متاح {time}'**
  String scheduleTooFar(String time);

  /// No description provided for @scheduleTooSoon.
  ///
  /// In ar, this message translates to:
  /// **'أقرب موعد متاح {time}'**
  String scheduleTooSoon(String time);

  /// No description provided for @scheduleWindowDays.
  ///
  /// In ar, this message translates to:
  /// **'{days, plural, =1{يوم واحد} =2{يومين} few{{days} أيام} other{{days} يوماً}}'**
  String scheduleWindowDays(int days);

  /// No description provided for @scheduleWindowExceededError.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكن الجدولة لأكثر من 7 أيام من الآن'**
  String get scheduleWindowExceededError;

  /// No description provided for @scheduleWindowExceededUntilError.
  ///
  /// In ar, this message translates to:
  /// **'لا يمكن الجدولة بعد {time}'**
  String scheduleWindowExceededUntilError(String time);

  /// No description provided for @scheduledBookNew.
  ///
  /// In ar, this message translates to:
  /// **'احجز رحلة'**
  String get scheduledBookNew;

  /// No description provided for @scheduledCancelButton.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الحجز'**
  String get scheduledCancelButton;

  /// No description provided for @scheduledCancelledTitle.
  ///
  /// In ar, this message translates to:
  /// **'تم إلغاء الحجز'**
  String get scheduledCancelledTitle;

  /// No description provided for @scheduledCountdownLabel.
  ///
  /// In ar, this message translates to:
  /// **'يبدأ بعد'**
  String get scheduledCountdownLabel;

  /// No description provided for @scheduledDetailEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'رحلة مجدولة'**
  String get scheduledDetailEyebrow;

  /// No description provided for @scheduledDriverConfirmedBadge.
  ///
  /// In ar, this message translates to:
  /// **'مؤكد'**
  String get scheduledDriverConfirmedBadge;

  /// No description provided for @scheduledDriverReservedBadge.
  ///
  /// In ar, this message translates to:
  /// **'محجوز'**
  String get scheduledDriverReservedBadge;

  /// No description provided for @scheduledDriverTitle.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن المحجوز'**
  String get scheduledDriverTitle;

  /// No description provided for @scheduledEmptyCopy.
  ///
  /// In ar, this message translates to:
  /// **'احجز رحلتك من الرئيسية واختر «جدولة»'**
  String get scheduledEmptyCopy;

  /// No description provided for @scheduledEmptyTitle.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد رحلات مجدولة'**
  String get scheduledEmptyTitle;

  /// No description provided for @scheduledFareLabel.
  ///
  /// In ar, this message translates to:
  /// **'الأجرة التقديرية'**
  String get scheduledFareLabel;

  /// No description provided for @scheduledLateCancelNote.
  ///
  /// In ar, this message translates to:
  /// **'انتهت مدة الإلغاء المجاني، وقد تُطبَّق رسوم عند الإلغاء (ستراها قبل التأكيد).'**
  String get scheduledLateCancelNote;

  /// No description provided for @scheduledLimitReachedError.
  ///
  /// In ar, this message translates to:
  /// **'وصلت للحد الأقصى من الرحلات المجدولة'**
  String get scheduledLimitReachedError;

  /// No description provided for @scheduledLimitReachedMaxError.
  ///
  /// In ar, this message translates to:
  /// **'وصلت للحد الأقصى من الرحلات المجدولة ({max})'**
  String scheduledLimitReachedMaxError(int max);

  /// No description provided for @scheduledLinkCopy.
  ///
  /// In ar, this message translates to:
  /// **'حجوزاتك القادمة وحالتها'**
  String get scheduledLinkCopy;

  /// No description provided for @scheduledNoDriverYet.
  ///
  /// In ar, this message translates to:
  /// **'لم يحجز أي كابتن رحلتك بعد، وسنبدأ البحث تلقائياً قبل الموعد.'**
  String get scheduledNoDriverYet;

  /// No description provided for @scheduledPhaseConfirmed.
  ///
  /// In ar, this message translates to:
  /// **'الكابتن مؤكد'**
  String get scheduledPhaseConfirmed;

  /// No description provided for @scheduledPhaseEnded.
  ///
  /// In ar, this message translates to:
  /// **'انتهت'**
  String get scheduledPhaseEnded;

  /// No description provided for @scheduledPhaseInProgress.
  ///
  /// In ar, this message translates to:
  /// **'الرحلة جارية'**
  String get scheduledPhaseInProgress;

  /// No description provided for @scheduledPhaseReserved.
  ///
  /// In ar, this message translates to:
  /// **'تم حجزها من كابتن'**
  String get scheduledPhaseReserved;

  /// No description provided for @scheduledPhaseSearching.
  ///
  /// In ar, this message translates to:
  /// **'جارٍ البحث عن كابتن'**
  String get scheduledPhaseSearching;

  /// No description provided for @scheduledPhaseWaiting.
  ///
  /// In ar, this message translates to:
  /// **'بانتظار كابتن'**
  String get scheduledPhaseWaiting;

  /// No description provided for @scheduledSearchStartedCopy.
  ///
  /// In ar, this message translates to:
  /// **'بدأ البحث عن كابتن لرحلتك، تابعها من شاشة الرحلة.'**
  String get scheduledSearchStartedCopy;

  /// No description provided for @scheduledSearchStartsAt.
  ///
  /// In ar, this message translates to:
  /// **'إن لم يحجزها كابتن سنبدأ البحث {time}'**
  String scheduledSearchStartsAt(String time);

  /// No description provided for @scheduledTrackTrip.
  ///
  /// In ar, this message translates to:
  /// **'تتبع الرحلة'**
  String get scheduledTrackTrip;

  /// No description provided for @scheduledTripTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلة مجدولة'**
  String get scheduledTripTitle;

  /// No description provided for @scheduledTripsCopy.
  ///
  /// In ar, this message translates to:
  /// **'رحلاتك القادمة مرتبة حسب الموعد'**
  String get scheduledTripsCopy;

  /// No description provided for @scheduledTripsEyebrow.
  ///
  /// In ar, this message translates to:
  /// **'الحجوزات'**
  String get scheduledTripsEyebrow;

  /// No description provided for @scheduledTripsTitle.
  ///
  /// In ar, this message translates to:
  /// **'رحلاتي المجدولة'**
  String get scheduledTripsTitle;

  /// No description provided for @tripAirportDropoff.
  ///
  /// In ar, this message translates to:
  /// **'توصيل إلى مطار {code}'**
  String tripAirportDropoff(String code);

  /// No description provided for @tripAirportPickup.
  ///
  /// In ar, this message translates to:
  /// **'استلام من مطار {code}'**
  String tripAirportPickup(String code);

  /// No description provided for @tripStatusScheduled.
  ///
  /// In ar, this message translates to:
  /// **'مجدولة'**
  String get tripStatusScheduled;

  /// No description provided for @reservationOpenMarket.
  ///
  /// In ar, this message translates to:
  /// **'افتح السوق'**
  String get reservationOpenMarket;

  /// No description provided for @reservationNotConfirmableNotDueError.
  ///
  /// In ar, this message translates to:
  /// **'لم يحن وقت التأكيد بعد'**
  String get reservationNotConfirmableNotDueError;

  /// No description provided for @reservationNotConfirmableExpiredError.
  ///
  /// In ar, this message translates to:
  /// **'انتهت مهلة التأكيد'**
  String get reservationNotConfirmableExpiredError;
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
