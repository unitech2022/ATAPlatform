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
  String get promoCopy =>
      'Save the places you visit often to request a ride in one step.';

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
}
