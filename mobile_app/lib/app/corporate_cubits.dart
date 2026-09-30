import 'dart:async';

import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_cubit.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// App-wide F19 cubits and their bindings: the rider's company membership
/// and pending invitations load after sign-in (riders only) and reload when
/// an invitation is accepted or a `corporate.*` push arrives.
class CorporateCubits {
  CorporateCubits({
    required this.membership,
    required this.invitations,
    required this._watchIncoming,
  });

  factory CorporateCubits.fromInjector() => CorporateCubits(
    membership: CorporateMembershipCubit(getMembership: getIt()),
    invitations: CorporateInvitationsCubit(
      getInvitations: getIt(),
      accept: getIt(),
      decline: getIt(),
    ),
    watchIncoming: getIt(),
  );

  /// Event codes of the corporate notifications (`docs/08` §F13.2).
  static const String eventPrefix = 'corporate.';

  final CorporateMembershipCubit membership;
  final CorporateInvitationsCubit invitations;
  final WatchIncomingNotifications _watchIncoming;

  final List<StreamSubscription<Object?>> _subscriptions =
      <StreamSubscription<Object?>>[];
  bool _bound = false;

  Widget provide({required Widget child}) => MultiBlocProvider(
    providers: <BlocProvider<dynamic>>[
      BlocProvider<CorporateMembershipCubit>.value(value: membership),
      BlocProvider<CorporateInvitationsCubit>.value(value: invitations),
    ],
    child: child,
  );

  void bind(SessionCubit session) {
    _sync(session.state);
    _subscriptions
      ..add(session.stream.listen(_sync))
      ..add(
        invitations.stream
            .where(
              (CorporateInvitationsState s) =>
                  s.outcome == InvitationOutcome.accepted,
            )
            .listen((_) => membership.refresh()),
      )
      ..add(_watchIncoming().listen(_onPush));
  }

  void _sync(SessionState session) {
    final bool rider =
        session.isAuthenticated &&
        !session.needsRiderOnboarding &&
        !session.session!.isDriver;
    if (rider == _bound) return;
    _bound = rider;
    if (rider) {
      membership.start();
      invitations.load();
    } else {
      membership.stop();
      invitations.reset();
    }
  }

  void _onPush(PushEvent push) {
    if (!_bound || !(push.eventCode?.startsWith(eventPrefix) ?? false)) return;
    membership.refresh();
    invitations.load();
  }

  Future<void> close() async {
    for (final StreamSubscription<Object?> s in _subscriptions) {
      await s.cancel();
    }
  }
}
