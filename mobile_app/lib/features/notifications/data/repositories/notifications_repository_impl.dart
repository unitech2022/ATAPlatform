import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/notifications/data/datasources/notifications_remote_data_source.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [NotificationsRepository] backed by the API.
class NotificationsRepositoryImpl implements NotificationsRepository {
  const NotificationsRepositoryImpl(this._remote);

  final NotificationsRemoteDataSource _remote;

  @override
  Future<Either<Failure, NotificationsPage>> getNotifications({int page = 1}) =>
      guard(() => _remote.list(page: page));

  @override
  Future<Either<Failure, Unit>> markRead(List<String>? ids) => guard(() async {
    await _remote.markRead(ids);
    return unit;
  });
}
