import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Saves `PUT /me/notification-preferences`.
class UpdateNotificationPreferences
    implements UseCase<NotificationPreferences, NotificationPreferences> {
  const UpdateNotificationPreferences(this._repository);

  final AccountRepository _repository;

  @override
  Future<Either<Failure, NotificationPreferences>> call(
    NotificationPreferences params,
  ) => _repository.updateNotificationPreferences(params);
}
