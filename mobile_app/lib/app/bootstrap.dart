import 'package:ata_app/app/app.dart';
import 'package:ata_app/app/push/push_session_binder.dart';
import 'package:ata_app/app/rating_prompt_binder.dart';
import 'package:ata_app/app/router/app_router.dart';
import 'package:ata_app/app/safety_cubits.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:go_router/go_router.dart';

/// Creates the app-wide cubits and router from the registered dependencies,
/// binds push notifications and the pending-rating prompt (F15) to the
/// session and starts session restoration.
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
  final SafetyCubits safety = SafetyCubits.fromInjector()
    ..bind(session: session, trips: trips, deepLinks: deepLinks);
  final PendingRatingCubit pendingRating = PendingRatingCubit(
    getPending: getIt(),
  );
  RatingPromptBinder(pendingRating).bind(session: session, trips: trips);
  PushSessionBinder(
    push: getIt(),
    registerDevice: getIt(),
  ).bindCubits(session, locale);
  session.restore();
  final GoRouter router = createAppRouter(session, trips);
  safety.bindRouter(router);
  return AtaApp(
    sessionCubit: session,
    localeCubit: locale,
    tripCubits: trips,
    safetyCubits: safety,
    deepLinkCubit: deepLinks,
    pendingRatingCubit: pendingRating,
    router: router,
  );
}
