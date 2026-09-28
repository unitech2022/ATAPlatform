import 'package:equatable/equatable.dart';

/// Timestamps of the main transitions.
class TripTimeline extends Equatable {
  const TripTimeline({
    this.requestedAt,
    this.assignedAt,
    this.arrivedAt,
    this.startedAt,
    this.completedAt,
    this.cancelledAt,
  });

  final DateTime? requestedAt;
  final DateTime? assignedAt;
  final DateTime? arrivedAt;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final DateTime? cancelledAt;

  @override
  List<Object?> get props => <Object?>[
    requestedAt,
    assignedAt,
    arrivedAt,
    startedAt,
    completedAt,
    cancelledAt,
  ];
}

/// One `trip_events` row.
class TripEvent extends Equatable {
  const TripEvent({required this.type, required this.actor, this.createdAt});

  final String type;
  final String actor;
  final DateTime? createdAt;

  @override
  List<Object?> get props => <Object?>[type, actor, createdAt];
}
