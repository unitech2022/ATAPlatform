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

  static const String home = '/home';
  static const String trip = '/trip';
  static const String rides = '/rides';
  static const String wallet = '/wallet';
  static const String walletTopUp = '/wallet/top-up';
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
