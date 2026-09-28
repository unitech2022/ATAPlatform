import 'package:equatable/equatable.dart';

/// One in-app notification.
class NotificationItem extends Equatable {
  const NotificationItem({
    required this.id,
    required this.type,
    required this.title,
    required this.body,
    required this.createdAt,
    this.data,
    this.readAt,
  });

  final String id;
  final String type;
  final String title;
  final String body;
  final DateTime createdAt;
  final Map<String, dynamic>? data;
  final DateTime? readAt;

  bool get isUnread => readAt == null;

  @override
  List<Object?> get props => <Object?>[
    id,
    type,
    title,
    body,
    createdAt,
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
