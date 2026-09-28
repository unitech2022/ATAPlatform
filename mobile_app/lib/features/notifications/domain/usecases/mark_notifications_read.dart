import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Marks the given notifications (or all, with `null`) as read.
class MarkNotificationsRead implements UseCase<Unit, List<String>?> {
  const MarkNotificationsRead(this._repository);

  final NotificationsRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(List<String>? params) =>
      _repository.markRead(params);
}
