import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:equatable/equatable.dart';

/// State of the "my rides" page.
class RidesState extends Equatable {
  const RidesState({
    this.trips = const <TripSummary>[],
    this.loading = false,
    this.failure,
  });

  final List<TripSummary> trips;
  final bool loading;
  final Failure? failure;

  bool get isEmpty => !loading && failure == null && trips.isEmpty;

  RidesState copyWith({
    List<TripSummary>? trips,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => RidesState(
    trips: trips ?? this.trips,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[trips, loading, failure];
}
