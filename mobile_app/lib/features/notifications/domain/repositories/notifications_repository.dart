import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:fpdart/fpdart.dart';

/// `/notifications` endpoints and the push feed.
abstract interface class NotificationsRepository {
  Future<Either<Failure, NotificationsPage>> getNotifications({int page = 1});

  /// `null` marks everything as read.
  Future<Either<Failure, Unit>> markRead(List<String>? ids);

  /// `POST /notifications/{id}/opened` (read + open tracking).
  Future<Either<Failure, Unit>> markOpened(String id);

  /// Pushes received while the app is in the foreground.
  Stream<PushEvent> watchIncoming();

  /// Pushes the user tapped.
  Stream<PushEvent> watchOpened();
}
