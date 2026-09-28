import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';

/// JSON mapping of `ReliabilitySummary` (passenger and driver).
abstract final class ReliabilityModel {
  static ReliabilitySummary fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? next = JsonReaders.object(json, 'nextLevel');
    final Map<String, dynamic>? effects = JsonReaders.object(json, 'effects');
    return ReliabilitySummary(
      role: JsonReaders.optionalString(json, 'role') ?? 'passenger',
      level: RestrictionLevel.parse(JsonReaders.optionalString(json, 'level')),
      restrictedUntil: JsonReaders.date(json, 'restrictedUntil'),
      windowDays: JsonReaders.optionalInteger(json, 'windowDays') ?? 30,
      tripsAccepted: JsonReaders.integer(json, 'tripsAccepted'),
      tripsCompleted: JsonReaders.integer(json, 'tripsCompleted'),
      cancellationsAtFault: JsonReaders.integer(json, 'cancellationsAtFault'),
      cancellationRate: JsonReaders.number(json, 'cancellationRate'),
      reliabilityRate: JsonReaders.optionalNumber(json, 'reliabilityRate') ?? 1,
      noShowCount: JsonReaders.integer(json, 'noShowCount'),
      penaltyPoints: JsonReaders.integer(json, 'penaltyPoints'),
      nextLevel: next == null
          ? null
          : ReliabilityNextLevel(
              level: RestrictionLevel.parse(
                JsonReaders.optionalString(next, 'level'),
              ),
              minPenaltyPoints: JsonReaders.optionalInteger(
                next,
                'minPenaltyPoints',
              ),
              minCancellationRate: JsonReaders.optionalNumber(
                next,
                'minCancellationRate',
              ),
            ),
      recentEvents: JsonReaders.objects(
        json,
        'recentEvents',
      ).map(_event).toList(growable: false),
      offersReceived: JsonReaders.optionalInteger(json, 'offersReceived'),
      offersAccepted: JsonReaders.optionalInteger(json, 'offersAccepted'),
      acceptanceRate: JsonReaders.optionalNumber(json, 'acceptanceRate'),
      matchingFactor: effects == null
          ? null
          : JsonReaders.optionalNumber(effects, 'matchingFactor'),
      incentiveMultiplier: effects == null
          ? null
          : JsonReaders.optionalNumber(effects, 'incentiveMultiplier'),
    );
  }

  static ReliabilityEvent _event(Map<String, dynamic> json) => ReliabilityEvent(
    tripId: JsonReaders.string(json, 'tripId'),
    tripNumber: JsonReaders.string(json, 'tripNumber'),
    stage: JsonReaders.string(json, 'stage'),
    reasonName: JsonReaders.string(json, 'reasonName'),
    feeCharged: JsonReaders.number(json, 'feeCharged'),
    penaltyPoints: JsonReaders.integer(json, 'penaltyPoints'),
    excuseStatus:
        JsonReaders.optionalString(json, 'excuseStatus') ?? 'not_applicable',
    createdAt: JsonReaders.date(json, 'createdAt'),
  );
}
