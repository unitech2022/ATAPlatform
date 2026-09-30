import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:equatable/equatable.dart';

/// Where a scheduled booking stands, as shown to the rider.
enum ScheduledPhase {
  /// Booked; no driver has reserved it yet.
  waitingForDriver,

  /// A driver reserved it and has yet to confirm.
  driverReserved,

  /// The driver confirmed (first prompt) or is assigned.
  driverConfirmed,

  /// The normal driver search started.
  searching,

  /// A driver is on the way / the trip runs (leaves the scheduled flow).
  inProgress,

  /// Cancelled, no drivers or completed.
  ended,
}

/// A `Trip` booked for later (`status = scheduled`) with its `scheduling`.
class ScheduledTrip extends Equatable {
  const ScheduledTrip(this.trip);

  final Trip trip;

  String get id => trip.id;
  DateTime? get scheduledAt => trip.scheduledAt;
  TripScheduling? get scheduling => trip.scheduling;
  TripReservationInfo? get reservation => scheduling?.reservation;

  /// Free-cancel deadline reported by the API.
  DateTime? get freeCancelUntil => scheduling?.freeCancelUntil;

  bool get isWaiting => trip.status == TripStage.scheduled;

  ScheduledPhase get phase {
    final TripStage status = trip.status;
    if (status.isTerminal) return ScheduledPhase.ended;
    if (status.isSearching) return ScheduledPhase.searching;
    if (status != TripStage.scheduled) return ScheduledPhase.inProgress;
    final TripReservationInfo? r = reservation;
    if (r == null || !r.status.isActive) return ScheduledPhase.waitingForDriver;
    return r.isConfirmed
        ? ScheduledPhase.driverConfirmed
        : ScheduledPhase.driverReserved;
  }

  /// Cancelling now is free (before the API's deadline).
  bool isFreeCancelAt(DateTime now) {
    final DateTime? until = freeCancelUntil;
    return until != null && now.isBefore(until);
  }

  /// Time left until the pickup, never negative.
  Duration? untilStart(DateTime now) {
    final DateTime? at = scheduledAt;
    if (at == null) return null;
    final Duration left = at.difference(now);
    return left.isNegative ? Duration.zero : left;
  }

  @override
  List<Object?> get props => <Object?>[trip];
}
