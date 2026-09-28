import 'package:ata_app/features/notifications/domain/entities/notification_types.dart';
import 'package:equatable/equatable.dart';

/// One in-app notification.
class NotificationItem extends Equatable {
  const NotificationItem({
    required this.id,
    required this.type,
    required this.title,
    required this.body,
    required this.createdAt,
    this.category,
    this.data,
    this.readAt,
  });

  final String id;

  /// Event code as stored by the API (legacy rows use `snake_case`).
  final String type;
  final String title;
  final String body;
  final DateTime createdAt;
  final String? category;
  final Map<String, dynamic>? data;
  final DateTime? readAt;

  bool get isUnread => readAt == null;

  /// Catalog code, with legacy types normalized.
  String get eventCode => NotificationTypes.normalize(type);

  /// `trips`, `wallet`, `safety`, `promotions`, `offers` or `system`.
  String get resolvedCategory =>
      category ?? NotificationTypes.categoryOf(eventCode);

  /// `data.deepLink`, or a link derived from the event for older rows.
  String? get deepLink =>
      data?['deepLink']?.toString() ??
      NotificationTypes.fallbackLink(eventCode, data);

  NotificationItem markedRead(DateTime at) => NotificationItem(
    id: id,
    type: type,
    title: title,
    body: body,
    createdAt: createdAt,
    category: category,
    data: data,
    readAt: readAt ?? at,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    type,
    title,
    body,
    createdAt,
    category,
    data,
    readAt,
  ];
}

/// A page of notifications plus the unread counter.
class NotificationsPage extends Equatable {
  const NotificationsPage({required this.items, required this.unreadCount});

  final List<NotificationItem> items;
  final int unreadCount;

  @override
  List<Object?> get props => <Object?>[items, unreadCount];
}
