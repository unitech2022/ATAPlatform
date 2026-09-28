import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/push/push_service.dart';
import 'package:ata_app/features/notifications/data/datasources/notifications_remote_data_source.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [NotificationsRepository] backed by the API and the [PushService].
class NotificationsRepositoryImpl implements NotificationsRepository {
  const NotificationsRepositoryImpl(this._remote, this._push);

  final NotificationsRemoteDataSource _remote;
  final PushService _push;

  @override
  Future<Either<Failure, NotificationsPage>> getNotifications({int page = 1}) =>
      guard(() => _remote.list(page: page));

  @override
  Future<Either<Failure, Unit>> markRead(List<String>? ids) => guard(() async {
    await _remote.markRead(ids);
    return unit;
  });

  @override
  Future<Either<Failure, Unit>> markOpened(String id) => guard(() async {
    await _remote.markOpened(id);
    return unit;
  });

  @override
  Stream<PushEvent> watchIncoming() => _push.received;

  @override
  Stream<PushEvent> watchOpened() => _push.opened;
}
