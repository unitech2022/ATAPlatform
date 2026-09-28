import 'dart:async';

import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notification_opened.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_opened_notifications.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Receives `ata://` links (tapped pushes, inbox rows), waits until the
/// session is ready, then exposes the go_router location to open. The
/// router's redirect rules (active trip, pending offer) still win.
class DeepLinkCubit extends Cubit<DeepLinkState> {
  DeepLinkCubit({
    required this._watchOpened,
    required this._markOpened,
    this._parse = const ParseDeepLink(),
    bool Function()? hasActiveTrip,
  }) : _hasActiveTrip = hasActiveTrip ?? _never,
       super(const DeepLinkState());

  final WatchOpenedNotifications _watchOpened;
  final MarkNotificationOpened _markOpened;
  final ParseDeepLink _parse;
  final bool Function() _hasActiveTrip;

  SessionState _session = const SessionState.unknown();
  StreamSubscription<PushEvent>? _opened;
  StreamSubscription<SessionState>? _sessions;

  static bool _never() => false;

  bool get _ready => _session.isAuthenticated && !_session.needsRiderOnboarding;

  /// Starts listening to tapped pushes. Idempotent.
  void start() {
    _opened ??= _watchOpened().listen(
      (PushEvent push) => open(
        push.deepLink,
        notificationId: push.notificationId,
        actionId: push.actionId,
      ),
    );
  }

  /// Follows the session (current value, then every change).
  void bindSession(SessionState initial, Stream<SessionState> changes) {
    _sessions?.cancel();
    sessionChanged(initial);
    _sessions = changes.listen(sessionChanged);
  }

  void sessionChanged(SessionState session) {
    _session = session;
    final String? pending = state.pendingLink;
    if (pending != null && _ready) _resolve(pending, state.actionId);
  }

  /// Opens [link]; [notificationId] is reported as opened (best effort).
  Future<void> open(
    String? link, {
    String? notificationId,
    String? actionId,
  }) async {
    if (notificationId != null) unawaited(_markOpened(notificationId));
    if (link == null || link.isEmpty) return;
    if (!_ready) {
      emit(state.copyWith(pendingLink: link, actionId: actionId));
      return;
    }
    _resolve(link, actionId);
  }

  void _resolve(String link, String? actionId) {
    final DeepLink? target = _parse(
      DeepLinkParams(
        link: link,
        isDriver: _session.session!.isDriver,
        hasActiveTrip: _hasActiveTrip(),
      ),
    );
    emit(
      DeepLinkState(
        target: target,
        actionId: actionId,
        inboxRequested: state.inboxRequested,
      ),
    );
  }

  /// The router navigated to [DeepLinkState.target].
  void consumed() {
    final DeepLink? target = state.target;
    if (target == null) return;
    emit(
      state.copyWith(
        clearTarget: true,
        inboxRequested: state.inboxRequested || target.opensInbox,
      ),
    );
  }

  /// The notifications sheet was shown.
  void inboxShown() => emit(state.copyWith(inboxRequested: false));

  @override
  Future<void> close() async {
    await _opened?.cancel();
    await _sessions?.cancel();
    return super.close();
  }
}
