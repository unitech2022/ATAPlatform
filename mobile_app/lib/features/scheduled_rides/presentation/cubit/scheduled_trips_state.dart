import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:equatable/equatable.dart';

/// State of `ScheduledTripsCubit` ("رحلاتي المجدولة", rider).
class ScheduledTripsState extends Equatable {
  const ScheduledTripsState({
    this.trips = const <ScheduledTrip>[],
    this.loading = false,
    this.loaded = false,
    this.failure,
  });

  final List<ScheduledTrip> trips;
  final bool loading;
  final bool loaded;
  final Failure? failure;

  bool get isEmpty => loaded && failure == null && trips.isEmpty;

  ScheduledTripsState copyWith({
    List<ScheduledTrip>? trips,
    bool? loading,
    bool? loaded,
    Failure? failure,
    bool clearFailure = false,
  }) => ScheduledTripsState(
    trips: trips ?? this.trips,
    loading: loading ?? this.loading,
    loaded: loaded ?? this.loaded,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[trips, loading, loaded, failure];
}
