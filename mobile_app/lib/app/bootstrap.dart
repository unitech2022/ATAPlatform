import 'package:ata_app/app/app.dart';
import 'package:ata_app/app/push/push_session_binder.dart';
import 'package:ata_app/app/router/app_router.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';

/// Creates the app-wide cubits and router from the registered dependencies,
/// binds push notifications to the session and starts session restoration.
AtaApp bootstrapApp() {
  final SessionCubit session = SessionCubit(
    restoreSession: getIt(),
    logout: getIt(),
    events: getIt(),
  );
  final LocaleCubit locale = LocaleCubit(
    getSavedLocale: getIt(),
    changeLanguage: getIt(),
  );
  final TripCubits trips = TripCubits.fromInjector()..bindSession(session);
  final DeepLinkCubit deepLinks =
      DeepLinkCubit(
          watchOpened: getIt(),
          markOpened: getIt(),
          hasActiveTrip: () => trips.presence.hasPassengerTrip,
        )
        ..bindSession(session.state, session.stream)
        ..start();
  PushSessionBinder(
    push: getIt(),
    registerDevice: getIt(),
  ).bindCubits(session, locale);
  session.restore();
  return AtaApp(
    sessionCubit: session,
    localeCubit: locale,
    tripCubits: trips,
    deepLinkCubit: deepLinks,
    router: createAppRouter(session, trips),
  );
}
