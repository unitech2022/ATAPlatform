import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:equatable/equatable.dart';

/// Progress of a ride request from the home sheet.
enum TripRequestStatus { idle, requesting, searching, cancelling, failure }

/// State of [TripRequestCubit].
class TripRequestState extends Equatable {
  const TripRequestState({
    this.status = TripRequestStatus.idle,
    this.trip,
    this.estimate,
    this.failure,
  });

  final TripRequestStatus status;

  /// The created trip (status `searching`) once the request succeeded.
  final Trip? trip;
  final TripEstimate? estimate;
  final Failure? failure;

  bool get isBusy =>
      status == TripRequestStatus.requesting ||
      status == TripRequestStatus.cancelling;
  bool get isSearching => status == TripRequestStatus.searching;

  TripRequestState copyWith({
    TripRequestStatus? status,
    Trip? trip,
    TripEstimate? estimate,
    Failure? failure,
    bool clearFailure = false,
    bool clearTrip = false,
  }) => TripRequestState(
    status: status ?? this.status,
    trip: clearTrip ? null : trip ?? this.trip,
    estimate: estimate ?? this.estimate,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[status, trip, estimate, failure];
}
