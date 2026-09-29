import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/estimate_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/request_trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Turns the home sheet into a real request: estimate, then create the trip.
/// Replaces the local "searching" flag of Step 1.
class TripRequestCubit extends Cubit<TripRequestState> {
  TripRequestCubit({
    required this._estimateTrip,
    required this._requestTrip,
    required this._cancelTrip,
  }) : super(const TripRequestState());

  final EstimateTrip _estimateTrip;
  final RequestTrip _requestTrip;
  final CancelTrip _cancelTrip;

  /// Requests the trip. An [TripRequest.offeredPrice] switches the pricing
  /// mode to `offer` (and drops any promo code, F15); otherwise the category
  /// price is `fixed`. Without a
  /// [TripRequest.quoteId] (F10) the legacy estimate runs first.
  Future<void> request(TripRequest draft) async {
    if (state.isBusy || state.isSearching) return;
    emit(
      state.copyWith(
        status: TripRequestStatus.requesting,
        clearFailure: true,
        clearTrip: true,
      ),
    );
    final bool offer = draft.offeredPrice != null;
    final TripRequest request = draft.copyWith(
      pricingMode: offer ? PricingMode.offer : PricingMode.fixed,
      clearPromoCode: offer,
    );
    TripEstimate? estimate;
    if (request.quoteId == null) {
      final estimateResult = await _estimateTrip(request);
      estimate = estimateResult.fold((failure) {
        emit(
          state.copyWith(status: TripRequestStatus.failure, failure: failure),
        );
        return null;
      }, (TripEstimate estimate) => estimate);
      if (estimate == null) return;
    }

    final result = await _requestTrip(request);
    emit(
      result.fold(
        (failure) => state.copyWith(
          status: TripRequestStatus.failure,
          estimate: estimate,
          failure: failure,
        ),
        (Trip trip) => state.copyWith(
          status: TripRequestStatus.searching,
          estimate: estimate,
          trip: trip,
        ),
      ),
    );
  }

  Future<void> cancel(String reasonCode, {String? note}) async {
    final Trip? trip = state.trip;
    if (trip == null || state.isBusy) return;
    emit(state.copyWith(status: TripRequestStatus.cancelling));
    final result = await _cancelTrip(
      CancelTripParams(
        tripId: trip.id,
        actor: TripActor.passenger,
        reasonCode: reasonCode,
        note: note,
      ),
    );
    emit(
      result.fold(
        (failure) =>
            state.copyWith(status: TripRequestStatus.failure, failure: failure),
        (_) => const TripRequestState(),
      ),
    );
  }

  /// Back to the form (after a failure or once the trip page took over).
  void reset() => emit(const TripRequestState());
}
