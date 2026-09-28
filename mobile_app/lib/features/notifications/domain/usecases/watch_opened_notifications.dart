import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';

/// Pushes the user tapped (their deep link is opened).
class WatchOpenedNotifications {
  const WatchOpenedNotifications(this._repository);

  final NotificationsRepository _repository;

  Stream<PushEvent> call() => _repository.watchOpened();
}
