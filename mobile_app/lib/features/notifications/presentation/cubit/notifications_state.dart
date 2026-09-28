import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:equatable/equatable.dart';

/// Inbox state shown in the header bell and sheet.
class NotificationsState extends Equatable {
  const NotificationsState({
    this.items = const <NotificationItem>[],
    this.unreadCount = 0,
    this.loading = false,
    this.failure,
  });

  final List<NotificationItem> items;
  final int unreadCount;
  final bool loading;
  final Failure? failure;

  NotificationsState copyWith({
    List<NotificationItem>? items,
    int? unreadCount,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => NotificationsState(
    items: items ?? this.items,
    unreadCount: unreadCount ?? this.unreadCount,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[items, unreadCount, loading, failure];
}
