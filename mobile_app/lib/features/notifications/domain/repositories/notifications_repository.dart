import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:fpdart/fpdart.dart';

/// `/notifications` endpoints.
abstract interface class NotificationsRepository {
  Future<Either<Failure, NotificationsPage>> getNotifications({int page = 1});

  /// `null` marks everything as read.
  Future<Either<Failure, Unit>> markRead(List<String>? ids);
}
