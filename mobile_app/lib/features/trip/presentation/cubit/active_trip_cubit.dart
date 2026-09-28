import 'dart:async';
import 'dart:math';

import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_active_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_driver_location.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Watches the passenger's active trip for the whole session. The router
/// sends the rider to `/trip` while [ActiveTripState.hasTrip] is true.
class ActiveTripCubit extends Cubit<ActiveTripState> {
  ActiveTripCubit({
    required this._watchActiveTrip,
    required this._watchDriverLocation,
    required this._getTrip,
    required this._cancelTrip,
    this._ticker = periodicTicker,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const ActiveTripState());

  final WatchActiveTrip _watchActiveTrip;
  final WatchDriverLocation _watchDriverLocation;
  final GetTrip _getTrip;
  final CancelTrip _cancelTrip;
  final Ticker _ticker;
  final DateTime Function() _now;

  static const Duration _tickInterval = Duration(seconds: 1);

  StreamSubscription<Trip?>? _trips;
  StreamSubscription<DriverLocationUpdate>? _locations;
  StreamSubscription<void>? _clock;

  /// Starts (or restores on app start) the feed. Idempotent.
  void start() {
    if (_trips != null) return;
    emit(state.copyWith(status: ActiveTripStatus.loading));
    _trips = _watchActiveTrip(TripActor.passenger).listen(_onTrip);
    _locations = _watchDriverLocation().listen(_onLocation);
  }

  Future<void> stop() async {
    await _trips?.cancel();
    await _locations?.cancel();
    await _clock?.cancel();
    _trips = null;
    _locations = null;
    _clock = null;
    if (!isClosed) emit(const ActiveTripState());
  }

  /// Shows a trip created by the home sheet before the feed delivers it.
  void adopt(Trip trip) => _apply(trip);

  Future<void> cancel(CancelReason reason, {String? note}) async {
    final Trip? trip = state.trip;
    if (trip == null || !state.canCancel) return;
    emit(state.copyWith(cancelling: true, clearFailure: true));
    final result = await _cancelTrip(
      CancelTripParams(
        tripId: trip.id,
        actor: TripActor.passenger,
        reason: reason,
        note: note,
      ),
    );
    result.fold(
      (failure) => emit(state.copyWith(cancelling: false, failure: failure)),
      (Trip cancelled) => _apply(cancelled, cancelling: false),
    );
  }

  /// Leaves the receipt / cancellation screen.
  void dismiss() {
    if (state.trip?.status.isTerminal ?? false) {
      emit(
        state.copyWith(
          clearTrip: true,
          clearLocation: true,
          clearFailure: true,
        ),
      );
    }
  }

  Future<void> _onTrip(Trip? trip) async {
    final Trip? current = state.trip;
    if (trip != null) {
      _apply(trip);
      return;
    }
    if (current == null || current.status.isTerminal) {
      emit(state.copyWith(status: ActiveTripStatus.watching));
      return;
    }
    // The feed lost a trip we still show: fetch its final state once.
    final result = await _getTrip(current.id);
    result.fold(
      (_) => emit(
        state.copyWith(
          status: ActiveTripStatus.watching,
          clearTrip: true,
          clearLocation: true,
        ),
      ),
      _apply,
    );
  }

  void _onLocation(DriverLocationUpdate update) {
    if (isClosed || update.tripId != state.trip?.id) return;
    emit(state.copyWith(driverLocation: update));
  }

  void _apply(Trip trip, {bool? cancelling}) {
    if (isClosed) return;
    final bool sameTrip = state.trip?.id == trip.id;
    emit(
      state.copyWith(
        status: ActiveTripStatus.watching,
        trip: trip,
        cancelling: cancelling,
        waitingSeconds: _waitingFor(trip),
        clearLocation: !sameTrip,
      ),
    );
    _syncClock(trip);
  }

  void _syncClock(Trip trip) {
    if (trip.status.isWaiting) {
      _clock ??= _ticker(_tickInterval).listen((_) {
        final Trip? current = state.trip;
        if (current != null && !isClosed) {
          emit(state.copyWith(waitingSeconds: _waitingFor(current)));
        }
      });
    } else {
      _clock?.cancel();
      _clock = null;
    }
  }

  int _waitingFor(Trip trip) {
    final DateTime? arrivedAt = trip.timeline.arrivedAt;
    if (arrivedAt == null) return trip.waitingSeconds;
    return max(0, _now().difference(arrivedAt).inSeconds);
  }

  @override
  Future<void> close() async {
    await _trips?.cancel();
    await _locations?.cancel();
    await _clock?.cancel();
    return super.close();
  }
}
