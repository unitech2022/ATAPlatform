import 'dart:async';

import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/usecases/get_notifications.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notifications_read.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads the inbox, marks items as read and refreshes whenever a push
/// arrives while the app is open.
class NotificationsCubit extends Cubit<NotificationsState> {
  NotificationsCubit({
    required this._getNotifications,
    required this._markRead,
    this._watchIncoming,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const NotificationsState());

  final GetNotifications _getNotifications;
  final MarkNotificationsRead _markRead;
  final WatchIncomingNotifications? _watchIncoming;
  final DateTime Function() _now;
  StreamSubscription<PushEvent>? _incoming;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getNotifications(const NoParams());
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (NotificationsPage page) => state.copyWith(
          loading: false,
          items: page.items,
          unreadCount: page.unreadCount,
        ),
      ),
    );
  }

  /// Reloads the inbox on every foreground push. Idempotent.
  void watchPushes() {
    _incoming ??= _watchIncoming?.call().listen((_) => load());
  }

  Future<void> markAllRead() async {
    if (state.unreadCount == 0) return;
    final result = await _markRead(null);
    result.fold((_) {}, (_) => emit(state.copyWith(unreadCount: 0)));
  }

  /// Marks one row read locally (the open call records it on the API).
  void markItemRead(String id) {
    final int index = state.items.indexWhere(
      (NotificationItem item) => item.id == id && item.isUnread,
    );
    if (index < 0) return;
    final List<NotificationItem> items = List<NotificationItem>.of(state.items)
      ..[index] = state.items[index].markedRead(_now());
    emit(
      state.copyWith(
        items: items,
        unreadCount: state.unreadCount > 0 ? state.unreadCount - 1 : 0,
      ),
    );
  }

  @override
  Future<void> close() async {
    await _incoming?.cancel();
    return super.close();
  }
}
