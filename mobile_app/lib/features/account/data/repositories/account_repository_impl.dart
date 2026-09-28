import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/features/account/data/datasources/account_remote_data_source.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:ata_app/features/auth/data/datasources/auth_local_data_source.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:fpdart/fpdart.dart';

/// [AccountRepository] backed by the API and preferences.
class AccountRepositoryImpl implements AccountRepository {
  const AccountRepositoryImpl({
    required this._remote,
    required this._authLocal,
    required this._prefs,
  });

  final AccountRemoteDataSource _remote;
  final AuthLocalDataSource _authLocal;
  final PreferencesStorage _prefs;

  @override
  Future<Either<Failure, Profile>> getProfile() => guard(_remote.profile);

  @override
  Future<Either<Failure, User>> updateLanguage(String languageCode) =>
      guard(() => _remote.updateLanguage(languageCode));

  @override
  Future<Either<Failure, NotificationPreferences>>
  getNotificationPreferences() => guard(_remote.notificationPreferences);

  @override
  Future<Either<Failure, NotificationPreferences>>
  updateNotificationPreferences(NotificationPreferences preferences) =>
      guard(() async {
        await _remote.updateNotificationPreferences(preferences);
        return preferences;
      });

  @override
  Future<Either<Failure, Unit>> deleteAccount() => guard(() async {
    await _remote.deleteAccount();
    await _authLocal.clear();
    return unit;
  });

  @override
  Future<Either<Failure, Unit>> registerDevice({String? pushToken}) =>
      guard(() async {
        await _remote.registerDevice(
          deviceId: _prefs.deviceId,
          pushToken: pushToken,
        );
        return unit;
      });

  @override
  String getSavedLocale() => _prefs.localeCode;

  @override
  Future<Either<Failure, Unit>> saveLocale(String languageCode) =>
      guard(() async {
        await _prefs.saveLocale(languageCode);
        return unit;
      });
}
