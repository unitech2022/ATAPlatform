import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of `GET /driver/tier` and `/driver/incentives*`
/// (`docs/10` §F15.7, §F15.10).
abstract final class DriverRewardsModels {
  static DriverTierInfo tier(Map<String, dynamic> json) {
    final Map<String, dynamic> m =
        JsonReaders.object(json, 'metrics') ?? const <String, dynamic>{};
    final Map<String, dynamic>? r = JsonReaders.object(
      json,
      'nextRequirements',
    );
    final Map<String, dynamic> b =
        JsonReaders.object(json, 'benefits') ?? const <String, dynamic>{};
    return DriverTierInfo(
      tier: DriverTier.parse(JsonReaders.optionalString(json, 'tier')),
      nextTier: DriverTier.tryParse(
        JsonReaders.optionalString(json, 'nextTier'),
      ),
      periodDays: JsonReaders.optionalInteger(json, 'periodDays') ?? 28,
      metrics: TierMetrics(
        completedTrips: JsonReaders.integer(m, 'completedTrips'),
        ratingAvg: JsonReaders.number(m, 'ratingAvg'),
        acceptanceRate: JsonReaders.number(m, 'acceptanceRate'),
        cancellationRate: JsonReaders.number(m, 'cancellationRate'),
      ),
      nextRequirements: r == null
          ? null
          : TierRequirements(
              minCompletedTrips: JsonReaders.integer(r, 'minCompletedTrips'),
              minRatingAvg: JsonReaders.number(r, 'minRatingAvg'),
              minAcceptanceRate: JsonReaders.number(r, 'minAcceptanceRate'),
              maxCancellationRate:
                  JsonReaders.optionalNumber(r, 'maxCancellationRate') ?? 1,
            ),
      benefits: TierBenefits(
        commissionDiscountPercent: JsonReaders.number(
          b,
          'commissionDiscountPercent',
        ),
        text: JsonReaders.string(b, 'text'),
      ),
      recalculatesAt: JsonReaders.date(json, 'recalculatesAt'),
    );
  }

  static Incentive incentive(Map<String, dynamic> json) {
    final Map<String, dynamic>? w = JsonReaders.object(json, 'window');
    final Map<String, dynamic>? p = JsonReaders.object(json, 'progress');
    final Object? zones = json['zones'];
    final Object? categories = json['rideCategoryCodes'];
    return Incentive(
      id: JsonReaders.string(json, 'id'),
      name: JsonReaders.string(json, 'name'),
      description: JsonReaders.string(json, 'description'),
      type: JsonReaders.string(json, 'type'),
      targetTrips: JsonReaders.integer(json, 'targetTrips'),
      rewardAmount: JsonReaders.number(json, 'rewardAmount'),
      periodStart: JsonReaders.date(json, 'periodStart'),
      periodEnd: JsonReaders.date(json, 'periodEnd'),
      window: w == null
          ? null
          : IncentiveWindow(
              daysOfWeek: w['daysOfWeek'] is List<dynamic>
                  ? (w['daysOfWeek'] as List<dynamic>)
                        .whereType<num>()
                        .map((num d) => d.toInt())
                        .toList(growable: false)
                  : const <int>[],
              from: JsonReaders.optionalString(w, 'from'),
              to: JsonReaders.optionalString(w, 'to'),
            ),
      zones: zones is List<dynamic>
          ? JsonReaders.objects(json, 'zones')
                .map(
                  (Map<String, dynamic> z) => IncentiveZone(
                    id: JsonReaders.string(z, 'id'),
                    name: JsonReaders.string(z, 'name'),
                  ),
                )
                .toList(growable: false)
          : null,
      rideCategoryCodes: categories is List<dynamic>
          ? categories.map((Object? c) => c.toString()).toList(growable: false)
          : null,
      requiresOptIn: json['requiresOptIn'] == true,
      optedIn: json['optedIn'] == true,
      progress: p == null
          ? null
          : IncentiveProgress(
              completedTrips: JsonReaders.integer(p, 'completedTrips'),
              status: IncentiveProgressStatus.parse(
                JsonReaders.optionalString(p, 'status'),
              ),
              rewardAmount: JsonReaders.optionalNumber(p, 'rewardAmount'),
              paidAt: JsonReaders.date(p, 'paidAt'),
            ),
    );
  }
}
