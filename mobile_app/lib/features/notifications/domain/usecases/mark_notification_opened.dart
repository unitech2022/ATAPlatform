import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Records that a notification was opened (`POST /notifications/{id}/opened`).
class MarkNotificationOpened implements UseCase<Unit, String> {
  const MarkNotificationOpened(this._repository);

  final NotificationsRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String id) => _repository.markOpened(id);
}
