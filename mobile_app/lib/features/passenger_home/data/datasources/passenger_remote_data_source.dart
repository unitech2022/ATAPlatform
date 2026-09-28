import 'package:ata_app/core/network/api_client.dart';

/// `/passenger/preferences`.
class PassengerRemoteDataSource {
  const PassengerRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _preferencesPath = '/passenger/preferences';

  Future<void> updatePreferences(Map<String, dynamic> body) =>
      _api.patch(_preferencesPath, body: body);
}
