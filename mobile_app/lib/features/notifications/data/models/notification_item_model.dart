import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';

/// JSON mapping for [NotificationItem].
class NotificationItemModel extends NotificationItem {
  const NotificationItemModel({
    required super.id,
    required super.type,
    required super.title,
    required super.body,
    required super.createdAt,
    super.category,
    super.data,
    super.readAt,
  });

  factory NotificationItemModel.fromJson(Map<String, dynamic> json) =>
      NotificationItemModel(
        id: json['id'] as String,
        type: json['type'] as String? ?? 'general',
        title: json['title'] as String? ?? '',
        body: json['body'] as String? ?? '',
        category: json['category'] as String?,
        data: json['data'] as Map<String, dynamic>?,
        readAt: DateTime.tryParse(json['readAt'] as String? ?? ''),
        createdAt:
            DateTime.tryParse(json['createdAt'] as String? ?? '') ??
            DateTime.now(),
      );
}
