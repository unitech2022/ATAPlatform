import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_presence.dart';

/// Pure redirect rules driven by the session state and the ongoing trips.
///
/// Returns the location to go to, or `null` to allow [location].
String? resolveRedirect(
  SessionState session,
  String location, {
  TripPresence presence = const TripPresence(),
}) {
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
      if (session.session!.isDriver) {
        return _driverRedirect(session, location, presence);
      }
      return _riderRedirect(location, presence);
  }
}

String? _riderRedirect(String location, TripPresence presence) {
  if (presence.hasPassengerTrip) {
    return location == AppRoutes.trip ? null : AppRoutes.trip;
  }
  final bool inDriver = location.startsWith(AppRoutes.driver);
  final bool inAuth = location.startsWith(AppRoutes.authPrefix);
  if (inAuth ||
      inDriver ||
      location == AppRoutes.splash ||
      location == AppRoutes.trip) {
    return AppRoutes.home;
  }
  return null;
}

String? _driverRedirect(
  SessionState session,
  String location,
  TripPresence presence,
) {
  if (session.isDriverPending) {
    return location == AppRoutes.driverPending ? null : AppRoutes.driverPending;
  }
  if (presence.hasDriverTrip) {
    return location == AppRoutes.driverTrip ? null : AppRoutes.driverTrip;
  }
  if (presence.hasOffer) {
    return location == AppRoutes.driverOffer ? null : AppRoutes.driverOffer;
  }
  final bool allowed =
      location.startsWith(AppRoutes.driver) &&
      location != AppRoutes.driverPending &&
      location != AppRoutes.driverTrip &&
      location != AppRoutes.driverOffer;
  return allowed ? null : AppRoutes.driver;
}
