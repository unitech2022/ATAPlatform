import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/notifications/data/models/notification_item_model.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';

/// `/notifications` endpoints.
class NotificationsRemoteDataSource {
  const NotificationsRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _listPath = '/notifications';
  static const String _readPath = '/notifications/read';

  Future<NotificationsPage> list({required int page}) async {
    final Map<String, dynamic> body =
        await _api.get(_listPath, query: <String, dynamic>{'page': page})
            as Map<String, dynamic>;
    final PageResult<NotificationItemModel> result =
        PageResult<NotificationItemModel>.fromJson(
          body,
          NotificationItemModel.fromJson,
        );
    return NotificationsPage(
      items: result.items,
      unreadCount: (body['unreadCount'] as num?)?.toInt() ?? 0,
    );
  }

  Future<void> markRead(List<String>? ids) =>
      _api.post(_readPath, body: <String, dynamic>{'ids': ids});

  Future<void> markOpened(String id) => _api.post('$_listPath/$id/opened');
}
