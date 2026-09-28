import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:fpdart/fpdart.dart';

/// `/me` endpoints and local app preferences.
abstract interface class AccountRepository {
  Future<Either<Failure, Profile>> getProfile();
  Future<Either<Failure, User>> updateLanguage(String languageCode);
  Future<Either<Failure, NotificationPreferences>> getNotificationPreferences();
  Future<Either<Failure, NotificationPreferences>>
  updateNotificationPreferences(NotificationPreferences preferences);
  Future<Either<Failure, Unit>> deleteAccount();

  /// `PUT /me/devices` with the push subscription id (diagnostics only).
  Future<Either<Failure, Unit>> registerDevice({String? pushToken});

  /// Locally persisted locale (`ar` by default).
  String getSavedLocale();
  Future<Either<Failure, Unit>> saveLocale(String languageCode);
}
