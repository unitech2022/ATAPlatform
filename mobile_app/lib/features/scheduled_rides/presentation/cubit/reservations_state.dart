import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:equatable/equatable.dart';

/// What the driver just did with a reservation (for the page to announce).
enum ReservationEvent { confirmed, finalConfirmed, released }

/// State of `ReservationsCubit`.
class ReservationsState extends Equatable {
  const ReservationsState({
    required this.now,
    this.list = ReservationList.active,
    this.active = const <Reservation>[],
    this.history = const <Reservation>[],
    this.loading = false,
    this.loaded = false,
    this.failure,
    this.busyTripId,
    this.actionFailure,
    this.event,
    this.assignedTrip,
    this.releasedPenaltyPoints = 0,
  });

  /// The clock at the last tick (drives the confirmation countdowns).
  final DateTime now;
  final ReservationList list;
  final List<Reservation> active;
  final List<Reservation> history;
  final bool loading;
  final bool loaded;
  final Failure? failure;

  /// Trip being confirmed / released.
  final String? busyTripId;

  /// `reservation_not_confirmable` and other action errors.
  final Failure? actionFailure;
  final ReservationEvent? event;

  /// The trip returned by the final confirmation (the driver's active trip).
  final Trip? assignedTrip;

  /// Points charged by the last release.
  final int releasedPenaltyPoints;

  List<Reservation> get current =>
      list == ReservationList.active ? active : history;
  bool get isEmpty => loaded && failure == null && current.isEmpty;
  bool get isBusy => busyTripId != null;

  /// Reservations with a confirmation prompt due now, most urgent first.
  List<Reservation> get pending {
    final List<Reservation> due = active
        .where((Reservation r) => r.pendingAt(now) != null)
        .toList();
    due.sort(
      (Reservation a, Reservation b) =>
          a.deadlineAt(now)!.compareTo(b.deadlineAt(now)!),
    );
    return due;
  }

  Reservation? get nextPending => pending.firstOrNull;

  /// The reservation of [tripId] in either list.
  Reservation? find(String tripId) {
    for (final Reservation r in <Reservation>[...active, ...history]) {
      if (r.tripId == tripId) return r;
    }
    return null;
  }

  ReservationsState copyWith({
    DateTime? now,
    ReservationList? list,
    List<Reservation>? active,
    List<Reservation>? history,
    bool? loading,
    bool? loaded,
    Failure? failure,
    String? busyTripId,
    Failure? actionFailure,
    ReservationEvent? event,
    Trip? assignedTrip,
    int? releasedPenaltyPoints,
    bool clearFailure = false,
    bool clearBusy = false,
    bool clearAction = false,
    bool clearEvent = false,
  }) => ReservationsState(
    now: now ?? this.now,
    list: list ?? this.list,
    active: active ?? this.active,
    history: history ?? this.history,
    loading: loading ?? this.loading,
    loaded: loaded ?? this.loaded,
    failure: clearFailure ? null : failure ?? this.failure,
    busyTripId: clearBusy ? null : busyTripId ?? this.busyTripId,
    actionFailure: clearAction ? null : actionFailure ?? this.actionFailure,
    event: clearEvent ? null : event ?? this.event,
    assignedTrip: clearEvent ? null : assignedTrip ?? this.assignedTrip,
    releasedPenaltyPoints: releasedPenaltyPoints ?? this.releasedPenaltyPoints,
  );

  @override
  List<Object?> get props => <Object?>[
    now,
    list,
    active,
    history,
    loading,
    loaded,
    failure,
    busyTripId,
    actionFailure,
    event,
    assignedTrip,
    releasedPenaltyPoints,
  ];
}
