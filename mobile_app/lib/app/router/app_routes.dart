import 'package:ata_app/features/auth/domain/entities/user_role.dart';

/// Route paths.
abstract final class AppRoutes {
  static const String splash = '/splash';

  static const String language = '/auth/language';
  static const String role = '/auth/role';
  static const String phone = '/auth/phone';
  static const String otp = '/auth/otp';
  static const String terms = '/auth/terms';
  static const String authPrefix = '/auth';

  static const String driver = '/driver';
  static const String driverPending = '/driver/pending';
  static const String driverOffer = '/driver/offer';
  static const String driverTrip = '/driver/trip';
  static const String driverEarnings = '/driver/earnings';
  static const String driverPayouts = '/driver/payouts';
  static const String driverPayoutRequest = '/driver/payouts/request';
  static const String driverTopUp = '/driver/top-up';
  static const String driverTripChat = '/driver/trip/chat';
  static const String driverReliability = '/driver/reliability';
  static const String driverLostItems = '/driver/lost-items';
  static const String driverTier = '/driver/tier';
  static const String driverIncentives = '/driver/incentives';
  static const String driverRatings = '/driver/ratings';
  static const String driverRate = '/driver/rate';
  static const String driverScheduled = '/driver/scheduled';
  static const String driverAirportQueue = '/driver/airport-queue';

  /// Query parameter selecting the dashboard tab (`/driver?tab=documents`).
  static const String tabParam = 'tab';

  /// Query parameter pre-filling the top-up amount.
  static const String amountParam = 'amount';

  /// Path parameter of the rides detail / receipt routes.
  static const String tripIdParam = 'tripId';

  /// Path parameters of the F12 safety routes.
  static const String caseIdParam = 'caseId';
  static const String alertIdParam = 'alertId';
  static const String reportIdParam = 'reportId';

  /// Path parameter of `/driver/incentives/:incentiveId` (F15).
  static const String incentiveIdParam = 'incentiveId';

  /// Query parameter pre-filling the promo code of the home sheet.
  static const String promoParam = 'promo';

  static const String home = '/home';
  static const String trip = '/trip';
  static const String rate = '/rate';
  static const String promotions = '/promotions';
  static const String scheduled = '/scheduled';
  static const String tripChat = '/trip/chat';
  static const String rides = '/rides';
  static const String wallet = '/wallet';
  static const String walletTopUp = '/wallet/top-up';
  static const String walletPaymentMethods = '/wallet/payment-methods';
  static const String walletAddCard = '/wallet/payment-methods/add';
  static const String safety = '/safety';
  static const String safetyContacts = '/safety/contacts';
  static const String safetyReport = '/safety/report';
  static const String safetyCases = '/safety/cases';
  static const String safetyCheck = '/safety/check';
  static const String safetyLostItems = '/safety/lost-items';
  static const String account = '/account';
  static const String accountNotifications = '/account/notifications';
  static const String accountLanguage = '/account/language';
  static const String accountContact = '/account/contact';
  static const String accountTerms = '/account/terms';
  static const String accountDelete = '/account/delete';
  static const String accountReliability = '/account/reliability';
  static const String accountFavoriteDrivers = '/account/favorite-drivers';

  /// F19: the company membership, policy, budget and invitations
  /// (`ata://corporate/invitations`).
  static const String accountCorporate = '/account/corporate';

  /// F18 support: the rider paths (inside the shell) and their driver twins.
  static const String support = '/support';
  static const String driverSupport = '/driver/support';

  /// Path parameters / query parameters of the support routes.
  static const String slugParam = 'slug';
  static const String ticketIdParam = 'ticketId';
  static const String ticketTypeParam = 'type';
  static const String disputeParam = 'dispute';

  /// Query parameter carrying the chosen role to the phone screen.
  static const String roleParam = 'role';

  static String phoneFor(UserRole role) => '$phone?$roleParam=${role.apiValue}';

  /// `/rides/{tripId}/receipt` (F11).
  static String rideReceipt(String tripId) => '$rides/$tripId/receipt';

  /// `/rides/{tripId}/lost-item` (F12).
  static String rideLostItem(String tripId) => '$rides/$tripId/lost-item';

  /// `/rate/{tripId}` (F15, `ata://rate/{tripId}`).
  static String rateTrip(String tripId) => '$rate/$tripId';

  /// `/driver/rate/{tripId}` (driver side of `ata://rate/{tripId}`).
  static String driverRateTrip(String tripId) => '$driverRate/$tripId';

  /// `/scheduled/{tripId}` (F17, `ata://scheduled/{tripId}`).
  static String scheduledTrip(String tripId) => '$scheduled/$tripId';

  /// `/driver/scheduled/{tripId}` (F17, `ata://driver/scheduled/{tripId}`).
  static String driverScheduledTrip(String tripId) =>
      '$driverScheduled/$tripId';

  /// `/driver/incentives/{id}`.
  static String driverIncentive(String id) => '$driverIncentives/$id';

  /// `/home?promo=CODE` (a code picked on the promotions page).
  static String homeWithPromo(String code) => Uri(
    path: home,
    queryParameters: <String, String>{promoParam: code},
  ).toString();

  /// `/safety/report?tripId=…` (F12).
  static String safetyReportFor(String tripId) =>
      '$safetyReport?$tripIdParam=$tripId';

  /// `/safety/cases/{caseId}`.
  static String safetyCase(String caseId) => '$safetyCases/$caseId';

  /// `/safety/check/{alertId}` ("are you OK?").
  static String safetyCheckFor(String alertId) => '$safetyCheck/$alertId';

  /// `/driver/lost-items/{reportId}`.
  static String driverLostItem(String reportId) => '$driverLostItems/$reportId';

  /// `/driver/top-up?amount=…` (settles the cash debt).
  static String driverTopUpFor(double? amount) => amount == null || amount <= 0
      ? driverTopUp
      : '$driverTopUp?$amountParam=${amount.ceil()}';

  /// `/support` or `/driver/support` (F18 help center).
  static String supportRoot({bool driver = false}) =>
      driver ? driverSupport : support;

  /// `/support/articles/{slug}`.
  static String supportArticle(String slug, {bool driver = false}) =>
      '${supportRoot(driver: driver)}/articles/${Uri.encodeComponent(slug)}';

  /// `/support/tickets` (my tickets).
  static String supportTickets({bool driver = false}) =>
      '${supportRoot(driver: driver)}/tickets';

  /// `/support/tickets/{ticketId}` (`ata://support/tickets/{ticketId}`).
  static String supportTicket(String ticketId, {bool driver = false}) =>
      '${supportTickets(driver: driver)}/$ticketId';

  /// `/support/tickets/new?type=&tripId=&dispute=1`.
  static String newSupportTicket({
    bool driver = false,
    String? type,
    String? tripId,
    bool dispute = false,
  }) {
    final Map<String, String> query = <String, String>{
      ticketTypeParam: ?type,
      tripIdParam: ?tripId,
      if (dispute) disputeParam: '1',
    };
    return Uri(
      path: '${supportTickets(driver: driver)}/new',
      queryParameters: query.isEmpty ? null : query,
    ).toString();
  }

  /// The receipt's "مشكلة في الرحلة؟" (`trip_issue` about [tripId]).
  static String tripIssueTicket(String tripId) =>
      newSupportTicket(type: 'trip_issue', tripId: tripId);

  /// The receipt's "مشكلة في الأجرة" (a fare dispute about [tripId]).
  static String fareDisputeTicket(String tripId) =>
      newSupportTicket(type: 'payment_issue', tripId: tripId, dispute: true);

  /// A ticket thread hides the bottom navigation (reply box at the bottom).
  static bool isSupportThread(String location) {
    if (!location.startsWith('$support/tickets/')) return false;
    final String rest = location.substring('$support/tickets/'.length);
    return rest.isNotEmpty && !rest.startsWith('new') && !rest.contains('/');
  }

  /// Full-screen map pages of the rider (no bottom navigation).
  static bool isMapPage(String location) =>
      location == home || location == trip;

  /// Rider pages without the floating bottom navigation.
  static bool hidesBottomNav(String location) =>
      isMapPage(location) || location == tripChat || isSupportThread(location);

  /// Tabs of the floating bottom navigation, in order.
  static const List<String> passengerTabs = <String>[
    home,
    rides,
    wallet,
    account,
  ];

  /// Index of the bottom-nav tab that owns [location], or `null`.
  static int? tabIndexFor(String location) {
    // Scheduled rides belong to the rides tab.
    if (location == scheduled || location.startsWith('$scheduled/')) return 1;
    // Help and support is reached from the account page.
    if (location == support || location.startsWith('$support/')) return 3;
    for (int i = 0; i < passengerTabs.length; i++) {
      if (location == passengerTabs[i] ||
          location.startsWith('${passengerTabs[i]}/')) {
        return i;
      }
    }
    return null;
  }
}
