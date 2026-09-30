import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:equatable/equatable.dart';

/// State of `ScheduledDetailCubit` (`/scheduled/:tripId`).
class ScheduledDetailState extends Equatable {
  const ScheduledDetailState({
    required this.now,
    this.trip,
    this.loading = false,
    this.failure,
  });

  final DateTime now;
  final ScheduledTrip? trip;
  final bool loading;
  final Failure? failure;

  /// Time left until the pickup (drives the countdown).
  Duration? get remaining => trip?.untilStart(now);

  /// Cancelling now is inside the free window.
  bool get freeCancel => trip?.isFreeCancelAt(now) ?? false;

  ScheduledDetailState copyWith({
    DateTime? now,
    ScheduledTrip? trip,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => ScheduledDetailState(
    now: now ?? this.now,
    trip: trip ?? this.trip,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[now, trip, loading, failure];
}
