import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the first page of notifications with the unread count.
class GetNotifications implements UseCase<NotificationsPage, NoParams> {
  const GetNotifications(this._repository);

  final NotificationsRepository _repository;

  @override
  Future<Either<Failure, NotificationsPage>> call(NoParams params) =>
      _repository.getNotifications();
}
