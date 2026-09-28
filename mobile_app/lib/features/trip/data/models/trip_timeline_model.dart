import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/trip_timeline.dart';

/// JSON mapping for [TripTimeline].
class TripTimelineModel extends TripTimeline {
  const TripTimelineModel({
    super.requestedAt,
    super.assignedAt,
    super.arrivedAt,
    super.startedAt,
    super.completedAt,
    super.cancelledAt,
  });

  factory TripTimelineModel.fromJson(Map<String, dynamic> json) =>
      TripTimelineModel(
        requestedAt: JsonReaders.date(json, 'requestedAt'),
        assignedAt: JsonReaders.date(json, 'assignedAt'),
        arrivedAt: JsonReaders.date(json, 'arrivedAt'),
        startedAt: JsonReaders.date(json, 'startedAt'),
        completedAt: JsonReaders.date(json, 'completedAt'),
        cancelledAt: JsonReaders.date(json, 'cancelledAt'),
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'requestedAt': requestedAt?.toIso8601String(),
    'assignedAt': assignedAt?.toIso8601String(),
    'arrivedAt': arrivedAt?.toIso8601String(),
    'startedAt': startedAt?.toIso8601String(),
    'completedAt': completedAt?.toIso8601String(),
    'cancelledAt': cancelledAt?.toIso8601String(),
  };
}

/// JSON mapping for [TripEvent].
class TripEventModel extends TripEvent {
  const TripEventModel({
    required super.type,
    required super.actor,
    super.createdAt,
  });

  factory TripEventModel.fromJson(Map<String, dynamic> json) => TripEventModel(
    type: JsonReaders.string(json, 'type'),
    actor: JsonReaders.string(json, 'actor'),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'type': type,
    'actor': actor,
    'createdAt': createdAt?.toIso8601String(),
  };
}
