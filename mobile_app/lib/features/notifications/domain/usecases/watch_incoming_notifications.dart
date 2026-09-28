import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';

/// Pushes received while the app is open (refreshes the inbox).
class WatchIncomingNotifications {
  const WatchIncomingNotifications(this._repository);

  final NotificationsRepository _repository;

  Stream<PushEvent> call() => _repository.watchIncoming();
}
