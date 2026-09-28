import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/estimate_fare.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Ride-request sheet: categories, stops, options and the optional price
/// offer. The request itself is sent by `TripRequestCubit`.
class HomeCubit extends Cubit<HomeState> {
  HomeCubit({
    required this._getRideCategories,
    required this._updatePreferences,
    this._estimateFare = const EstimateFare(),
    bool preferFemaleDriver = false,
  }) : super(HomeState(preferFemaleDriver: preferFemaleDriver));

  final GetRideCategories _getRideCategories;
  final UpdatePassengerPreferences _updatePreferences;
  final EstimateFare _estimateFare;

  Future<void> loadCategories() async {
    emit(state.copyWith(loadingCategories: true, clearFailure: true));
    final result = await _getRideCategories(const NoParams());
    result.fold(
      (failure) => emit(
        state.copyWith(loadingCategories: false, categoriesFailure: failure),
      ),
      (List<RideCategory> categories) {
        final String? selected = categories.isEmpty
            ? null
            : categories.first.id;
        _emitWithEstimate(
          state.copyWith(
            loadingCategories: false,
            categories: categories,
            selectedCategoryId: selected,
          ),
        );
      },
    );
  }

  void selectCategory(String id) {
    final HomeState next = state.copyWith(selectedCategoryId: id);
    // Drop stops that exceed the new category's limit.
    final int max = next.maxStops;
    _emitWithEstimate(
      next.copyWith(
        stops: next.stops.length > max ? next.stops.sublist(0, max) : null,
      ),
    );
  }

  void addStop(String name) {
    if (!state.canAddStop) return;
    _emitWithEstimate(state.copyWith(stops: <String>[...state.stops, name]));
  }

  void removeStop(int index) {
    if (index < 0 || index >= state.stops.length) return;
    final List<String> stops = List<String>.of(state.stops)..removeAt(index);
    _emitWithEstimate(state.copyWith(stops: stops));
  }

  void selectRideTime(RideTime time) => emit(state.copyWith(rideTime: time));

  void selectPayment(PaymentOption option) =>
      emit(state.copyWith(payment: option));

  Future<void> togglePreferFemaleDriver() async {
    final bool next = !state.preferFemaleDriver;
    emit(state.copyWith(preferFemaleDriver: next));
    // Best effort persistence; the local choice is kept on failure.
    await _updatePreferences(
      PassengerPreferencesParams(preferFemaleDriver: next),
    );
  }

  /// Step between two proposed prices and the lowest accepted offer.
  static const double offeredPriceStep = 5;
  static const double minOfferedPrice = 5;

  /// Starts a price offer from the current estimate, or clears it.
  void toggleOfferedPrice() {
    if (state.hasOfferedPrice) {
      emit(state.copyWith(clearOfferedPrice: true));
    } else {
      emit(state.copyWith(offeredPrice: _roundPrice(state.estimate.price)));
    }
  }

  void adjustOfferedPrice(double delta) {
    final double? current = state.offeredPrice;
    if (current == null) return;
    final double next = current + delta;
    emit(
      state.copyWith(
        offeredPrice: next < minOfferedPrice ? minOfferedPrice : next,
      ),
    );
  }

  double _roundPrice(double price) {
    final double rounded =
        (price / offeredPriceStep).round() * offeredPriceStep;
    return rounded < minOfferedPrice ? minOfferedPrice : rounded;
  }

  void _emitWithEstimate(HomeState next) {
    final RideCategory? category = next.selectedCategory;
    emit(
      next.copyWith(
        estimate: category == null
            ? null
            : _estimateFare(category: category, stops: next.stops.length),
      ),
    );
  }
}
