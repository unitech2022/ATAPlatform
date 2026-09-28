import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads `GET /me/notification-preferences`.
class GetNotificationPreferences
    implements UseCase<NotificationPreferences, NoParams> {
  const GetNotificationPreferences(this._repository);

  final AccountRepository _repository;

  @override
  Future<Either<Failure, NotificationPreferences>> call(NoParams params) =>
      _repository.getNotificationPreferences();
}
