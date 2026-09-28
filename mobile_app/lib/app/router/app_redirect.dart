import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';

/// Pure redirect rules driven by the session state.
///
/// Returns the location to go to, or `null` to allow [location].
String? resolveRedirect(SessionState session, String location) {
  final bool inAuth = location.startsWith(AppRoutes.authPrefix);
  switch (session.status) {
    case SessionStatus.unknown:
      return location == AppRoutes.splash ? null : AppRoutes.splash;
    case SessionStatus.unauthenticated:
      return inAuth ? null : AppRoutes.language;
    case SessionStatus.authenticated:
      if (session.needsRiderOnboarding) {
        return location == AppRoutes.terms ? null : AppRoutes.terms;
      }
      if (session.session!.isDriver) return _driverRedirect(session, location);
      final bool inDriver = location.startsWith(AppRoutes.driver);
      if (inAuth || inDriver || location == AppRoutes.splash) {
        return AppRoutes.home;
      }
      return null;
  }
}

String? _driverRedirect(SessionState session, String location) {
  if (session.isDriverPending) {
    return location == AppRoutes.driverPending ? null : AppRoutes.driverPending;
  }
  final bool allowed =
      location.startsWith(AppRoutes.driver) &&
      location != AppRoutes.driverPending;
  return allowed ? null : AppRoutes.driver;
}
