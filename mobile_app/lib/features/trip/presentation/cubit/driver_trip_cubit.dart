import 'dart:async';

import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/advance_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/verify_pin.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_active_trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The driver's active trip: one primary action per status, PIN entry and
/// cancellation. The router keeps the driver on `/driver/trip` while a trip
/// is present.
class DriverTripCubit extends Cubit<DriverTripState> {
  DriverTripCubit({
    required this._watchActiveTrip,
    required this._advanceTrip,
    required this._verifyPin,
    required this._cancelTrip,
  }) : super(const DriverTripState());

  final WatchActiveTrip _watchActiveTrip;
  final AdvanceTrip _advanceTrip;
  final VerifyPin _verifyPin;
  final CancelTrip _cancelTrip;

  StreamSubscription<Trip?>? _trips;

  /// Starts (or restores) the driver feed. Idempotent.
  void start() {
    if (_trips != null) return;
    emit(state.copyWith(status: DriverTripStatus.loading));
    _trips = _watchActiveTrip(TripActor.driver).listen(_onTrip);
  }

  Future<void> stop() async {
    await _trips?.cancel();
    _trips = null;
    if (!isClosed) emit(const DriverTripState());
  }

  /// Shows the trip returned by an accepted offer right away.
  void adopt(Trip trip) => _apply(trip);

  /// Runs [DriverTripState.nextStep]; [at] is sent with `complete`.
  Future<void> advance({GeoPoint? at}) async {
    final Trip? trip = state.trip;
    final TripStep? step = state.nextStep;
    if (trip == null || step == null || state.busy) return;
    emit(state.copyWith(busy: true, clearFailure: true));
    final result = await _advanceTrip(
      AdvanceTripParams(tripId: trip.id, step: step, at: at),
    );
    result.fold(
      (failure) => emit(state.copyWith(busy: false, failure: failure)),
      (Trip updated) => _apply(updated, busy: false),
    );
  }

  void addPinDigit(String digit) {
    if (state.busy || state.isPinComplete) return;
    emit(state.copyWith(pin: state.pin + digit, clearFailure: true));
  }

  void deletePinDigit() {
    if (state.busy || state.pin.isEmpty) return;
    emit(
      state.copyWith(
        pin: state.pin.substring(0, state.pin.length - 1),
        clearFailure: true,
      ),
    );
  }

  Future<void> submitPin() async {
    final Trip? trip = state.trip;
    if (trip == null || !state.isPinComplete || state.busy) return;
    emit(state.copyWith(busy: true, clearFailure: true));
    final result = await _verifyPin(
      VerifyPinParams(tripId: trip.id, pin: state.pin),
    );
    result.fold(
      (failure) => emit(state.copyWith(busy: false, pin: '', failure: failure)),
      (Trip updated) => _apply(updated, busy: false, pin: ''),
    );
  }

  Future<void> cancel(String reasonCode, {String? note}) async {
    final Trip? trip = state.trip;
    if (trip == null || !state.canCancel) return;
    emit(state.copyWith(busy: true, clearFailure: true));
    final result = await _cancelTrip(
      CancelTripParams(
        tripId: trip.id,
        actor: TripActor.driver,
        reasonCode: reasonCode,
        note: note,
      ),
    );
    result.fold(
      (failure) => emit(state.copyWith(busy: false, failure: failure)),
      (Trip cancelled) => _apply(cancelled, busy: false),
    );
  }

  /// Leaves the completed / cancelled summary.
  void dismiss() {
    if (state.trip?.status.isTerminal ?? false) {
      emit(state.copyWith(clearTrip: true, clearFailure: true, pin: ''));
    }
  }

  void _onTrip(Trip? trip) {
    if (isClosed) return;
    if (trip != null) {
      _apply(trip);
      return;
    }
    final Trip? current = state.trip;
    if (current != null && current.status.isTerminal) {
      emit(state.copyWith(status: DriverTripStatus.watching));
    } else {
      emit(
        state.copyWith(
          status: DriverTripStatus.watching,
          clearTrip: true,
          pin: '',
        ),
      );
    }
  }

  void _apply(Trip trip, {bool? busy, String? pin}) {
    if (isClosed) return;
    emit(
      state.copyWith(
        status: DriverTripStatus.watching,
        trip: trip,
        busy: busy,
        pin: pin,
      ),
    );
  }

  @override
  Future<void> close() async {
    await _trips?.cancel();
    return super.close();
  }
}
