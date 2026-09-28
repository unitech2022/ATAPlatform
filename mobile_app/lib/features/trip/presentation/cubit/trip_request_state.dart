import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
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

  /// `422 quote_expired`: the quote must be refreshed and confirmed again.
  bool get isQuoteExpired => failure?.code == ErrorCodes.quoteExpired;

  /// `422 outstanding_balance`: the wallet is negative and must be topped
  /// up before requesting (`Payments:BlockOnOutstandingBalance`).
  bool get isOutstandingBalance =>
      failure?.code == ErrorCodes.outstandingBalance;

  /// Amount owed, from `details.amount` (positive).
  double? get outstandingAmount => failure?.numDetail(ErrorCodes.amount)?.abs();

  /// Range returned with `422 offer_out_of_range`, to clamp the offer.
  OfferBounds? get offerBounds {
    final Failure? failure = this.failure;
    if (failure == null || failure.code != ErrorCodes.offerOutOfRange) {
      return null;
    }
    final double? min = failure.numDetail(ErrorCodes.offerMin);
    final double? max = failure.numDetail(ErrorCodes.offerMax);
    return min == null || max == null ? null : OfferBounds(min: min, max: max);
  }

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
