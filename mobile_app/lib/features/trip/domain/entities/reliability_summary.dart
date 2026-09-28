import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:equatable/equatable.dart';

/// Next threshold of the reliability ladder.
class ReliabilityNextLevel extends Equatable {
  const ReliabilityNextLevel({
    required this.level,
    this.minPenaltyPoints,
    this.minCancellationRate,
  });

  final RestrictionLevel level;
  final int? minPenaltyPoints;
  final double? minCancellationRate;

  @override
  List<Object?> get props => <Object?>[
    level,
    minPenaltyPoints,
    minCancellationRate,
  ];
}

/// One recent cancellation of the summary.
class ReliabilityEvent extends Equatable {
  const ReliabilityEvent({
    required this.tripId,
    this.tripNumber = '',
    this.stage = '',
    this.reasonName = '',
    this.feeCharged = 0,
    this.penaltyPoints = 0,
    this.excuseStatus = 'not_applicable',
    this.createdAt,
  });

  final String tripId;
  final String tripNumber;
  final String stage;
  final String reasonName;
  final double feeCharged;
  final int penaltyPoints;
  final String excuseStatus;
  final DateTime? createdAt;

  @override
  List<Object?> get props => <Object?>[
    tripId,
    tripNumber,
    stage,
    reasonName,
    feeCharged,
    penaltyPoints,
    excuseStatus,
    createdAt,
  ];
}

/// `GET /passenger/reliability` and `GET /driver/reliability`.
class ReliabilitySummary extends Equatable {
  const ReliabilitySummary({
    required this.role,
    this.level = RestrictionLevel.none,
    this.restrictedUntil,
    this.windowDays = 30,
    this.tripsAccepted = 0,
    this.tripsCompleted = 0,
    this.cancellationsAtFault = 0,
    this.cancellationRate = 0,
    this.reliabilityRate = 1,
    this.noShowCount = 0,
    this.penaltyPoints = 0,
    this.nextLevel,
    this.recentEvents = const <ReliabilityEvent>[],
    this.offersReceived,
    this.offersAccepted,
    this.acceptanceRate,
    this.matchingFactor,
    this.incentiveMultiplier,
  });

  final String role;
  final RestrictionLevel level;
  final DateTime? restrictedUntil;
  final int windowDays;
  final int tripsAccepted;
  final int tripsCompleted;
  final int cancellationsAtFault;

  /// 0..1.
  final double cancellationRate;
  final double reliabilityRate;
  final int noShowCount;
  final int penaltyPoints;
  final ReliabilityNextLevel? nextLevel;
  final List<ReliabilityEvent> recentEvents;

  // Driver only.
  final int? offersReceived;
  final int? offersAccepted;
  final double? acceptanceRate;

  /// `effects.matchingFactor` (1.0 or the deprioritize factor).
  final double? matchingFactor;

  /// `effects.incentiveMultiplier` (1 − reduction %).
  final double? incentiveMultiplier;

  bool get isDriver => role == 'driver';

  @override
  List<Object?> get props => <Object?>[
    role,
    level,
    restrictedUntil,
    windowDays,
    tripsAccepted,
    tripsCompleted,
    cancellationsAtFault,
    cancellationRate,
    reliabilityRate,
    noShowCount,
    penaltyPoints,
    nextLevel,
    recentEvents,
    offersReceived,
    offersAccepted,
    acceptanceRate,
    matchingFactor,
    incentiveMultiplier,
  ];
}
