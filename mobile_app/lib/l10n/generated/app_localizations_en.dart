// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'ATA';

  @override
  String get back => 'Back';

  @override
  String get cancel => 'Cancel';

  @override
  String get retry => 'Retry';

  @override
  String get currency => 'SAR';

  @override
  String priceWithCurrency(String amount) {
    return '$amount SAR';
  }

  @override
  String minutesLabel(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count mins',
      one: '1 min',
      zero: 'Now',
    );
    return '$_temp0';
  }

  @override
  String get errorNetwork =>
      'Could not reach the server. Check your internet connection.';

  @override
  String get errorUnexpected => 'Something went wrong. Please try again.';

  @override
  String get errorUnauthorized => 'Your session expired. Please sign in again.';

  @override
  String get loading => 'Loading…';

  @override
  String get comingSoon => 'Coming soon';

  @override
  String get keypadDelete => 'Delete';

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
  String get roleEyebrow => 'Start your journey with ATA';

  @override
  String get roleTitle => 'How would you like to use ATA?';

  @override
  String get roleCopy =>
      'Choose an account type to continue with your phone number';

  @override
  String get riderTag => 'For riders';

  @override
  String get riderTitle => 'Sign up as a rider';

  @override
  String get riderCopy =>
      'Request a ride in minutes and track your driver until arrival.';

  @override
  String get riderCta => 'Get started';

  @override
  String get driverTag => 'For drivers';

  @override
  String get driverTitle => 'Sign up as a driver';

  @override
  String get driverCopy =>
      'Register your number, then upload your documents online for approval.';

  @override
  String get driverCta => 'Join ATA';

  @override
  String get roleTerms =>
      'By continuing, you agree to our Terms of Use and Privacy Policy';

  @override
  String get phoneTitle => 'Enter your phone number';

  @override
  String get phoneCopyRider =>
      'We will send you a verification code to create your account.';

  @override
  String get phoneCopyDriver =>
      'We will send you a verification code to start your driver application.';

  @override
  String get phonePlaceholder => '5X XXX XXXX';

  @override
  String get phoneCountryCode => '+966';

  @override
  String get phoneSend => 'Send verification code';

  @override
  String get phoneInvalid =>
      'Enter a valid Saudi mobile number starting with 5';

  @override
  String rateLimited(int seconds) {
    return 'Too many attempts. Try again in $seconds seconds';
  }

  @override
  String get otpTitle => 'Verify your number';

  @override
  String get otpCopy => 'We sent a 4-digit code to';

  @override
  String get otpVerify => 'Verify and continue';

  @override
  String get otpResend => 'Resend code';

  @override
  String otpResendIn(int seconds) {
    return 'Resend in ${seconds}s';
  }

  @override
  String otpDevHint(String code) {
    return 'Dev code: $code';
  }

  @override
  String otpInvalid(int attempts) {
    return 'Incorrect code. Attempts left: $attempts';
  }

  @override
  String get otpExpired => 'The code has expired. Request a new one.';

  @override
  String get otpLocked =>
      'Verification is temporarily locked. Try again later.';

  @override
  String get termsEyebrow => 'One last step';

  @override
  String get termsTitle => 'What is your name?';

  @override
  String get termsCopy =>
      'Tell us the name your captain should use and accept the terms to continue.';

  @override
  String get fullNameLabel => 'Full name';

  @override
  String get fullNameHint => 'e.g. Abdullah Mohammed';

  @override
  String get acceptTermsLabel =>
      'I agree to the Terms of Use and Privacy Policy';

  @override
  String get termsContinue => 'Start using ATA';

  @override
  String get pendingTitle => 'Your application was created';

  @override
  String get pendingCopy =>
      'Your driver account is not active yet. Upload your documents on the ATA website so our team can review them and activate your account.';

  @override
  String get pendingReviewTitle => 'Your application is under review';

  @override
  String get pendingReviewCopy =>
      'Our team is reviewing your documents. We will notify you on your phone as soon as your account is activated.';

  @override
  String get pendingRejectedTitle => 'Application rejected';

  @override
  String get pendingRejectedCopy =>
      'Check the rejection reason, update your documents on the ATA website and submit again.';

  @override
  String get pendingSuspendedTitle => 'Account suspended';

  @override
  String get pendingSuspendedCopy =>
      'Your account has been temporarily suspended. Contact driver support for details.';

  @override
  String get pendingApprovedTitle => 'Your account is active';

  @override
  String get pendingApprovedCopy =>
      'Your driver account is now active. Open the driver portal to start receiving rides.';

  @override
  String get applicationNumber => 'Application number';

  @override
  String get step1Title => 'Upload documents';

  @override
  String get step1Copy => 'On the ATA website';

  @override
  String get step2Title => 'Admin review';

  @override
  String get step2Copy => 'Within 24–48 hours';

  @override
  String get step3Title => 'Account activation';

  @override
  String get step3Copy => 'Notification on your phone';

  @override
  String get requiredDocuments => 'Required documents';

  @override
  String get docStatusRequired => 'Required';

  @override
  String get docStatusPending => 'Under review';

  @override
  String get docStatusVerified => 'Accepted';

  @override
  String get docStatusRejected => 'Rejected';

  @override
  String get docNationalId => 'National ID or Iqama';

  @override
  String get docDrivingLicense => 'Valid driving license';

  @override
  String get docVehicleRegistration => 'Vehicle registration';

  @override
  String get docPersonalPhoto => 'Clear personal photo';

  @override
  String get openUploadPortal => 'Go to the document upload site';

  @override
  String get openDriverPortal => 'Open the driver portal';

  @override
  String get backToLogin => 'Back to sign in';

  @override
  String get refreshStatus => 'Refresh status';

  @override
  String rejectionReason(String reason) {
    return 'Rejection reason: $reason';
  }

  @override
  String get portalOpenFailed => 'Could not open the link';

  @override
  String get navHome => 'Home';

  @override
  String get navRides => 'My rides';

  @override
  String get navWallet => 'Wallet';

  @override
  String get navAccount => 'Account';

  @override
  String get notificationsTitle => 'Notifications';

  @override
  String get manageNotifications => 'Manage notifications';

  @override
  String get notificationsEmpty => 'No new notifications';

  @override
  String get markAllRead => 'Mark all as read';

  @override
  String get menuViewProfile => 'View profile';

  @override
  String get menuSettings => 'Settings';

  @override
  String get menuAccountSettings => 'Account settings';

  @override
  String get menuLanguage => 'Language';

  @override
  String get menuNotifications => 'Notifications';

  @override
  String get menuSafety => 'Safety and privacy';

  @override
  String get menuContact => 'Contact us';

  @override
  String get guestName => 'ATA rider';

  @override
  String get homeEyebrow => 'Welcome to ATA';

  @override
  String get homeTitle => 'Where would you like to go?';

  @override
  String get pickupLabel => 'Pickup';

  @override
  String get pickupCurrent => 'Your current location';

  @override
  String get stopLabel => 'Extra stop';

  @override
  String get removeStop => 'Remove';

  @override
  String get destinationLabel => 'Destination';

  @override
  String get destinationDefault => 'Riyadh Front';

  @override
  String get addStop => 'Add another stop';

  @override
  String get maxStopsReached => 'Maximum number of stops added';

  @override
  String get stopOption1 => 'Al Nakheel Mall';

  @override
  String get stopOption2 => 'Kingdom Tower';

  @override
  String get stopOption3 => 'King Abdullah Park';

  @override
  String get timeNow => 'Now';

  @override
  String get timeSchedule => 'Schedule';

  @override
  String get femaleDriverTitle => 'I prefer a female driver';

  @override
  String get femaleDriverTag => 'For women';

  @override
  String get femaleDriverCopy =>
      'Lets women request a female driver when one is available';

  @override
  String get chooseRide => 'Choose your ride';

  @override
  String get pricesEstimated => 'Prices are estimates';

  @override
  String get paymentMethod => 'Payment method';

  @override
  String get paymentCash => 'Cash';

  @override
  String get paymentWallet => 'ATA wallet';

  @override
  String get paymentCard => 'Card';

  @override
  String requestRide(String name, String price) {
    return 'Request $name · $price';
  }

  @override
  String get safeRide => 'Your ride is safe and monitored around the clock';

  @override
  String get searchingTitle => 'Finding your captain';

  @override
  String searchingCopy(String eta) {
    return 'We are finding the nearest captain. They will reach you in $eta.';
  }

  @override
  String get cashOnArrival => 'Pay cash on arrival';

  @override
  String get payWithWallet => 'Pay from ATA wallet';

  @override
  String get payWithCard => 'Pay by card';

  @override
  String extraStopsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count extra stops',
      one: '1 extra stop',
    );
    return '$_temp0';
  }

  @override
  String get femaleRequestedTitle => 'Female driver requested';

  @override
  String get femaleRequestedCopy =>
      'We will look for the nearest available female driver';

  @override
  String get cancelRequest => 'Cancel request';

  @override
  String get categoriesError => 'Could not load ride categories';

  @override
  String get ridesEyebrow => 'Your activity';

  @override
  String get ridesTitle => 'My rides';

  @override
  String get ridesCopy =>
      'Review your past rides, payment details, and book the same trip again.';

  @override
  String get recentTrips => 'Recent trips';

  @override
  String get allTrips => 'All';

  @override
  String get tripStatusCompleted => 'Completed';

  @override
  String get tripStatusCancelled => 'Cancelled';

  @override
  String get tripStatusActive => 'In progress';

  @override
  String get ridesEmptyTitle => 'No rides yet';

  @override
  String get ridesEmptyCopy =>
      'Your rides will appear here after your first request.';

  @override
  String get promoTitle => 'Your usual destination, closer';

  @override
  String get promoCopy => 'Copy';

  @override
  String get promoCta => 'Book a ride now';

  @override
  String tripRoute(String pickup, String destination) {
    return '$pickup → $destination';
  }

  @override
  String get walletEyebrow => 'Secure payments';

  @override
  String get walletTitle => 'ATA wallet';

  @override
  String get walletCopy =>
      'Control your balance and payment methods, and review all your transactions in one place.';

  @override
  String get currentBalance => 'Current balance';

  @override
  String get paymentMethods => 'Payment methods';

  @override
  String get topUp => 'Top up wallet';

  @override
  String walletBalanceLine(String amount) {
    return 'Balance: $amount';
  }

  @override
  String get madaCard => 'mada card';

  @override
  String cardEnding(String digits) {
    return 'Ending in $digits';
  }

  @override
  String get payCash => 'Pay in cash';

  @override
  String get payCashCopy => 'Pay the captain after the ride';

  @override
  String get topUpCopy => 'Choose the amount to add to your ATA balance.';

  @override
  String get topUpAmount => 'Top-up amount';

  @override
  String confirmTopUp(String amount) {
    return 'Confirm top-up of $amount';
  }

  @override
  String get topUpSuccessTitle => 'Wallet topped up';

  @override
  String topUpSuccessCopy(String amount) {
    return '$amount was added to your wallet balance.';
  }

  @override
  String get newBalance => 'New balance';

  @override
  String get backToWallet => 'Back to wallet';

  @override
  String get sandboxMethod => 'Sandbox payment';

  @override
  String get sandboxCopy => 'Test environment, nothing is charged';

  @override
  String get walletError => 'Could not load the wallet';

  @override
  String get safetyEyebrow => 'Safety first';

  @override
  String get safetyTitle => 'Your safety on every ride';

  @override
  String get safetyCopy =>
      'Smart tools and an always-available support team for a reassuring experience from pickup to arrival.';

  @override
  String get shareTripTitle => 'Share your trip';

  @override
  String get shareTripCopy =>
      'Send your route and captain details to people you trust.';

  @override
  String get helpCenterTitle => 'Help center';

  @override
  String get helpCenterCopy =>
      'Talk directly to the safety team around the clock.';

  @override
  String get trustedContactsTitle => 'Trusted contacts';

  @override
  String get trustedContactsCopy => 'Add people to be alerted when needed.';

  @override
  String get learnMore => 'Learn more';

  @override
  String get emergencyTitle => 'Need urgent help?';

  @override
  String get emergencyCopy => 'The safety team is available now';

  @override
  String get contactUs => 'Contact us';

  @override
  String get accountEyebrow => 'My account';

  @override
  String accountWelcome(String name) {
    return 'Welcome, $name';
  }

  @override
  String get accountCopy =>
      'Manage your details, ride settings, and privacy preferences.';

  @override
  String memberSince(String year) {
    return 'Member since $year';
  }

  @override
  String get passengerRating => 'Rider rating';

  @override
  String get settings => 'Settings';

  @override
  String get personalInfo => 'Personal details';

  @override
  String get personalInfoCopy => 'Name, phone number and email';

  @override
  String get savedPlaces => 'Saved places';

  @override
  String get savedPlacesCopy => 'Home and work';

  @override
  String get privacySecurity => 'Privacy and security';

  @override
  String get privacySecurityCopy => 'Manage your data and permissions';

  @override
  String get notificationsRow => 'Notifications';

  @override
  String get notificationsRowCopy => 'Manage alerts and offers';

  @override
  String get languageRow => 'Language';

  @override
  String get languageArabic => 'العربية';

  @override
  String get languageEnglish => 'English';

  @override
  String get contactRow => 'Contact us';

  @override
  String get contactRowCopy => 'Support and help';

  @override
  String get termsRow => 'Terms and conditions';

  @override
  String get termsRowCopy => 'ATA service terms';

  @override
  String get logout => 'Log out';

  @override
  String get deleteApp => 'Delete app';

  @override
  String get backToSettings => 'Back to settings';

  @override
  String get languagePanelEyebrow => 'Preferences';

  @override
  String get languagePanelTitle => 'App language';

  @override
  String get languagePanelCopy =>
      'Choose the language you prefer inside the ATA app.';

  @override
  String get languageArabicCopy => 'العربية — المملكة العربية السعودية';

  @override
  String get languageEnglishCopy => 'English — United Kingdom';

  @override
  String get languageSavedNote =>
      'Your language choice is saved automatically and used the next time you open the app.';

  @override
  String get notifPrefsEyebrow => 'Stay informed';

  @override
  String get notifPrefsTitle => 'Notifications';

  @override
  String get notifPrefsCopy =>
      'Choose the alerts you want to receive from ATA.';

  @override
  String get prefTripsTitle => 'Trip alerts';

  @override
  String get prefTripsCopy => 'Request status, driver arrival and trip updates';

  @override
  String get prefWalletTitle => 'Wallet and payments';

  @override
  String get prefWalletCopy => 'Top-ups, charges and trip receipts';

  @override
  String get prefSafetyTitle => 'Safety alerts';

  @override
  String get prefSafetyCopy => 'Important alerts and security updates';

  @override
  String get prefOffersTitle => 'Offers and news';

  @override
  String get prefOffersCopy => 'Exclusive discounts and offers from ATA';

  @override
  String get contactEyebrow => 'We are here to help';

  @override
  String get contactTitle => 'Contact us';

  @override
  String get contactCopy => 'Pick the best way to reach the ATA support team.';

  @override
  String get liveChatTitle => 'Live chat';

  @override
  String get liveChatValue => 'Available now';

  @override
  String get liveChatCopy => 'Start a chat';

  @override
  String get callTitle => 'Call us';

  @override
  String get callValue => '9200 123 45';

  @override
  String get callCopy => 'Every day, around the clock';

  @override
  String get emailTitle => 'Email';

  @override
  String get emailValue => 'help@ata.sa';

  @override
  String get emailCopy => 'We reply within 24 hours';

  @override
  String get termsPanelEyebrow => 'Last updated: January 2025';

  @override
  String get termsPanelTitle => 'Terms and conditions';

  @override
  String get termsPanelCopy =>
      'Please read the ATA terms of service carefully.';

  @override
  String get terms1Title => '1. Using the service';

  @override
  String get terms1Copy =>
      'The ATA app is a technology platform for requesting transport services. By using the app, you confirm the accuracy of the information you provide and your compliance with applicable regulations.';

  @override
  String get terms2Title => '2. Account and responsibility';

  @override
  String get terms2Copy =>
      'You are responsible for protecting your phone number and account, and for informing support immediately of any suspected unauthorized use.';

  @override
  String get terms3Title => '3. Rides and payments';

  @override
  String get terms3Copy =>
      'An estimated fare is shown before you request a ride. It may change according to the actual distance and time or additional regulatory fees.';

  @override
  String get terms4Title => '4. Privacy';

  @override
  String get terms4Copy =>
      'Location and trip data are processed to provide and improve the service, in line with the privacy policy and approved data-protection standards.';

  @override
  String get deleteTitle => 'Delete the app and your data?';

  @override
  String get deleteCopy =>
      'Your account, ride history and saved data will be permanently deleted. This action cannot be undone.';

  @override
  String get deleteWarning =>
      'You will not be able to recover your account data after confirming.';

  @override
  String get confirmDelete => 'Confirm deletion';

  @override
  String get driverPortal => 'Driver portal';

  @override
  String get driverAccountEyebrow => 'Driver account';

  @override
  String driverWelcome(String name) {
    return 'Welcome, $name';
  }

  @override
  String get driverDashboardCopy =>
      'Manage your rides, earnings and account details in one place.';

  @override
  String get driverGuestName => 'ATA captain';

  @override
  String get onlineLabel => 'Available for rides';

  @override
  String get offlineLabel => 'Offline';

  @override
  String get tabOverview => 'Overview';

  @override
  String get tabDocuments => 'Documents and vehicle';

  @override
  String get tabSettings => 'Account settings';

  @override
  String get statEarningsToday => 'Today\'s earnings';

  @override
  String get statTrips => 'Trips';

  @override
  String get statTripsMeta => 'completed';

  @override
  String get statHours => 'Hours online';

  @override
  String get statHoursMeta => 'hours today';

  @override
  String get statRating => 'Rating';

  @override
  String get statRatingMeta => 'out of 5.0';

  @override
  String get viewAll => 'View all';

  @override
  String get driverTripsEmpty => 'No trips yet. Go online to receive requests.';

  @override
  String get thisWeek => 'This week';

  @override
  String get totalEarnings => 'Total earnings';

  @override
  String get weeklyTarget => 'Weekly target';

  @override
  String get transferEarnings => 'Transfer earnings';

  @override
  String get documents => 'Documents';

  @override
  String get accountVerified => 'Account verified';

  @override
  String expiresOn(String date) {
    return 'Expires on $date';
  }

  @override
  String get noExpiry => 'No expiry date';

  @override
  String get docVerified => 'Verified';

  @override
  String get docExpiringSoon => 'Renew soon';

  @override
  String get docExpired => 'Expired';

  @override
  String get documentsEmpty => 'No documents uploaded yet';

  @override
  String get plateNumber => 'Plate number';

  @override
  String vehicleMeta(String color, String year) {
    return '$color · $year model';
  }

  @override
  String get updateVehicle => 'Update vehicle details';

  @override
  String get noVehicle => 'No vehicle added yet';

  @override
  String get driverPersonalCopy => 'Name, phone and profile photo';

  @override
  String get driverBank => 'Bank account';

  @override
  String get driverBankCopy => 'Manage your IBAN and payouts';

  @override
  String get driverTripSettings => 'Ride settings';

  @override
  String get driverTripSettingsCopy => 'Working area and request preferences';

  @override
  String get driverNotifCopy => 'Ride and earnings alerts';

  @override
  String get driverSupport => 'Help and support';

  @override
  String get driverSupportCopy => 'Contact the driver support team';

  @override
  String get driverNotApproved => 'Your account is not approved yet';

  @override
  String get tripEyebrow => 'Your current trip';

  @override
  String tripNumberLabel(String number) {
    return 'Trip $number';
  }

  @override
  String etaChip(String eta) {
    return 'Arrives in $eta';
  }

  @override
  String get etaUnknown => 'Calculating arrival time';

  @override
  String get driverAssignedTitle => 'A captain has been assigned';

  @override
  String get driverEnRouteTitle => 'Your captain is on the way';

  @override
  String get driverArrivedTitle => 'Your captain has arrived';

  @override
  String get waitingCopy => 'Your captain is waiting at the pickup point';

  @override
  String get waitingTimerLabel => 'Waiting time';

  @override
  String get pinTitle => 'Trip start code';

  @override
  String get pinCopy => 'Tell your captain this code when you board';

  @override
  String get callDriver => 'Call';

  @override
  String get shareTrip => 'Share';

  @override
  String shareTripText(
    String number,
    String driver,
    String vehicle,
    String plate,
  ) {
    return 'My ATA trip $number. Captain: $driver, vehicle: $vehicle ($plate).';
  }

  @override
  String ratingValue(String rating) {
    return 'Rating $rating';
  }

  @override
  String vehicleLine(String make, String model, String color) {
    return '$make $model · $color';
  }

  @override
  String get inTripTitle => 'Your trip is in progress';

  @override
  String get inTripCopy => 'Sit back, we will let you know when you arrive.';

  @override
  String get readyToStartTitle => 'Ready to go';

  @override
  String get readyToStartCopy => 'Code verified, your trip is about to start.';

  @override
  String get receiptTitle => 'You have arrived';

  @override
  String get receiptCopy =>
      'Thanks for riding with ATA. Here is your trip summary.';

  @override
  String get receiptFare => 'Fare';

  @override
  String get receiptDistance => 'Distance';

  @override
  String get receiptDuration => 'Duration';

  @override
  String get receiptPayment => 'Payment';

  @override
  String kmValue(String km) {
    return '$km km';
  }

  @override
  String metersValue(String meters) {
    return '$meters m';
  }

  @override
  String get rateTrip => 'Rate the trip';

  @override
  String get done => 'Done';

  @override
  String get cancelledTitle => 'Trip cancelled';

  @override
  String get cancelledCopy => 'You can request a new ride at any time.';

  @override
  String get noDriversTitle => 'No captain nearby';

  @override
  String get noDriversCopy =>
      'All captains are busy right now. Please try again shortly.';

  @override
  String get retryRequest => 'Request a new ride';

  @override
  String get cancelTrip => 'Cancel trip';

  @override
  String get cancelReasonTitle => 'Why do you want to cancel?';

  @override
  String get cancelReasonCopy => 'Pick a reason to cancel the trip right away.';

  @override
  String get reasonChangedMind => 'I changed my mind';

  @override
  String get reasonDriverLate => 'The captain is late';

  @override
  String get reasonWrongPickup => 'Wrong pickup location';

  @override
  String get reasonOther => 'Other reason';

  @override
  String get keepTrip => 'Keep the trip';

  @override
  String get offeredPriceLabel => 'Suggest a price';

  @override
  String get offeredPriceHint => 'Optional: propose a price that suits you';

  @override
  String offeredPriceActive(String price) {
    return 'Your offer: $price';
  }

  @override
  String get offeredPriceClear => 'Clear';

  @override
  String offeredPriceMin(String price) {
    return 'Min $price';
  }

  @override
  String offeredPriceMax(String price) {
    return 'Max $price';
  }

  @override
  String get offeredPriceDecrease => 'Lower the price by one riyal';

  @override
  String get offeredPriceIncrease => 'Raise the price by one riyal';

  @override
  String offerOutOfRange(String min, String max) {
    return 'Your offer is outside the allowed range ($min – $max SAR) and was adjusted';
  }

  @override
  String get quoteExpiredError =>
      'The price expired and was refreshed. Tap again to confirm.';

  @override
  String get quoteLoading => 'Calculating the price…';

  @override
  String get quoteFailed =>
      'Could not calculate the price. Shown prices are estimates.';

  @override
  String get quoteExpiredHint => 'The price has expired';

  @override
  String get refreshQuote => 'Refresh price';

  @override
  String get fareDetails => 'Price details';

  @override
  String fareDetailsCopy(String category) {
    return 'How the $category price was calculated';
  }

  @override
  String get fareBaseFare => 'Base fare';

  @override
  String fareDistance(String distance) {
    return 'Distance ($distance)';
  }

  @override
  String fareTime(String duration) {
    return 'Time ($duration)';
  }

  @override
  String get fareMinApplied => 'Minimum fare applied';

  @override
  String get fareTimeMultiplier => 'Time multiplier';

  @override
  String get fareDemandMultiplier => 'Demand multiplier';

  @override
  String get fareBookingFee => 'Booking fee';

  @override
  String get fareServiceFee => 'Service fee';

  @override
  String get fareDiscount => 'Discount';

  @override
  String get fareTotal => 'Total';

  @override
  String multiplierValue(String value) {
    return '×$value';
  }

  @override
  String demandBadge(String name, String multiplier) {
    return '$name ×$multiplier';
  }

  @override
  String get demandNormal => 'Normal demand';

  @override
  String get demandModerate => 'Moderate demand right now';

  @override
  String get demandHigh => 'High demand right now';

  @override
  String get demandVeryHigh => 'Very high demand right now';

  @override
  String get offerPassengerOffered => 'Passenger\'s offer';

  @override
  String offerRound(int round) {
    return 'Round $round';
  }

  @override
  String get tripActiveExists => 'You already have an active trip';

  @override
  String get offerExpiredError => 'This offer has expired';

  @override
  String pinInvalid(int attempts) {
    return 'Incorrect code. Attempts left: $attempts';
  }

  @override
  String get pinLocked => 'Code verification is locked. Contact support.';

  @override
  String get callFailed => 'Could not start the call';

  @override
  String get offerEyebrow => 'New request';

  @override
  String get offerTitle => 'A new trip near you';

  @override
  String offerSecondsLeft(int seconds) {
    return '${seconds}s';
  }

  @override
  String get offerDistanceToPickup => 'Distance to you';

  @override
  String get offerEta => 'Time to pickup';

  @override
  String get offerTripDistance => 'Trip distance';

  @override
  String get offerPassengerPrice => 'Passenger price';

  @override
  String get offerNetEarnings => 'Your net earnings';

  @override
  String get offerPassenger => 'Passenger';

  @override
  String get acceptOffer => 'Accept trip';

  @override
  String get rejectOffer => 'Decline';

  @override
  String get offerExpiredTitle => 'Offer expired';

  @override
  String get offerExpiredCopy =>
      'The next request will arrive as soon as it is available.';

  @override
  String get pickupTitle => 'Pickup';

  @override
  String get dropoffTitle => 'Destination';

  @override
  String stopN(int n) {
    return 'Stop $n';
  }

  @override
  String get driverTripEyebrow => 'Current trip';

  @override
  String get actionEnRoute => 'Heading to passenger';

  @override
  String get actionArrived => 'I have arrived';

  @override
  String get actionStart => 'Start trip';

  @override
  String get actionComplete => 'End trip';

  @override
  String get enterPinTitle => 'Enter the passenger code';

  @override
  String get enterPinCopy =>
      'Ask the passenger for the 4-digit trip start code';

  @override
  String get verifyPinAction => 'Verify code';

  @override
  String get stageDriverAssigned => 'Trip accepted';

  @override
  String get stageEnRoute => 'On the way to the passenger';

  @override
  String get stageArrived => 'At the pickup point';

  @override
  String get stageWaiting => 'Waiting for the passenger';

  @override
  String get stagePinVerified => 'Ready to depart';

  @override
  String get stageInTrip => 'Trip in progress';

  @override
  String get stageCompleted => 'Trip completed';

  @override
  String get callPassenger => 'Call passenger';

  @override
  String get backToDashboard => 'Back to dashboard';

  @override
  String get earningsLine => 'Your earnings for this trip';

  @override
  String get driverTripCompletedCopy =>
      'Well done! The trip earnings were added to your wallet.';

  @override
  String get driverTripCancelledCopy =>
      'This trip was cancelled. New requests will arrive soon.';

  @override
  String get locationDenied => 'ATA needs location access to receive trips';

  @override
  String get locationDeniedForever =>
      'Enable location access for ATA in the device settings';

  @override
  String get locationServiceDisabled =>
      'Turn on location services (GPS) to receive trips';

  @override
  String get locationStreaming => 'Your location is shared with passengers';

  @override
  String get noDriversNearby => 'No drivers nearby';

  @override
  String get cardBrandMada => 'mada';

  @override
  String get cardBrandVisa => 'Visa';

  @override
  String get cardBrandMastercard => 'Mastercard';

  @override
  String cardMasked(String brand, String last4) {
    return '$brand •••• $last4';
  }

  @override
  String cardExpiry(String expiry) {
    return 'Expires $expiry';
  }

  @override
  String get cardExpired => 'Card expired';

  @override
  String get cardPendingVerification => 'Awaiting bank verification';

  @override
  String get cardDefault => 'Default';

  @override
  String get cardMakeDefault => 'Make default';

  @override
  String get cardRemove => 'Remove';

  @override
  String get cardRemoveTitle => 'Remove card?';

  @override
  String cardRemoveCopy(String card) {
    return '$card will be removed from your account.';
  }

  @override
  String get savedCardsTitle => 'My cards';

  @override
  String get savedCardsCopy => 'Saved cards for trips and wallet top-ups.';

  @override
  String get savedCardsEmpty => 'No saved cards yet';

  @override
  String get manageCards => 'Manage cards';

  @override
  String get addCard => 'Add card';

  @override
  String get addCardTitle => 'Add a new card';

  @override
  String get addCardCopy =>
      'Your card is tokenised on this device; its number never reaches our servers.';

  @override
  String get cardNumberLabel => 'Card number';

  @override
  String get cardNumberHint => '0000 0000 0000 0000';

  @override
  String get cardNumberError => 'Invalid card number';

  @override
  String get cardExpiryLabel => 'Expiry';

  @override
  String get cardExpiryHint => 'MM/YY';

  @override
  String get cardExpiryError => 'Invalid date';

  @override
  String get cardCvcLabel => 'CVC';

  @override
  String get cardCvcHint => '123';

  @override
  String get cardCvcError => 'Invalid code';

  @override
  String get cardHolderLabel => 'Cardholder name';

  @override
  String get cardHolderHint => 'As shown on the card';

  @override
  String get cardSetDefault => 'Use as default card';

  @override
  String get saveCard => 'Save card';

  @override
  String get sandboxCardsHint =>
      'Sandbox: 4000 0000 0000 0002 is declined, 4000 0000 0000 3220 needs verification, mada test card 4406 4700 0000 0007.';

  @override
  String get paymentActionTitle => 'Bank verification required';

  @override
  String get paymentActionCopy =>
      'Complete the verification on your bank\'s page; the status updates automatically once confirmed.';

  @override
  String get paymentActionOpen => 'Open verification page';

  @override
  String get topUpSource => 'Pay with';

  @override
  String get topUpToContinue => 'Top up to continue';

  @override
  String get outstandingBalanceTitle => 'You have an outstanding balance';

  @override
  String get outstandingBalanceCopy =>
      'Your wallet balance is negative. Top up to request new rides.';

  @override
  String outstandingBalanceError(String amount) {
    return 'You owe SAR $amount. Top up your wallet to continue.';
  }

  @override
  String get paymentFailedError =>
      'Payment failed. Try another card or pay cash.';

  @override
  String get paymentMethodExpiredError => 'The card has expired';

  @override
  String get paymentMethodInUseError => 'The card is linked to an ongoing trip';

  @override
  String get paymentProviderUnavailableError =>
      'The payment service is unavailable. Try again later.';

  @override
  String get paymentFallbackCash =>
      'The card could not be charged, so the trip was switched to cash.';

  @override
  String collectCashLine(String amount) {
    return 'Collect in cash from the rider: $amount';
  }

  @override
  String get viewReceipt => 'View receipt';

  @override
  String get receiptEyebrow => 'Trip receipt';

  @override
  String get receiptBreakdown => 'Fare breakdown';

  @override
  String get receiptSubtotal => 'Subtotal';

  @override
  String get receiptDiscountTotal => 'Total discount';

  @override
  String receiptVat(String rate, String amount) {
    return 'Includes $rate% VAT: $amount';
  }

  @override
  String get receiptPaid => 'Amount paid';

  @override
  String get receiptRefunded => 'Refunded';

  @override
  String get receiptNetPaid => 'Net paid';

  @override
  String get receiptUnavailable => 'No receipt is available for this trip';

  @override
  String get discountSourcePromotion => 'Promotion';

  @override
  String get discountSourceFavoriteDriver => 'Favourite driver';

  @override
  String get cashDebtLimitError =>
      'Your cash dues exceed the limit. Settle them to go online.';

  @override
  String get cantGoOnlineTitle => 'You can\'t go online';

  @override
  String get cashDebtTitle => 'Cash dues';

  @override
  String get cashDebtCopy =>
      'Cash fares owed to the platform. Settle them before reaching the limit.';

  @override
  String cashDebtLimit(String limit) {
    return 'Limit: $limit';
  }

  @override
  String get settleDebt => 'Settle';

  @override
  String get settleDebtTitle => 'Settle cash dues';

  @override
  String get settleDebtCopy =>
      'Top up your driver wallet to settle cash-trip dues.';

  @override
  String get driverWalletEyebrow => 'Wallet & earnings';

  @override
  String get earningsStatementTitle => 'Earnings statement';

  @override
  String get earningsStatementCopy =>
      'Your earnings, platform commission and cash collected for the period.';

  @override
  String get periodToday => 'Today';

  @override
  String get periodWeek => 'Week';

  @override
  String get periodMonth => 'Month';

  @override
  String get statementNet => 'Net';

  @override
  String statementTrips(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count trips',
      one: '1 trip',
      zero: 'No trips',
    );
    return '$_temp0';
  }

  @override
  String get statementGross => 'Gross fares';

  @override
  String get statementCommission => 'Platform commission';

  @override
  String get statementEarnings => 'Your earnings';

  @override
  String get statementIncentives => 'Incentives';

  @override
  String get statementCompensation => 'Cancellation compensation';

  @override
  String get statementAdjustments => 'Adjustments';

  @override
  String get statementCashCollected => 'Cash collected';

  @override
  String get statementPayouts => 'Payouts';

  @override
  String get statementDaily => 'By day';

  @override
  String get payoutsTitle => 'Payouts';

  @override
  String get payoutsCopy =>
      'Request a transfer to your bank account and track its status.';

  @override
  String get payoutsEmpty => 'No payout requests yet';

  @override
  String get payoutHistory => 'Payout history';

  @override
  String get requestPayout => 'Request payout';

  @override
  String get payoutRequestCopy =>
      'The amount is sent to your registered IBAN once approved.';

  @override
  String get payoutAvailable => 'Available';

  @override
  String payoutAvailableLine(String amount) {
    return 'Available: $amount';
  }

  @override
  String get payoutIban => 'IBAN';

  @override
  String get payoutIbanMissing => 'Not added';

  @override
  String get payoutAmountLabel => 'Amount';

  @override
  String get payoutAmountInvalid => 'Enter a valid amount';

  @override
  String payoutMinimumHint(String amount) {
    return 'Minimum payout $amount';
  }

  @override
  String confirmPayout(String amount) {
    return 'Confirm payout of $amount';
  }

  @override
  String get payoutRequestedTitle => 'Payout requested';

  @override
  String get payoutRequestedCopy =>
      'We\'ll notify you when it is approved and paid.';

  @override
  String get payoutUnavailable => 'Payouts are unavailable right now';

  @override
  String get payoutRequested => 'Requested';

  @override
  String get payoutApproved => 'Approved';

  @override
  String get payoutPaid => 'Paid';

  @override
  String get payoutRejected => 'Rejected';

  @override
  String get payoutCancelled => 'Cancelled';

  @override
  String payoutRejectedReason(String reason) {
    return 'Reason: $reason';
  }

  @override
  String payoutBelowMinimumError(String amount) {
    return 'The amount is below the minimum payout (SAR $amount)';
  }

  @override
  String get payoutBelowMinimumReason =>
      'Your balance is below the minimum payout';

  @override
  String get payoutCashDebtReason => 'Settle your cash dues first';

  @override
  String get payoutPendingExistsError =>
      'You already have a payout in progress';

  @override
  String get ibanMissingError => 'Add your IBAN first';

  @override
  String get insufficientBalanceError =>
      'The amount exceeds your available balance';

  @override
  String get shareNotFoundError => 'Tracking link not found';

  @override
  String get shareExpiredError => 'The tracking link has expired';

  @override
  String get trustedContactsLimitError =>
      'You can add up to 5 trusted contacts';

  @override
  String get trustedContactExistsError => 'This contact is already added';

  @override
  String get chatClosedError => 'Chat is closed for this trip';

  @override
  String get lostItemWindowClosedError =>
      'The lost item reporting window has closed';

  @override
  String get cancellationReasonInvalidError => 'Invalid cancellation reason';

  @override
  String get cancellationFeeChangedError =>
      'The cancellation fee changed, review it and try again';

  @override
  String noShowTooEarlyError(int minutes) {
    return 'The required waiting time hasn\'t passed yet ($minutes min left)';
  }

  @override
  String get accountRestrictedError =>
      'Your account is temporarily restricted due to repeated cancellations';

  @override
  String accountRestrictedUntil(String date) {
    return 'Your account is restricted due to repeated cancellations until $date';
  }

  @override
  String get cancelNoteHint => 'Write the cancellation reason';

  @override
  String get cancelNoteRequired => 'This reason requires a note';

  @override
  String get confirmCancel => 'Confirm cancellation';

  @override
  String get reasonEmergencyBadge => 'Emergency';

  @override
  String get reasonExcusableBadge => 'Reviewed';

  @override
  String get cancelEmergencyNote =>
      'A safety case will be opened and the safety team will contact you; no fee or points apply until it is reviewed.';

  @override
  String get cancelExcusableNote =>
      'Operations will review this excuse; no fee or points apply until it is reviewed.';

  @override
  String get cancelFree => 'Free';

  @override
  String penaltyPointsValue(int count) {
    return '+$count points';
  }

  @override
  String get scheduledCancelFeeLabel => 'Scheduled booking cancellation fee';

  @override
  String get cancelPointsLabel => 'Impact on your reliability';

  @override
  String get cancelFeeLabel => 'Cancellation fee';

  @override
  String cancelFreeUntil(String time) {
    return 'Cancelling is free until $time';
  }

  @override
  String get cancelRequiresReview =>
      'The fee is potential and is decided after the excuse review';

  @override
  String noShowAvailableIn(String time) {
    return 'You can report a no-show in $time';
  }

  @override
  String get noShowAvailableNow => 'The wait is over, you can report a no-show';

  @override
  String get noShowAction => 'Passenger didn\'t show';

  @override
  String get noShowConfirmTitle => 'Confirm the passenger didn\'t show?';

  @override
  String get noShowConfirmCopy =>
      'The trip will be cancelled, the passenger charged the no-show fee and you compensated per the policy.';

  @override
  String get keepWaiting => 'Keep waiting';

  @override
  String get noShowRecorded =>
      'The no-show was recorded and the trip cancelled.';

  @override
  String compensationLine(String amount) {
    return 'Your compensation: $amount';
  }

  @override
  String get cancelFeePendingReview =>
      'The cancellation fee is under operations review.';

  @override
  String cancelFeeCharged(String amount) {
    return 'A $amount cancellation fee was charged.';
  }

  @override
  String get levelNone => 'Good standing';

  @override
  String get levelWarning => 'Warning';

  @override
  String get levelDeprioritized => 'Lower matching priority';

  @override
  String get levelIncentivesReduced => 'Reduced incentives';

  @override
  String get levelRestricted => 'Temporarily restricted';

  @override
  String get levelSuspended => 'Suspended';

  @override
  String get levelNoneCopy =>
      'Your record is good, keep completing your trips.';

  @override
  String get levelWarningCopy =>
      'Frequent cancellations add points and can restrict your account.';

  @override
  String get levelDeprioritizedCopy =>
      'You will temporarily receive fewer offers because of your cancellation rate.';

  @override
  String get levelIncentivesReducedCopy =>
      'You receive fewer offers and reduced incentives until your reliability improves.';

  @override
  String get levelRestrictedDriverCopy =>
      'You can\'t go online or receive requests until the restriction ends.';

  @override
  String get levelRestrictedRiderCopy =>
      'You can\'t request new trips until the restriction ends.';

  @override
  String get levelSuspendedCopy =>
      'The account is suspended until operations review it.';

  @override
  String get excusePending => 'Excuse under review';

  @override
  String get excuseApproved => 'Excuse approved';

  @override
  String get excuseRejected => 'Excuse rejected';

  @override
  String get reliabilityTitle => 'Your reliability';

  @override
  String get reliabilityCopy =>
      'Your cancellation rate, points and how they affect your account.';

  @override
  String get cancellationRateLabel => 'Cancellation rate';

  @override
  String get penaltyPointsLabel => 'Points';

  @override
  String get acceptanceRateLabel => 'Acceptance rate';

  @override
  String restrictedUntilLine(String date) {
    return 'Restricted until $date';
  }

  @override
  String get reliabilityDetails => 'View details';

  @override
  String get tripsAcceptedLabel => 'Assigned trips';

  @override
  String get tripsCompletedLabel => 'Completed trips';

  @override
  String get cancellationsAtFaultLabel => 'Counted cancellations';

  @override
  String get reliabilityRateLabel => 'Completion rate';

  @override
  String get noShowCountLabel => 'No-shows';

  @override
  String get matchingFactorLabel => 'Matching factor';

  @override
  String get incentiveMultiplierLabel => 'Incentive multiplier';

  @override
  String reliabilityWindow(int days) {
    return 'Last $days days';
  }

  @override
  String nextLevelLine(String level, String points, String rate) {
    return 'Next level “$level” at $points points or a $rate cancellation rate';
  }

  @override
  String get recentCancellations => 'Recent cancellations';

  @override
  String get noRecentCancellations => 'No counted cancellations.';

  @override
  String get caseTypeSos => 'Emergency (SOS)';

  @override
  String get caseTypeReport => 'Safety report';

  @override
  String get alertUnexpectedStop => 'Unexpected stop';

  @override
  String get alertRouteDeviation => 'Route deviation';

  @override
  String get alertTripOverrun => 'Trip running long';

  @override
  String get caseStatusOpen => 'Open';

  @override
  String get caseStatusInProgress => 'In progress';

  @override
  String get caseStatusEscalated => 'Escalated';

  @override
  String get caseStatusResolved => 'Resolved';

  @override
  String get alertUnexpectedStopCopy =>
      'We noticed the trip has been stopped for a while at an unexpected place.';

  @override
  String get alertRouteDeviationCopy =>
      'We noticed the trip moved away from the expected route.';

  @override
  String get alertTripOverrunCopy =>
      'The trip is taking much longer than expected.';

  @override
  String get safetyCheckCopy =>
      'We want to make sure you\'re safe during the trip.';

  @override
  String get reportUnsafeDriving => 'Unsafe driving';

  @override
  String get reportHarassment => 'Harassment or abuse';

  @override
  String get reportVehicleMismatch => 'Vehicle doesn\'t match the app';

  @override
  String get reportDriverMismatch => 'Driver doesn\'t match the app';

  @override
  String get reportPassengerMisconduct => 'Passenger misconduct';

  @override
  String get reportOther => 'Other';

  @override
  String get lostPhone => 'Phone';

  @override
  String get lostWallet => 'Wallet';

  @override
  String get lostBag => 'Bag';

  @override
  String get lostKeys => 'Keys';

  @override
  String get lostDocuments => 'Documents';

  @override
  String get lostOther => 'Other';

  @override
  String get lostStatusOpen => 'Waiting for the driver';

  @override
  String get lostStatusDriverContacted => 'Driver contacted';

  @override
  String get lostStatusFound => 'Found';

  @override
  String get lostStatusReturned => 'Returned';

  @override
  String get lostStatusNotFound => 'Not found';

  @override
  String get lostStatusClosed => 'Closed';

  @override
  String get sosSemantics => 'Emergency button, press and hold to confirm';

  @override
  String get sosLabel => 'SOS';

  @override
  String get sosHoldHint => 'Hold for emergency';

  @override
  String get sosKeepHolding => 'Keep holding…';

  @override
  String get sosSending => 'Sending the emergency alert…';

  @override
  String get sosActiveTitle => 'Emergency alert sent to the safety team';

  @override
  String get sosCancelledTitle => 'Emergency alert cancelled';

  @override
  String get sosFailedTitle => 'Couldn\'t send the emergency alert';

  @override
  String sosCaseLine(String number, String status) {
    return 'Case $number · $status';
  }

  @override
  String sosContactsNotified(int count) {
    return '$count trusted contacts notified';
  }

  @override
  String get sosSharingLocation =>
      'Your location is shared with the safety team every 10 seconds';

  @override
  String get sosCancelledCopy =>
      'The safety team will contact you to make sure you\'re safe.';

  @override
  String callEmergency(String number) {
    return 'Call emergency $number';
  }

  @override
  String get sosPressedByMistake => 'Pressed by mistake';

  @override
  String get close => 'Close';

  @override
  String get safetyCheckTitle => 'Are you OK?';

  @override
  String get safetyCheckHelpSent =>
      'The safety team was alerted and will contact you right away.';

  @override
  String get safetyCheckOkThanks => 'Thank you, glad you\'re OK.';

  @override
  String get safetyCheckExpired =>
      'We didn\'t get your answer; the safety team will contact you.';

  @override
  String safetyCheckCountdown(int seconds) {
    return 'Please answer within $seconds s';
  }

  @override
  String get safetyCheckOk => 'I\'m OK';

  @override
  String get safetyCheckHelp => 'I need help';

  @override
  String get manageSharing => 'Manage';

  @override
  String shareTripLinkText(String url) {
    return 'Follow my ATA trip live: $url';
  }

  @override
  String get shareSheetCopy =>
      'Send a live tracking link; you can revoke it at any time.';

  @override
  String get shareToContacts => 'Send to your trusted contacts';

  @override
  String get addTrustedContact => 'Add a trusted contact';

  @override
  String get sendBySms => 'Send by SMS';

  @override
  String smsSentTo(int count) {
    return 'Sent to $count';
  }

  @override
  String get activeLinks => 'Active links';

  @override
  String get noActiveLinks => 'No active links.';

  @override
  String shareViews(int count) {
    return '$count views';
  }

  @override
  String get revokeLink => 'Revoke';

  @override
  String get chatAction => 'Chat';

  @override
  String maskedCallPin(String pin) {
    return 'Call PIN: $pin';
  }

  @override
  String get callUnavailable =>
      'Calling isn\'t available right now, use the chat.';

  @override
  String get chatWithDriver => 'Chat with your driver';

  @override
  String get chatWithPassenger => 'Chat with the passenger';

  @override
  String get chatMaskedNote =>
      'Phone numbers are never shared and are hidden inside messages.';

  @override
  String get chatEmpty => 'No messages yet.';

  @override
  String get chatInputHint => 'Type a message…';

  @override
  String get chatSend => 'Send';

  @override
  String get chatClosedBanner => 'The trip ended; the chat is read-only.';

  @override
  String get messageSending => 'Sending…';

  @override
  String get messageFailed => 'Not sent, tap to retry';

  @override
  String get messageRead => 'Read';

  @override
  String get sosHoldCopy =>
      'Hold SOS to alert the safety team with your location';

  @override
  String get shareTripOnTripOnly =>
      'Available during a trip from the driver card, and can be sent automatically to your trusted contacts.';

  @override
  String get myReportsTitle => 'My reports';

  @override
  String get myReportsCopy =>
      'Follow your safety reports and emergency alerts.';

  @override
  String get lostItemsTitle => 'Lost items';

  @override
  String get lostItemsCopy => 'Follow your lost item reports.';

  @override
  String get trustedContactsPageCopy =>
      'Add up to 5 contacts who get an SMS in an emergency and, with auto-share on, your trip tracking link.';

  @override
  String get noTrustedContacts =>
      'You haven\'t added any trusted contacts yet.';

  @override
  String trustedContactsCount(int count, int max) {
    return '$count of $max';
  }

  @override
  String get edit => 'Edit';

  @override
  String get delete => 'Delete';

  @override
  String get autoShareLabel => 'Share my trips automatically';

  @override
  String get notifyOnSosLabel => 'Alert in an emergency';

  @override
  String get contactPhoneSelf => 'You can\'t add your own number';

  @override
  String get editTrustedContact => 'Edit trusted contact';

  @override
  String get contactNameLabel => 'Name';

  @override
  String get contactNameRequired => 'Name is required';

  @override
  String get contactPhoneLabel => 'Mobile number';

  @override
  String get contactPhoneHint => '05XXXXXXXX';

  @override
  String get contactRelationshipLabel => 'Relationship (optional)';

  @override
  String get save => 'Save';

  @override
  String get noReports => 'No reports.';

  @override
  String get caseNoUpdates =>
      'No updates yet; the safety team will contact you.';

  @override
  String get safetyReportTitle => 'Report a safety issue';

  @override
  String get safetyReportCopy =>
      'Tell us what happened on your trip (within 7 days); the safety team will review it.';

  @override
  String get reportSubmitted => 'Your report was received';

  @override
  String reportNumberLine(String number) {
    return 'Report number: $number';
  }

  @override
  String get chooseCategory => 'Choose a category';

  @override
  String get describeWhatHappened => 'Describe what happened…';

  @override
  String get descriptionRequired => 'A description is required';

  @override
  String get submitReport => 'Submit report';

  @override
  String get noPendingSafetyCheck => 'There is no pending safety check.';

  @override
  String get lostItemTitle => 'Report a lost item';

  @override
  String get lostItemCopy =>
      'Describe the item; we\'ll notify the driver and follow up through support (within 7 days of the trip).';

  @override
  String get lostItemSubmitted => 'Lost item report received';

  @override
  String get lostItemDescribe =>
      'Describe the item (colour, brand, where it was…)';

  @override
  String get lostItemContactPhone => 'Contact number (optional)';

  @override
  String get lostItemsPageCopy => 'Status of your lost item reports.';

  @override
  String get noLostItems => 'No lost item reports.';

  @override
  String get driverLostItemsTitle => 'Lost items';

  @override
  String get driverLostItemsCopy => 'Items passengers reported in your trips.';

  @override
  String get lostItemFound => 'Found it';

  @override
  String get lostItemNotFound => 'Not found';

  @override
  String get tripHelpTitle => 'Need help with this trip?';

  @override
  String get lostItemRowCopy => 'Left something in the car?';

  @override
  String get safetyReportRowCopy => 'Report unsafe driving or misconduct';

  @override
  String get later => 'Later';

  @override
  String get rateShort => 'Rate';

  @override
  String get rateNow => 'Rate now';

  @override
  String get ratePassenger => 'Rate the passenger';

  @override
  String get tripRated => 'Trip rated';

  @override
  String get rateTripCopy =>
      'Your rating helps us improve and stays anonymous.';

  @override
  String get rateDriverQuestion => 'How was your trip with the captain?';

  @override
  String rateDriverQuestionNamed(String name) {
    return 'How was your trip with $name?';
  }

  @override
  String get ratePassengerQuestion => 'How was the passenger?';

  @override
  String ratePassengerQuestionNamed(String name) {
    return 'How was $name?';
  }

  @override
  String get ratingStarsNone => 'Choose a star rating';

  @override
  String get ratingStars1 => 'Bad';

  @override
  String get ratingStars2 => 'Below expectations';

  @override
  String get ratingStars3 => 'Okay';

  @override
  String get ratingStars4 => 'Good';

  @override
  String get ratingStars5 => 'Excellent';

  @override
  String get ratingTagsPositive => 'What did you like?';

  @override
  String get ratingTagsNegative => 'What went wrong?';

  @override
  String get ratingTagDriving => 'Driving';

  @override
  String get ratingTagCleanliness => 'Cleanliness';

  @override
  String get ratingTagBehaviour => 'Behaviour';

  @override
  String get ratingTagNavigation => 'Navigation';

  @override
  String get ratingTagVehicleCondition => 'Vehicle condition';

  @override
  String get ratingTagPunctuality => 'Punctuality';

  @override
  String get ratingCommentHint => 'Add a comment (optional)';

  @override
  String get ratingSubmit => 'Submit rating';

  @override
  String get ratingThanks => 'Thanks for your rating';

  @override
  String get ratingThanksCopy =>
      'Your feedback helps us make every trip better.';

  @override
  String get ratingWindowClosedError => 'The rating period has ended';

  @override
  String get ratingExistsError => 'This trip has already been rated';

  @override
  String get pendingRatingTitle => 'Rate your last trip';

  @override
  String get pendingRatingCopy => 'Tell us how it went.';

  @override
  String pendingRatingCopyNamed(String name) {
    return 'How was your trip with $name?';
  }

  @override
  String get driverRatingsTitle => 'My ratings';

  @override
  String get driverRatingsCopy =>
      'Your average and what passengers mention most, anonymously.';

  @override
  String ratingCountLine(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count ratings',
      one: '1 rating',
      zero: 'No ratings yet',
    );
    return '$_temp0';
  }

  @override
  String get ratingTopTags => 'Mentioned most';

  @override
  String get ratingRecentComments => 'Recent comments';

  @override
  String get ratingNoComments => 'No comments yet.';

  @override
  String get fareTotalBeforeDiscount => 'Before discount';

  @override
  String receiptPromo(String code) {
    return 'Promo code $code';
  }

  @override
  String get promoReserved => 'Reserved';

  @override
  String get promoCodeLabel => 'Promo code';

  @override
  String get promoAddHint => 'Add a promo code if you have one';

  @override
  String get promoNotWithOffer =>
      'Promo codes do not apply when you offer a price';

  @override
  String promoAppliedLine(String code) {
    return '$code applied';
  }

  @override
  String promoAppliedDiscount(String code, String amount) {
    return '$code applied · you save $amount';
  }

  @override
  String get promoRemove => 'Remove';

  @override
  String get promoCodeTitle => 'Promo code';

  @override
  String get promoCodeCopy =>
      'Enter the code and we will check it against your current fare.';

  @override
  String get promoCodeHint => 'e.g. ATA10';

  @override
  String get promoApply => 'Apply';

  @override
  String get promoNotFoundError => 'The promo code is invalid';

  @override
  String get promoExpiredError => 'The promo code has expired';

  @override
  String get promoNotEligibleError =>
      'The promo code does not apply to this trip';

  @override
  String get promoUsageLimitError => 'The promo code has been used up';

  @override
  String get promoUserLimitError =>
      'You have used this code the maximum number of times';

  @override
  String get promoReasonFirstTrip => 'This code is for your first trip only';

  @override
  String get promoReasonNewUsers => 'This code is for new users only';

  @override
  String get promoReasonCity => 'This code is not available in your city';

  @override
  String get promoReasonCategory =>
      'This code does not apply to the selected ride type';

  @override
  String get promoReasonZone => 'This code does not apply to the pickup area';

  @override
  String get promoReasonPaymentMethod =>
      'This code does not apply to the selected payment method';

  @override
  String get promoReasonBookingType =>
      'This code does not apply to this booking type';

  @override
  String get promoReasonMinFare => 'The fare is below this code\'s minimum';

  @override
  String get promoReasonPricingMode =>
      'Promo codes do not apply when you offer a price';

  @override
  String get promotionsTitle => 'Offers';

  @override
  String get promotionsCopy => 'Promo codes available to you, used or expired.';

  @override
  String get promotionsLinkCopy => 'Promo codes available to you';

  @override
  String get promotionsEmpty => 'No offers here right now.';

  @override
  String get promoTabAvailable => 'Available';

  @override
  String get promoTabUsed => 'Used';

  @override
  String get promoTabExpired => 'Expired';

  @override
  String promoPercentOff(String percent) {
    return '$percent% off';
  }

  @override
  String promoUpTo(String discount, String cap) {
    return '$discount up to $cap';
  }

  @override
  String promoAmountOff(String amount) {
    return '$amount off';
  }

  @override
  String get promoFreeBookingFee => 'Free booking fee';

  @override
  String promoMinFare(String amount) {
    return 'Minimum fare $amount';
  }

  @override
  String get promoFirstTripOnly => 'First trip only';

  @override
  String promoValidTo(String date) {
    return 'Valid until $date';
  }

  @override
  String get promoUse => 'Use';

  @override
  String promoCopied(String code) {
    return 'Code $code copied';
  }

  @override
  String get tierTitle => 'Captain level';

  @override
  String get tierCopy => 'Recalculated weekly from your last 28 days.';

  @override
  String get tierBronze => 'Bronze';

  @override
  String get tierSilver => 'Silver';

  @override
  String get tierGold => 'Gold';

  @override
  String get tierPlatinum => 'Platinum';

  @override
  String get tierTopReached => 'You reached the top level — keep it up.';

  @override
  String tierTripsToNext(int count, String tier) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count trips to reach $tier',
      one: '1 trip to reach $tier',
    );
    return '$_temp0';
  }

  @override
  String tierProgressTo(String tier) {
    return 'Your progress to $tier';
  }

  @override
  String tierChecksMet(int met, int total) {
    return '$met of $total requirements met';
  }

  @override
  String tierCommissionDiscount(String percent) {
    return '$percent% commission discount';
  }

  @override
  String tierRequirementsTitle(String tier, int days) {
    return '$tier requirements (last $days days)';
  }

  @override
  String get tierCriterionTrips => 'Completed trips';

  @override
  String get tierCriterionRating => 'Average rating';

  @override
  String get tierCriterionAcceptance => 'Acceptance rate';

  @override
  String get tierCriterionCancellation => 'Cancellation rate';

  @override
  String tierRecalcAt(String date) {
    return 'Next recalculation: $date';
  }

  @override
  String get tierRecalcWeekly => 'Your level is recalculated every Sunday.';

  @override
  String get incentivesTitle => 'Incentives';

  @override
  String get incentivesCopy =>
      'Complete the required trips in the period to earn the reward.';

  @override
  String get incentivesLinkCopy =>
      'Active and upcoming quests and their rewards';

  @override
  String get incentivesEmpty => 'No incentives here right now.';

  @override
  String get incentiveNearest => 'Closest quest';

  @override
  String get incentiveTabActive => 'Active';

  @override
  String get incentiveTabUpcoming => 'Upcoming';

  @override
  String get incentiveTabCompleted => 'Completed';

  @override
  String get incentiveTypeDaily => 'Daily';

  @override
  String get incentiveTypeWeekly => 'Weekly';

  @override
  String get incentiveTypeZone => 'Zone';

  @override
  String get incentiveTypeOneTime => 'One-time';

  @override
  String get incentiveInProgress => 'In progress';

  @override
  String get incentiveAchieved => 'Achieved · paid after the period ends';

  @override
  String get incentivePaid => 'Paid';

  @override
  String get incentiveExpired => 'Expired';

  @override
  String get incentiveVoided => 'Voided';

  @override
  String incentiveTripsProgress(int done, int target) {
    return '$done of $target trips';
  }

  @override
  String incentiveTripsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count trips',
      one: '1 trip',
    );
    return '$_temp0';
  }

  @override
  String get incentiveJoinFirst => 'Join to start counting';

  @override
  String get incentiveJoined => 'You joined this incentive';

  @override
  String get incentiveOptIn => 'Join the incentive';

  @override
  String get incentiveOptInClosedError =>
      'Joining this incentive is not available';

  @override
  String incentiveEndsAt(String date) {
    return 'Ends $date';
  }

  @override
  String incentiveReducedNotice(String multiplier) {
    return 'Your rewards are reduced (×$multiplier) because of your reliability level. Improve it to get full rewards.';
  }

  @override
  String get incentiveDetailTitle => 'Incentive details';

  @override
  String get incentiveTarget => 'Target';

  @override
  String get incentiveWindow => 'Days and hours';

  @override
  String get incentiveZones => 'Zones';

  @override
  String get incentiveAllZones => 'All zones';

  @override
  String get incentiveCategories => 'Ride types';

  @override
  String get incentiveAllCategories => 'All ride types';

  @override
  String get incentivePeriod => 'Period';

  @override
  String get daySun => 'Sun';

  @override
  String get dayMon => 'Mon';

  @override
  String get dayTue => 'Tue';

  @override
  String get dayWed => 'Wed';

  @override
  String get dayThu => 'Thu';

  @override
  String get dayFri => 'Fri';

  @override
  String get daySat => 'Sat';

  @override
  String get favoriteDriversTitle => 'Favourite drivers';

  @override
  String get favoriteDriversCopy =>
      'Captains you added after past trips. Request them directly and get a discount.';

  @override
  String get favoriteEmptyTitle => 'No favourite captains yet';

  @override
  String get favoriteEmptyCopy =>
      'After a completed trip you can add the captain from the rating sheet or the receipt.';

  @override
  String favoriteTripsTogether(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count trips together',
      one: '1 trip together',
    );
    return '$_temp0';
  }

  @override
  String favoriteLastTrip(String date) {
    return 'Last trip $date';
  }

  @override
  String get favoriteAvailableNow => 'Available now';

  @override
  String favoriteAvailableEta(String eta) {
    return 'Available now · arrives in $eta';
  }

  @override
  String get favoriteRemove => 'Remove from favourites';

  @override
  String favoriteRemoveTitle(String name) {
    return 'Remove $name from favourites?';
  }

  @override
  String get favoriteRemoveCopy =>
      'You will no longer be able to request them directly or get the favourite discount. Any ongoing trip is not affected.';

  @override
  String get favoriteRemoveConfirm => 'Remove';

  @override
  String favoriteRemovedSnack(String name) {
    return '$name was removed from your favourites';
  }

  @override
  String get favoriteAdd => 'Add to favourites';

  @override
  String get favoriteAdding => 'Adding…';

  @override
  String get favoriteAdded => 'Added to favourites';

  @override
  String get favoriteAlready => 'Already in your favourites';

  @override
  String favoriteAddOptionCopy(String name) {
    return 'Request $name directly on your next trips';
  }

  @override
  String get favoriteAddOptionCopyAnon =>
      'Request this captain directly on your next trips';

  @override
  String get favoriteRatingAdded => 'The captain was added to your favourites';

  @override
  String favoriteRatingFailed(String reason) {
    return 'Your rating was sent, but the captain could not be added to your favourites: $reason';
  }

  @override
  String get favoriteNotEligibleError =>
      'You can add the captain after completing a trip with them';

  @override
  String get favoriteExistsError => 'The captain is already in your favourites';

  @override
  String get favoritesLimitError =>
      'You reached the maximum number of favourite captains';

  @override
  String get favoriteNotFavoriteError =>
      'This captain is no longer in your favourites';

  @override
  String get favoriteRowTitle => 'Favourite drivers';

  @override
  String get favoriteRowManage => 'Manage';

  @override
  String get favoriteRowHint =>
      'Pick a favourite captain to get your request first';

  @override
  String get favoriteRowNone =>
      'None of your favourite captains is available now';

  @override
  String get favoriteRowLoading => 'Looking for your favourite captains…';

  @override
  String get favoriteRowNotWithOffer =>
      'Not available with your own price offer';

  @override
  String get favoriteChipUnavailable => 'Unavailable now';

  @override
  String favoriteDiscountBadge(String percent) {
    return '$percent% off';
  }

  @override
  String favoriteSelectedLine(String name) {
    return 'Your request goes to $name first';
  }

  @override
  String get favoriteFallbackNote =>
      'If they are unavailable we will find the nearest captain';

  @override
  String favoriteDiscountSaved(String amount) {
    return 'Favourite captain discount · you save $amount';
  }

  @override
  String favoriteDiscountConditional(String name) {
    return 'The discount applies once $name accepts the trip';
  }

  @override
  String get favoritePromoNotStacked =>
      'The promo code was not applied: it cannot be combined with the larger favourite discount';

  @override
  String get favoriteLostToPromo =>
      'The favourite discount was not applied: the promo code is larger and cannot be combined';

  @override
  String get favoriteDeselect => 'Clear favourite captain';

  @override
  String get favoriteSearchingTitle => 'Contacting your favourite captain...';

  @override
  String favoriteSearchingCopy(String name) {
    return 'Your request goes to $name first. If they do not answer we will find the nearest captain.';
  }

  @override
  String favoriteFallbackNotice(String name) {
    return '$name could not answer, we are finding you another captain';
  }

  @override
  String favoriteUnavailableNotice(String name) {
    return '$name is unavailable now, we are finding the nearest captain';
  }

  @override
  String get receiptFavoriteDiscount => 'Favourite captain discount';

  @override
  String get favoriteDiscountApplied => 'Applied';

  @override
  String get favoriteDriverBadge => 'Favourite';

  @override
  String get offerFavoriteRequest => 'From a passenger who favours you';

  @override
  String get offerFavoriteExclusive => 'Exclusive to you';

  @override
  String get airportCategoryNotApplicableError =>
      'The airport category is only for airport trips';

  @override
  String get airportChoose =>
      'Choose the airport and the direction of your trip';

  @override
  String get airportChooseTerminal => 'Terminal (optional)';

  @override
  String get airportChooseZone => 'Choose a pickup zone';

  @override
  String get airportDirectionDropoff => 'To the airport';

  @override
  String get airportDirectionPickup => 'From the airport';

  @override
  String get airportDone => 'Done';

  @override
  String get airportFlightHint => 'e.g. SV1020';

  @override
  String get airportFlightInvalid => 'Invalid flight number, e.g. SV1020';

  @override
  String get airportFlightLabel => 'Flight number (optional)';

  @override
  String airportFlightLine(String flight) {
    return 'Flight $flight';
  }

  @override
  String get airportNoAirports => 'No airports available right now';

  @override
  String get airportNoDetails => 'No terminal selected yet';

  @override
  String get airportPickupZoneRequiredError =>
      'Choose a pickup zone at the airport';

  @override
  String get airportQueueCopy =>
      'Airport trips go to drivers in arrival order: first come, first served.';

  @override
  String airportQueueEligible(String airport) {
    return 'You are in the $airport waiting area';
  }

  @override
  String get airportQueueExitNote =>
      'If you leave the waiting area or go offline for a while you are removed from the queue automatically.';

  @override
  String get airportQueueEyebrow => 'Airport';

  @override
  String get airportQueueJoin => 'Join the queue';

  @override
  String get airportQueueJoinHint => 'Join to receive airport trips in order';

  @override
  String get airportQueueLeave => 'Leave the queue';

  @override
  String get airportQueueNotNearby =>
      'You are not in an airport waiting area right now';

  @override
  String airportQueuePosition(int position, int total) {
    return 'You are $position of $total';
  }

  @override
  String get airportQueueTitle => 'Airport queue';

  @override
  String airportQueueWait(int minutes) {
    return 'Estimated wait about $minutes min';
  }

  @override
  String get airportRemove => 'Remove';

  @override
  String get airportRowCopy => 'Choose the airport, terminal and flight number';

  @override
  String get airportRowTitle => 'Trip to or from an airport?';

  @override
  String get airportSheetTitle => 'Airport';

  @override
  String airportTerminalLabel(String code) {
    return 'Terminal $code';
  }

  @override
  String airportWaitingFree(int minutes) {
    return '$minutes min free waiting';
  }

  @override
  String airportWaitingPolicyCopy(int minutes) {
    return 'You get $minutes minutes of free waiting at pickup; waiting fees apply after that.';
  }

  @override
  String get airportWaitingPolicyTitle => 'Waiting policy';

  @override
  String countdownDaysHours(int days, int hours) {
    return '$days d $hours h';
  }

  @override
  String countdownHoursMinutes(int hours, int minutes) {
    return '$hours h $minutes min';
  }

  @override
  String get driverScheduledCopy =>
      'Upcoming scheduled requests in your area. Reserve what suits you and confirm before the time.';

  @override
  String get driverScheduledEyebrow => 'Reservations';

  @override
  String get driverScheduledLinkCopy =>
      'Marketplace and your upcoming reservations';

  @override
  String driverScheduledNext(String time) {
    return 'Next reservation: $time';
  }

  @override
  String get driverScheduledTabMarket => 'Marketplace';

  @override
  String get driverScheduledTabMine => 'My reservations';

  @override
  String get driverScheduledTitle => 'My scheduled rides';

  @override
  String get marketAirportBadge => 'Airport';

  @override
  String get marketApproxPickup =>
      'Pickup location is approximate until you reserve';

  @override
  String get marketDayAll => 'All';

  @override
  String marketDistanceToPickup(String km) {
    return '$km km away';
  }

  @override
  String get marketEmptyCopy => 'The list refreshes every minute';

  @override
  String get marketEmptyTitle => 'No scheduled requests right now';

  @override
  String marketExclusiveUntil(String time) {
    return 'Exclusive to you until $time';
  }

  @override
  String marketFare(String price) {
    return 'Fare $price';
  }

  @override
  String get marketLoadMore => 'Show more';

  @override
  String get marketNetEarnings => 'Your net earnings';

  @override
  String get marketReserve => 'Reserve this ride';

  @override
  String get marketReserved =>
      'Ride reserved. You will find it in My reservations';

  @override
  String get notInAirportWaitingAreaError =>
      'You must be inside the airport waiting area';

  @override
  String get reservationConfirmAction => 'Confirm';

  @override
  String reservationConfirmCopy(String time) {
    return 'Scheduled ride at $time. Confirm before the time runs out or the reservation is released.';
  }

  @override
  String get reservationConfirmFinalTitle =>
      'Final confirmation: get ready to go';

  @override
  String get reservationConfirmFirstTitle => 'Confirm your reservation';

  @override
  String reservationConfirmLeft(String time) {
    return 'Time left $time';
  }

  @override
  String get reservationConfirmed => 'Reservation confirmed';

  @override
  String get reservationConflictError =>
      'This time conflicts with a ride you already reserved';

  @override
  String get reservationDetailEyebrow => 'Scheduled reservation';

  @override
  String get reservationFare => 'Estimated fare';

  @override
  String get reservationFinalConfirmed =>
      'Final confirmation done. The ride starts at its time';

  @override
  String get reservationFreeReleaseLabel => 'Free release until';

  @override
  String get reservationLimitReachedError =>
      'You reached the maximum number of reservations';

  @override
  String get reservationNotConfirmableError => 'Cannot confirm right now';

  @override
  String get reservationNotConfirmableOfflineError =>
      'Go online to confirm the reservation';

  @override
  String get reservationNotConfirmableOnTripError =>
      'You have a trip in progress. Finish it first to confirm';

  @override
  String get reservationNotFound => 'Reservation not found';

  @override
  String get reservationPassenger => 'Passenger';

  @override
  String get reservationReleaseAction => 'Release reservation';

  @override
  String get reservationReleaseConfirm => 'Yes, release';

  @override
  String reservationReleaseFreeCopy(String time) {
    return 'Releasing is free until $time.';
  }

  @override
  String get reservationReleaseLateCopy =>
      'The free release window is over. Reliability points will be deducted, which can affect matching priority and incentives.';

  @override
  String get reservationReleaseTitle => 'Release this reservation?';

  @override
  String get reservationReleased => 'Reservation released';

  @override
  String reservationReleasedPoints(int points) {
    return 'Reservation released; $points reliability points deducted';
  }

  @override
  String get reservationStatusAssigned => 'Assigned to you';

  @override
  String get reservationStatusCancelled => 'Cancelled';

  @override
  String get reservationStatusCompleted => 'Completed';

  @override
  String get reservationStatusConfirmed => 'Confirmed';

  @override
  String get reservationStatusNoShow => 'No-show';

  @override
  String get reservationStatusReleased => 'Released';

  @override
  String get reservationStatusReserved => 'Reserved';

  @override
  String get reservationTakenError =>
      'Another driver already reserved this ride';

  @override
  String get reservationsActive => 'Upcoming';

  @override
  String get reservationsEmpty => 'No reservations here';

  @override
  String get reservationsHistory => 'Past';

  @override
  String get scheduleBooked => 'Your scheduled ride is booked';

  @override
  String get scheduleConfirm => 'Confirm time';

  @override
  String get scheduleConfirmedExpired =>
      'This time is no longer inside the bookable window. Pick a new time';

  @override
  String get scheduleConfirmedLabel => 'Ride time';

  @override
  String get scheduleDayHeading => 'Day';

  @override
  String get scheduleEdit => 'Edit';

  @override
  String scheduleFavoritePriority(String name) {
    return 'Your favourite driver $name gets first pick of this booking';
  }

  @override
  String get scheduleFixedPrice => 'Fixed price for that time · no surge';

  @override
  String get scheduleHourHeading => 'Hour';

  @override
  String get scheduleLeadTooShortError =>
      'The time must be at least 30 minutes from now';

  @override
  String scheduleLeadTooShortUntilError(String time) {
    return 'The earliest available time is $time';
  }

  @override
  String get scheduleMinuteHeading => 'Minute';

  @override
  String get scheduleNothingPicked => 'Pick a day and a time';

  @override
  String schedulePickerCopy(String lead, String window) {
    return 'Book at least $lead ahead and up to $window from now';
  }

  @override
  String get schedulePickerTitle => 'When do you need your ride?';

  @override
  String scheduleRide(String name, String price) {
    return 'Book $name · $price';
  }

  @override
  String get scheduleRulesFallback =>
      'Could not load the scheduling rules; using the defaults.';

  @override
  String get scheduleSelectedLabel => 'Selected time';

  @override
  String scheduleTooFar(String time) {
    return 'The latest available time is $time';
  }

  @override
  String scheduleTooSoon(String time) {
    return 'The earliest available time is $time';
  }

  @override
  String scheduleWindowDays(int days) {
    String _temp0 = intl.Intl.pluralLogic(
      days,
      locale: localeName,
      other: '$days days',
      one: '1 day',
    );
    return '$_temp0';
  }

  @override
  String get scheduleWindowExceededError =>
      'You cannot schedule more than 7 days ahead';

  @override
  String scheduleWindowExceededUntilError(String time) {
    return 'You cannot schedule after $time';
  }

  @override
  String get scheduledBookNew => 'Book a ride';

  @override
  String get scheduledCancelButton => 'Cancel booking';

  @override
  String get scheduledCancelledTitle => 'Booking cancelled';

  @override
  String get scheduledCountdownLabel => 'Starts in';

  @override
  String get scheduledDetailEyebrow => 'Scheduled ride';

  @override
  String get scheduledDriverConfirmedBadge => 'Confirmed';

  @override
  String get scheduledDriverReservedBadge => 'Reserved';

  @override
  String get scheduledDriverTitle => 'Reserved driver';

  @override
  String get scheduledEmptyCopy =>
      'Book from the home screen and choose \"Schedule\"';

  @override
  String get scheduledEmptyTitle => 'No scheduled rides';

  @override
  String get scheduledFareLabel => 'Estimated fare';

  @override
  String get scheduledLateCancelNote =>
      'The free cancellation window is over; a fee may apply (you will see it before confirming).';

  @override
  String get scheduledLimitReachedError =>
      'You reached the maximum number of scheduled rides';

  @override
  String scheduledLimitReachedMaxError(int max) {
    return 'You reached the maximum number of scheduled rides ($max)';
  }

  @override
  String get scheduledLinkCopy => 'Your upcoming bookings and their status';

  @override
  String get scheduledNoDriverYet =>
      'No driver has reserved your ride yet. We will start searching automatically before the time.';

  @override
  String get scheduledPhaseConfirmed => 'Driver confirmed';

  @override
  String get scheduledPhaseEnded => 'Ended';

  @override
  String get scheduledPhaseInProgress => 'Ride in progress';

  @override
  String get scheduledPhaseReserved => 'Reserved by a driver';

  @override
  String get scheduledPhaseSearching => 'Searching for a driver';

  @override
  String get scheduledPhaseWaiting => 'Waiting for a driver';

  @override
  String get scheduledSearchStartedCopy =>
      'The search for your driver started. Follow it on the trip screen.';

  @override
  String scheduledSearchStartsAt(String time) {
    return 'If no driver reserves it, the search starts $time';
  }

  @override
  String get scheduledTrackTrip => 'Track the ride';

  @override
  String get scheduledTripTitle => 'Scheduled ride';

  @override
  String get scheduledTripsCopy => 'Your upcoming rides, soonest first';

  @override
  String get scheduledTripsEyebrow => 'Bookings';

  @override
  String get scheduledTripsTitle => 'My scheduled rides';

  @override
  String tripAirportDropoff(String code) {
    return 'Drop-off at $code airport';
  }

  @override
  String tripAirportPickup(String code) {
    return 'Pickup from $code airport';
  }

  @override
  String get tripStatusScheduled => 'Scheduled';

  @override
  String get reservationOpenMarket => 'Open the marketplace';

  @override
  String get reservationNotConfirmableNotDueError =>
      'It is not time to confirm yet';

  @override
  String get reservationNotConfirmableExpiredError =>
      'The confirmation window has ended';

  @override
  String get supportEyebrow => 'Help';

  @override
  String get supportTitle => 'Help & support';

  @override
  String get supportCopy => 'Find an answer or contact the support team';

  @override
  String get supportRowCopy => 'Help center and your tickets';

  @override
  String get helpSearchHint => 'Search help';

  @override
  String get helpTopicsTitle => 'Topics';

  @override
  String helpArticlesCount(int count) {
    return '$count articles';
  }

  @override
  String get helpNoResults => 'No matching articles';

  @override
  String get helpNoResultsCopy =>
      'Try other words or open a ticket and we will help';

  @override
  String get helpAllTopics => 'All topics';

  @override
  String get helpLoadMore => 'Show more';

  @override
  String helpArticleUpdated(String date) {
    return 'Updated $date';
  }

  @override
  String get helpRelatedTitle => 'Related articles';

  @override
  String get helpFeedbackTitle => 'Was this article helpful?';

  @override
  String get helpFeedbackYes => 'Yes';

  @override
  String get helpFeedbackNo => 'No';

  @override
  String get helpFeedbackThanks => 'Thanks for your feedback';

  @override
  String get helpFeedbackNoCopy =>
      'Sorry about that. You can open a ticket and the support team will help.';

  @override
  String get helpStillNeedTitle => 'Did not find your answer?';

  @override
  String get helpStillNeedCopy =>
      'Open a ticket and the support team will reply';

  @override
  String get supportMyTickets => 'My tickets';

  @override
  String get supportMyTicketsCopy =>
      'Follow your requests and the team replies';

  @override
  String supportUnreadBadge(int count) {
    return '$count new';
  }

  @override
  String get supportContactUs => 'Contact us';

  @override
  String get supportContactUsCopy =>
      'Open a new ticket and the team will reply';

  @override
  String get ticketsEyebrow => 'Support';

  @override
  String get ticketsTitle => 'My tickets';

  @override
  String get ticketsCopy =>
      'Your requests to the support team and their status';

  @override
  String get ticketsTabOpen => 'Open';

  @override
  String get ticketsTabClosed => 'Closed';

  @override
  String get ticketsEmpty => 'No tickets here';

  @override
  String get ticketsEmptyCopy =>
      'When you contact support your ticket shows up here';

  @override
  String get ticketsNew => 'New ticket';

  @override
  String ticketNumberAndTrip(String number, String trip) {
    return '$number · trip $trip';
  }

  @override
  String get ticketTypeTripIssue => 'Trip issue';

  @override
  String get ticketTypePaymentIssue => 'Payment and fare';

  @override
  String get ticketTypeLostItem => 'Lost item';

  @override
  String get ticketTypeSafety => 'Safety';

  @override
  String get ticketTypeAccount => 'Account';

  @override
  String get ticketTypeOther => 'Other';

  @override
  String get ticketStatusOpen => 'Open';

  @override
  String get ticketStatusPendingUser => 'Waiting for you';

  @override
  String get ticketStatusInProgress => 'In progress';

  @override
  String get ticketStatusResolved => 'Resolved';

  @override
  String get ticketStatusClosed => 'Closed';

  @override
  String get ticketBannerOpen =>
      'We received your ticket and the team will review it';

  @override
  String get ticketBannerPendingUser =>
      'The team is waiting for your reply to continue';

  @override
  String get ticketBannerInProgress =>
      'The support team is working on your request';

  @override
  String get ticketBannerResolved =>
      'Your request was resolved. If the problem remains you can reply here.';

  @override
  String get ticketBannerClosed => 'This ticket is closed';

  @override
  String get newTicketEyebrow => 'New ticket';

  @override
  String get newTicketTitle => 'How can we help?';

  @override
  String get newTicketCopy =>
      'Choose the request type and tell us what happened';

  @override
  String get newTicketTypeLabel => 'Request type';

  @override
  String get newTicketTripLabel => 'Related trip';

  @override
  String get newTicketTripRequired => 'Choose the trip your request is about';

  @override
  String get newTicketTripOptional => 'Optional';

  @override
  String get newTicketNoTrip => 'No trip';

  @override
  String get newTicketNoTrips => 'No recent trips';

  @override
  String get newTicketSubjectLabel => 'Subject';

  @override
  String get newTicketMessageLabel => 'Details';

  @override
  String get newTicketSubmit => 'Send ticket';

  @override
  String newTicketCreated(String number) {
    return 'Your ticket $number was sent';
  }

  @override
  String get attachmentsTitle => 'Attachments';

  @override
  String get attachmentsAdd => 'Add attachment';

  @override
  String get attachmentsHint =>
      'Images or PDF up to 10 MB, at most 5 attachments';

  @override
  String get attachmentUploading => 'Uploading…';

  @override
  String get attachmentFailedRetry => 'Upload failed · tap to retry';

  @override
  String get attachmentRemove => 'Remove attachment';

  @override
  String get attachmentLimitError => 'At most 5 attachments';

  @override
  String get unsupportedFileTypeError =>
      'Unsupported file type (JPG / PNG images or PDF only)';

  @override
  String get fileTooLargeError => 'The file is larger than 10 MB';

  @override
  String get ticketClosedError => 'The ticket is closed, create a new one';

  @override
  String get disputeExistsError => 'There is already a dispute for this trip';

  @override
  String get disputeWindowClosedError => 'The fare dispute period has ended';

  @override
  String get disputeToggleTitle => 'Dispute the fare';

  @override
  String get disputeToggleCopy =>
      'Ask us to review the amount charged for this trip';

  @override
  String get disputeWindowNote =>
      'You can dispute within a limited period after the trip, one dispute per trip.';

  @override
  String get disputeReasonLabel => 'Reason';

  @override
  String get disputeReasonOvercharged => 'Charged more than expected';

  @override
  String get disputeReasonRouteLonger => 'The route was longer than needed';

  @override
  String get disputeReasonWaitingCharged => 'Incorrect waiting charge';

  @override
  String get disputeReasonCancellationFee => 'Undue cancellation fee';

  @override
  String get disputeReasonPromoNotApplied => 'Discount not applied';

  @override
  String get disputeReasonOther => 'Other reason';

  @override
  String get disputeRefundLabel => 'Refund you ask for (optional)';

  @override
  String get disputeRefundHint => 'Example: 12.00';

  @override
  String get disputeRefundInvalid => 'Enter a valid amount';

  @override
  String get disputeDefaultSubject => 'Fare dispute';

  @override
  String get disputeCardTitle => 'Fare dispute';

  @override
  String get disputeStatusOpen => 'Open';

  @override
  String get disputeStatusUnderReview => 'Under review';

  @override
  String get disputeStatusApproved => 'Approved';

  @override
  String get disputeStatusPartiallyApproved => 'Partially approved';

  @override
  String get disputeStatusRejected => 'Rejected';

  @override
  String disputeCharged(String amount) {
    return 'Amount charged: $amount';
  }

  @override
  String disputeRequested(String amount) {
    return 'Requested: $amount';
  }

  @override
  String disputeApproved(String amount) {
    return 'Refunded: $amount';
  }

  @override
  String get disputeNoRefund => 'No refund';

  @override
  String get threadAgentName => 'ATA support team';

  @override
  String get threadYou => 'You';

  @override
  String get threadReplyHint => 'Write your reply…';

  @override
  String get threadSend => 'Send';

  @override
  String get threadClosedTitle => 'This ticket is closed';

  @override
  String get threadClosedCopy =>
      'New replies cannot be added. Create a new ticket if you need help.';

  @override
  String get threadCreateNew => 'Create a new ticket';

  @override
  String threadRelatedTrip(String trip) {
    return 'Trip $trip';
  }

  @override
  String get threadAttachmentOpen => 'Open / share';

  @override
  String get threadAttachmentLoading => 'Loading the attachment…';

  @override
  String get csatTitle => 'How was your support experience?';

  @override
  String get csatCommentHint => 'Add a comment (optional)';

  @override
  String get csatSubmit => 'Send rating';

  @override
  String get csatThanks => 'Thanks for your rating';

  @override
  String csatStar(int n) {
    return '$n of 5';
  }

  @override
  String get tripHelpIssueTitle => 'A problem with the trip?';

  @override
  String get tripHelpIssueCopy => 'Tell the support team and we will help';

  @override
  String get tripHelpFareTitle => 'A problem with the fare';

  @override
  String get tripHelpFareCopy => 'Dispute the amount charged for this trip';

  @override
  String get supportFollowTicket => 'Follow up with support';

  @override
  String get paymentCorporate => 'Company account';

  @override
  String get corpSectionTitle => 'Company account';

  @override
  String get corpPaidByCompany => 'Paid by the company account';

  @override
  String get corpBillingNote => 'This trip is billed to your company account';

  @override
  String get corpBilledAmount => 'Billed to the company';

  @override
  String corpOptionBudget(String amount) {
    return 'Left this month: $amount';
  }

  @override
  String corpOptionTripLimit(String amount) {
    return 'Max per trip: $amount';
  }

  @override
  String corpOptionUnavailable(String reason) {
    return 'Unavailable: $reason';
  }

  @override
  String get corpPurposeLabel => 'Trip purpose';

  @override
  String get corpPurposeHint => 'For example: client meeting';

  @override
  String get corpPurposeRequired => 'The trip purpose is required';

  @override
  String get corpCostCenterLabel => 'Cost center';

  @override
  String get corpCostCenterRequired => 'Choose a cost center';

  @override
  String get corpCostCenterNone => 'No cost center';

  @override
  String corpCostCenterItem(String code, String name) {
    return '$code · $name';
  }

  @override
  String corpRemainingBudget(String amount) {
    return 'Left in your monthly budget: $amount';
  }

  @override
  String get corpPromoDisabled =>
      'Promo codes do not apply to the company account';

  @override
  String get corpFavoriteDisabled =>
      'The favourite-driver discount does not apply to the company account';

  @override
  String get corpViolationsTitle =>
      'This trip does not match your company policy';

  @override
  String get corpViolationsHint =>
      'Change the category or time, or pick another payment method';

  @override
  String get corpViolationCategory =>
      'This category is not allowed by your company policy';

  @override
  String corpViolationCategoryAllowed(String list) {
    return 'Allowed categories: $list';
  }

  @override
  String get corpViolationDay => 'Trips are not allowed on this day';

  @override
  String corpViolationDayAllowed(String list) {
    return 'Allowed days: $list';
  }

  @override
  String get corpViolationTime => 'The trip time is outside the allowed hours';

  @override
  String corpViolationTimeAllowed(String list) {
    return 'Allowed hours: $list';
  }

  @override
  String get corpViolationZone => 'The pickup or drop-off zone is not allowed';

  @override
  String corpViolationZoneAllowed(String list) {
    return 'Allowed zones: $list';
  }

  @override
  String corpViolationMaxFare(String limit) {
    return 'The fare exceeds the per-trip limit ($limit)';
  }

  @override
  String get corpViolationScheduled =>
      'Scheduled rides are not allowed by your company policy';

  @override
  String get corpViolationBudget =>
      'You have exceeded your available monthly budget';

  @override
  String corpViolationBudgetLeft(String amount) {
    return 'You have exceeded your available monthly budget, $amount left';
  }

  @override
  String get corpViolationCredit =>
      'The company account exceeded its credit limit';

  @override
  String get corpViolationUnknown => 'The trip violates your company policy';

  @override
  String get corpListSeparator => ', ';

  @override
  String corpTimeWindow(String from, String to) {
    return '$from – $to';
  }

  @override
  String get corporateNotMemberError =>
      'You are not a member of a company account';

  @override
  String get corporateAccountInactiveError =>
      'The company account is not active';

  @override
  String get corporateMemberElsewhereError =>
      'This number is linked to another company account';

  @override
  String get corporatePolicyViolationError =>
      'The trip violates your company policy';

  @override
  String get corporateBudgetExceededError =>
      'You have exceeded your available monthly budget';

  @override
  String corporateBudgetExceededLeftError(String amount) {
    return 'You have exceeded your available monthly budget, SAR $amount left';
  }

  @override
  String get corporateCreditLimitError =>
      'The company account exceeded its credit limit, contact your company admin';

  @override
  String get invitationExpiredError =>
      'The invitation has expired, ask your company to send it again';

  @override
  String get corpMyCompany => 'My company';

  @override
  String get corpMyCompanyCopy =>
      'The company account linked to your number, its trip policy and budget';

  @override
  String get corpRoleEmployee => 'Employee';

  @override
  String get corpRoleAdmin => 'Company admin';

  @override
  String get corpStatusActive => 'Active';

  @override
  String get corpStatusInvited => 'Invitation pending';

  @override
  String get corpStatusDisabled => 'Disabled';

  @override
  String get corpStatusDisabledCopy =>
      'Your company membership is disabled, contact your company admin';

  @override
  String corpEmployeeNumber(String number) {
    return 'Employee no.: $number';
  }

  @override
  String corpDepartment(String name) {
    return 'Department: $name';
  }

  @override
  String get corpBudgetTitle => 'Monthly budget';

  @override
  String corpBudgetUsed(String spent, String limit) {
    return '$spent of $limit used';
  }

  @override
  String corpBudgetLeft(String amount) {
    return '$amount left';
  }

  @override
  String get corpBudgetNone => 'No monthly limit';

  @override
  String corpTripLimit(String amount) {
    return 'Max per trip $amount';
  }

  @override
  String get corpPolicyTitle => 'Company policy';

  @override
  String corpPolicyCategories(String list) {
    return 'Categories: $list';
  }

  @override
  String get corpPolicyAllCategories => 'Categories: all';

  @override
  String corpPolicyDays(String list) {
    return 'Days: $list';
  }

  @override
  String corpPolicyHours(String list) {
    return 'Hours: $list';
  }

  @override
  String get corpPolicyPurposeRequired => 'The trip purpose is required';

  @override
  String get corpPolicyCostCenterRequired => 'A cost center is required';

  @override
  String get corpPolicyScheduledAllowed => 'Scheduled rides: allowed';

  @override
  String get corpPolicyScheduledBlocked => 'Scheduled rides: not allowed';

  @override
  String get corpNoMembership => 'You are not a member of a company account';

  @override
  String get corpNoMembershipCopy =>
      'When your company invites you, the invitation will show here';

  @override
  String get corpInvitationsTitle => 'Company invitations';

  @override
  String corpInvitationFrom(String company) {
    return '$company invited you to join';
  }

  @override
  String corpInvitationRole(String role) {
    return 'Role: $role';
  }

  @override
  String corpInvitationExpires(String date) {
    return 'Expires $date';
  }

  @override
  String get corpInvitationExpired => 'Expired';

  @override
  String get corpAccept => 'Accept';

  @override
  String get corpDecline => 'Decline';

  @override
  String corpInvitationAccepted(String company) {
    return 'You joined $company';
  }

  @override
  String get corpInvitationDeclined => 'Invitation declined';

  @override
  String get corpPromptTitle => 'You have a company invitation';

  @override
  String get corpPromptCopy =>
      'Accept it to pay your rides from the company account';

  @override
  String get corpPromptAction => 'View invitation';

  @override
  String get corpMembershipError => 'Could not load the company account';
}
