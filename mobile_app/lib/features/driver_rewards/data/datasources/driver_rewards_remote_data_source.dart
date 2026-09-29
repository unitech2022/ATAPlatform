import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/driver_rewards/data/models/driver_rewards_models.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';

/// `/driver/tier` and `/driver/incentives*`.
class DriverRewardsRemoteDataSource {
  const DriverRewardsRemoteDataSource(this._api);

  final ApiClient _api;

  static const String tierPath = '/driver/tier';
  static const String incentivesPath = '/driver/incentives';

  Future<DriverTierInfo> tier() async => DriverRewardsModels.tier(
    await _api.get(tierPath) as Map<String, dynamic>,
  );

  Future<List<Incentive>> incentives(IncentiveTab tab) async {
    final Object? body = await _api.get(
      incentivesPath,
      query: <String, dynamic>{'status': tab.apiValue},
    );
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    return items is List<dynamic>
        ? items
              .whereType<Map<String, dynamic>>()
              .map(DriverRewardsModels.incentive)
              .toList(growable: false)
        : const <Incentive>[];
  }

  Future<Incentive> incentive(String id) async => DriverRewardsModels.incentive(
    await _api.get('$incentivesPath/$id') as Map<String, dynamic>,
  );

  Future<void> optIn(String id) => _api.post('$incentivesPath/$id/opt-in');
}
