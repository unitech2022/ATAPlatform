import 'package:equatable/equatable.dart';

/// `{ id, code, name }` of an airport in queue responses.
class AirportRef extends Equatable {
  const AirportRef({required this.id, required this.code, required this.name});

  final String id;
  final String code;
  final String name;

  @override
  List<Object?> get props => <Object?>[id, code, name];
}

/// `AirportQueueUpdated({ position, total, estimatedWaitMinutes })`.
class AirportQueuePosition extends Equatable {
  const AirportQueuePosition({
    required this.position,
    required this.total,
    this.estimatedWaitMinutes,
  });

  final int position;
  final int total;
  final int? estimatedWaitMinutes;

  @override
  List<Object?> get props => <Object?>[position, total, estimatedWaitMinutes];
}

/// `GET /driver/airport-queue`: in the queue (with the position) or not
/// (with the airport the driver could join, if any).
class AirportQueueStatus extends Equatable {
  const AirportQueueStatus({
    this.inQueue = false,
    this.airport,
    this.position,
    this.total,
    this.enteredAt,
    this.estimatedWaitMinutes,
    this.eligibleAirport,
  });

  final bool inQueue;
  final AirportRef? airport;
  final int? position;
  final int? total;
  final DateTime? enteredAt;
  final int? estimatedWaitMinutes;
  final AirportRef? eligibleAirport;

  /// The airport this status is about (queue or eligible).
  AirportRef? get relevantAirport => inQueue ? airport : eligibleAirport;

  /// Worth showing on the overview: queued or standing at an airport.
  bool get isRelevant => inQueue || eligibleAirport != null;

  AirportQueueStatus withPosition(AirportQueuePosition update) =>
      AirportQueueStatus(
        inQueue: true,
        airport: airport,
        position: update.position,
        total: update.total,
        enteredAt: enteredAt,
        estimatedWaitMinutes: update.estimatedWaitMinutes,
      );

  @override
  List<Object?> get props => <Object?>[
    inQueue,
    airport,
    position,
    total,
    enteredAt,
    estimatedWaitMinutes,
    eligibleAirport,
  ];
}
