import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/account/data/models/notification_preferences_model.dart';
import 'package:ata_app/features/account/data/models/profile_model.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/auth/data/models/user_model.dart';

/// `/me*` endpoints.
class AccountRemoteDataSource {
  const AccountRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _mePath = '/me';
  static const String _prefsPath = '/me/notification-preferences';

  Future<ProfileModel> profile() async =>
      ProfileModel.fromJson(await _api.get(_mePath) as Map<String, dynamic>);

  Future<UserModel> updateLanguage(String languageCode) async =>
      UserModel.fromJson(
        await _api.patch(
              _mePath,
              body: <String, dynamic>{'language': languageCode},
            )
            as Map<String, dynamic>,
      );

  Future<NotificationPreferencesModel> notificationPreferences() async =>
      NotificationPreferencesModel.fromJson(
        await _api.get(_prefsPath) as Map<String, dynamic>,
      );

  Future<void> updateNotificationPreferences(NotificationPreferences prefs) =>
      _api.put(_prefsPath, body: NotificationPreferencesModel.toJson(prefs));

  Future<void> deleteAccount() => _api.delete(_mePath);
}
