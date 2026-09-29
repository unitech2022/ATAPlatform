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
  String get scheduleComingSoon =>
      'Scheduling is coming soon (up to 7 days ahead)';

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
}
