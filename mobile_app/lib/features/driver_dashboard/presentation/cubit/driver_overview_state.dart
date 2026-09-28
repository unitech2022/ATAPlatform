import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:equatable/equatable.dart';

/// State of the overview tab: earnings and recent trips.
class DriverOverviewState extends Equatable {
  const DriverOverviewState({
    this.earnings = const EarningsSummary.empty(),
    this.trips = const <TripSummary>[],
    this.loading = false,
    this.failure,
  });

  final EarningsSummary earnings;
  final List<TripSummary> trips;
  final bool loading;
  final Failure? failure;

  DriverOverviewState copyWith({
    EarningsSummary? earnings,
    List<TripSummary>? trips,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => DriverOverviewState(
    earnings: earnings ?? this.earnings,
    trips: trips ?? this.trips,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[earnings, trips, loading, failure];
}
