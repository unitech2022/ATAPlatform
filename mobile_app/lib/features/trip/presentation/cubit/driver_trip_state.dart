import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:equatable/equatable.dart';

/// Whether the driver feed is running.
enum DriverTripStatus { idle, loading, watching }

/// State of [DriverTripCubit]: the driver's current trip and PIN entry.
class DriverTripState extends Equatable {
  const DriverTripState({
    this.status = DriverTripStatus.idle,
    this.trip,
    this.busy = false,
    this.pin = '',
    this.failure,
  });

  static const int pinLength = 4;

  final DriverTripStatus status;
  final Trip? trip;
  final bool busy;
  final String pin;
  final Failure? failure;

  bool get hasTrip => trip != null;
  TripStage? get stage => trip?.status;
  bool get isPinComplete => pin.length == pinLength;
  bool get needsPin => stage?.isWaiting ?? false;
  bool get canCancel => (stage?.canCancel ?? false) && !busy;

  /// The single primary action for the current status, if any.
  TripStep? get nextStep => switch (stage) {
    TripStage.driverAssigned => TripStep.enRoute,
    TripStage.driverEnRoute => TripStep.arrived,
    TripStage.pinVerified => TripStep.start,
    TripStage.inTrip => TripStep.complete,
    _ => null,
  };

  DriverTripState copyWith({
    DriverTripStatus? status,
    Trip? trip,
    bool? busy,
    String? pin,
    Failure? failure,
    bool clearTrip = false,
    bool clearFailure = false,
  }) => DriverTripState(
    status: status ?? this.status,
    trip: clearTrip ? null : trip ?? this.trip,
    busy: busy ?? this.busy,
    pin: pin ?? this.pin,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[status, trip, busy, pin, failure];
}
