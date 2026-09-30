import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:equatable/equatable.dart';

/// Which confirmation prompt is due.
enum ConfirmationKind {
  /// T-60: keeps the reservation (`reserved` -> `confirmed`).
  first,

  /// T-15: assigns the driver (`confirmed` -> `assigned`).
  finalStep,
}

/// A driver's reservation of a scheduled trip (`docs/11` §F17.4).
class Reservation extends Equatable {
  const Reservation({
    required this.id,
    required this.tripId,
    required this.status,
    required this.scheduledAt,
    this.source = 'marketplace',
    this.pickup,
    this.dropoff,
    this.passengerFirstName = '',
    this.estimatedFare = 0,
    this.driverNetEarnings = 0,
    this.confirmDeadline,
    this.finalConfirmDeadline,
    this.freeReleaseUntil,
    this.reservedAt,
    this.penaltyPoints = 0,
  });

  final String id;
  final String tripId;
  final ReservationStatus status;

  /// `marketplace`, `favorite` or `admin`.
  final String source;
  final DateTime scheduledAt;
  final TripStop? pickup;
  final TripStop? dropoff;
  final String passengerFirstName;
  final double estimatedFare;
  final double driverNetEarnings;

  /// Deadline of the first confirmation, while it is being asked.
  final DateTime? confirmDeadline;

  /// Deadline of the final confirmation, while it is being asked.
  final DateTime? finalConfirmDeadline;

  /// Releasing before this is free; later costs reliability points.
  final DateTime? freeReleaseUntil;
  final DateTime? reservedAt;

  /// Points charged when the reservation was released (`release` response).
  final int penaltyPoints;

  bool get isActive => status.isActive;

  /// The driver can still give the trip up (before the assignment).
  bool get canRelease =>
      status == ReservationStatus.reserved ||
      status == ReservationStatus.confirmed;

  /// The confirmation prompt due at [now], or `null`.
  ConfirmationKind? pendingAt(DateTime now) {
    if (status == ReservationStatus.reserved &&
        confirmDeadline != null &&
        now.isBefore(confirmDeadline!)) {
      return ConfirmationKind.first;
    }
    if (status == ReservationStatus.confirmed &&
        finalConfirmDeadline != null &&
        now.isBefore(finalConfirmDeadline!)) {
      return ConfirmationKind.finalStep;
    }
    return null;
  }

  /// Deadline of the prompt due at [now].
  DateTime? deadlineAt(DateTime now) => switch (pendingAt(now)) {
    ConfirmationKind.first => confirmDeadline,
    ConfirmationKind.finalStep => finalConfirmDeadline,
    null => null,
  };

  /// Releasing at [now] is after the free window (penalty points).
  bool isLateReleaseAt(DateTime now) =>
      freeReleaseUntil != null && !now.isBefore(freeReleaseUntil!);

  @override
  List<Object?> get props => <Object?>[
    id,
    tripId,
    status,
    source,
    scheduledAt,
    pickup,
    dropoff,
    passengerFirstName,
    estimatedFare,
    driverNetEarnings,
    confirmDeadline,
    finalConfirmDeadline,
    freeReleaseUntil,
    reservedAt,
    penaltyPoints,
  ];
}

/// Result of `POST /driver/scheduled/{tripId}/confirm`; the final
/// confirmation also returns the now-assigned [trip].
class ConfirmResult extends Equatable {
  const ConfirmResult({required this.reservation, this.trip});

  final Reservation reservation;
  final Trip? trip;

  @override
  List<Object?> get props => <Object?>[reservation, trip];
}
