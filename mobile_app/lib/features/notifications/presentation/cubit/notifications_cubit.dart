import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/usecases/get_notifications.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notifications_read.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads the inbox and marks items as read.
class NotificationsCubit extends Cubit<NotificationsState> {
  NotificationsCubit({required this._getNotifications, required this._markRead})
    : super(const NotificationsState());

  final GetNotifications _getNotifications;
  final MarkNotificationsRead _markRead;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getNotifications(const NoParams());
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

  Future<void> markAllRead() async {
    if (state.unreadCount == 0) return;
    final result = await _markRead(null);
    result.fold((_) {}, (_) => emit(state.copyWith(unreadCount: 0)));
  }
}
