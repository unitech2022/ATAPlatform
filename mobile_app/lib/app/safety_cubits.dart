import 'dart:async';

import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_state.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_cubit.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// App-wide F12 cubits and their bindings: the SOS, the "are you OK?"
/// prompt (riders; hub, pushes and `ata://safety/check/{id}` links) and the
/// in-trip chat bound to the active trip (open while `/trip/chat` or
/// `/driver/trip/chat` is shown).
class SafetyCubits {
  SafetyCubits({
    required this.sos,
    required this.safetyCheck,
    required this.chat,
  });

  factory SafetyCubits.fromInjector() => SafetyCubits(
    sos: SosCubit(
      triggerSos: getIt(),
      sendLocation: getIt(),
      cancelSos: getIt(),
      getCase: getIt(),
      requestAccess: getIt(),
      watchPosition: getIt(),
    ),
    safetyCheck: SafetyCheckCubit(
      watchChecks: getIt(),
      getPending: getIt(),
      respond: getIt(),
    ),
    chat: TripChatCubit(
      watchMessages: getIt(),
      send: getIt(),
      markRead: getIt(),
      getQuickReplies: getIt(),
    ),
  );

  final SosCubit sos;
  final SafetyCheckCubit safetyCheck;
  final TripChatCubit chat;

  final List<StreamSubscription<Object?>> _subscriptions =
      <StreamSubscription<Object?>>[];
  GoRouter? _router;

  Widget provide({required Widget child}) => MultiBlocProvider(
    providers: <BlocProvider<dynamic>>[
      BlocProvider<SosCubit>.value(value: sos),
      BlocProvider<SafetyCheckCubit>.value(value: safetyCheck),
      BlocProvider<TripChatCubit>.value(value: chat),
    ],
    child: child,
  );

  void bind({
    required SessionCubit session,
    required TripCubits trips,
    required DeepLinkCubit deepLinks,
  }) {
    _syncSession(session.state);
    _syncChat(trips);
    _subscriptions
      ..add(session.stream.listen(_syncSession))
      ..add(trips.changes.listen((_) => _syncChat(trips)))
      ..add(deepLinks.stream.listen(_onDeepLink));
  }

  /// Marks the chat open while its route is visible.
  void bindRouter(GoRouter router) {
    _router = router;
    router.routerDelegate.addListener(_onRoute);
  }

  void _onRoute() {
    final String path =
        _router?.routerDelegate.currentConfiguration.uri.path ?? '';
    final bool chatVisible =
        path == AppRoutes.tripChat || path == AppRoutes.driverTripChat;
    if (chatVisible && !chat.state.isOpen) chat.open();
    if (!chatVisible && chat.state.isOpen) chat.leave();
  }

  void _syncSession(SessionState session) {
    final bool rider =
        session.isAuthenticated &&
        !session.needsRiderOnboarding &&
        !session.session!.isDriver;
    if (rider) {
      safetyCheck.start();
    } else {
      safetyCheck.stop();
    }
    if (!session.isAuthenticated) {
      chat.unbind();
      sos.dismiss();
    }
  }

  void _syncChat(TripCubits trips) {
    final Trip? rider = trips.activeTrip.state.trip;
    final Trip? driver = trips.driverTrip.state.trip;
    if (driver != null && driver.status.hasDriver) {
      chat.bind(ChatTarget(tripId: driver.id, actor: TripActor.driver));
    } else if (rider != null && rider.status.hasDriver) {
      chat.bind(ChatTarget(tripId: rider.id, actor: TripActor.passenger));
    } else if (chat.state.isBound) {
      chat.unbind();
    }
  }

  void _onDeepLink(DeepLinkState state) {
    final DeepLink? target = state.target;
    const String prefix = '${AppRoutes.safetyCheck}/';
    if (target == null || !target.route.startsWith(prefix)) return;
    final String alertId = target.route.substring(prefix.length);
    if (alertId.isEmpty) return;
    safetyCheck.open(alertId, actionId: state.actionId);
  }

  Future<void> close() async {
    _router?.routerDelegate.removeListener(_onRoute);
    for (final StreamSubscription<Object?> s in _subscriptions) {
      await s.cancel();
    }
    await Future.wait(<Future<void>>[
      sos.close(),
      safetyCheck.close(),
      chat.close(),
    ]);
  }
}
