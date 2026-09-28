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

  /// Query parameter selecting the dashboard tab (`/driver?tab=documents`).
  static const String tabParam = 'tab';

  /// Query parameter pre-filling the top-up amount.
  static const String amountParam = 'amount';

  /// Path parameter of the rides detail / receipt routes.
  static const String tripIdParam = 'tripId';

  static const String home = '/home';
  static const String trip = '/trip';
  static const String rides = '/rides';
  static const String wallet = '/wallet';
  static const String walletTopUp = '/wallet/top-up';
  static const String walletPaymentMethods = '/wallet/payment-methods';
  static const String walletAddCard = '/wallet/payment-methods/add';
  static const String safety = '/safety';
  static const String account = '/account';
  static const String accountNotifications = '/account/notifications';
  static const String accountLanguage = '/account/language';
  static const String accountContact = '/account/contact';
  static const String accountTerms = '/account/terms';
  static const String accountDelete = '/account/delete';

  /// Query parameter carrying the chosen role to the phone screen.
  static const String roleParam = 'role';

  static String phoneFor(UserRole role) => '$phone?$roleParam=${role.apiValue}';

  /// `/rides/{tripId}/receipt` (F11).
  static String rideReceipt(String tripId) => '$rides/$tripId/receipt';

  /// `/driver/top-up?amount=…` (settles the cash debt).
  static String driverTopUpFor(double? amount) => amount == null || amount <= 0
      ? driverTopUp
      : '$driverTopUp?$amountParam=${amount.ceil()}';

  /// Full-screen map pages of the rider (no bottom navigation).
  static bool isMapPage(String location) =>
      location == home || location == trip;

  /// Tabs of the floating bottom navigation, in order.
  static const List<String> passengerTabs = <String>[
    home,
    rides,
    wallet,
    account,
  ];

  /// Index of the bottom-nav tab that owns [location], or `null`.
  static int? tabIndexFor(String location) {
    for (int i = 0; i < passengerTabs.length; i++) {
      if (location == passengerTabs[i] ||
          location.startsWith('${passengerTabs[i]}/')) {
        return i;
      }
    }
    return null;
  }
}
