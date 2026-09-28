import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:equatable/equatable.dart';

/// Whether the passenger feed is running.
enum ActiveTripStatus { idle, loading, watching }

/// State of [ActiveTripCubit]: the passenger's current trip.
class ActiveTripState extends Equatable {
  const ActiveTripState({
    this.status = ActiveTripStatus.idle,
    this.trip,
    this.driverLocation,
    this.waitingSeconds = 0,
    this.cancelling = false,
    this.failure,
  });

  final ActiveTripStatus status;
  final Trip? trip;
  final DriverLocationUpdate? driverLocation;

  /// Seconds since the driver arrived (ticks while waiting).
  final int waitingSeconds;
  final bool cancelling;
  final Failure? failure;

  bool get hasTrip => trip != null;
  bool get isLoading => status == ActiveTripStatus.loading;
  TripStage? get stage => trip?.status;
  int? get etaMinutes => driverLocation?.etaMinutes;
  bool get canCancel => (stage?.canCancel ?? false) && !cancelling;

  ActiveTripState copyWith({
    ActiveTripStatus? status,
    Trip? trip,
    DriverLocationUpdate? driverLocation,
    int? waitingSeconds,
    bool? cancelling,
    Failure? failure,
    bool clearTrip = false,
    bool clearLocation = false,
    bool clearFailure = false,
  }) => ActiveTripState(
    status: status ?? this.status,
    trip: clearTrip ? null : trip ?? this.trip,
    driverLocation: clearLocation
        ? null
        : driverLocation ?? this.driverLocation,
    waitingSeconds: waitingSeconds ?? this.waitingSeconds,
    cancelling: cancelling ?? this.cancelling,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    trip,
    driverLocation,
    waitingSeconds,
    cancelling,
    failure,
  ];
}
