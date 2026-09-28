import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/driver_onboarding/data/models/driver_application_model.dart';

/// `/driver/application`.
class DriverOnboardingRemoteDataSource {
  const DriverOnboardingRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _applicationPath = '/driver/application';

  Future<DriverApplicationModel> application() async =>
      DriverApplicationModel.fromJson(
        await _api.get(_applicationPath) as Map<String, dynamic>,
      );
}
