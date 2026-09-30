import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_detail_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_trip.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// One scheduled booking: the trip with its reserved driver and a countdown
/// to the pickup. The clock ticks every second and the trip is re-read every
/// [refreshEvery] ticks, so a reservation or the search start shows up.
class ScheduledDetailCubit extends Cubit<ScheduledDetailState> {
  ScheduledDetailCubit({
    required this.tripId,
    required this._getTrip,
    this._ticker = periodicTicker,
    DateTime Function()? now,
  }) : _clock = now ?? DateTime.now,
       super(ScheduledDetailState(now: (now ?? DateTime.now)()));

  static const Duration tick = Duration(seconds: 1);
  static const int refreshEvery = 30;

  final String tripId;
  final GetTrip _getTrip;
  final Ticker _ticker;
  final DateTime Function() _clock;

  StreamSubscription<void>? _clockSub;
  int _ticks = 0;

  Future<void> load({bool silent = false}) async {
    _clockSub ??= _ticker(tick).listen((_) {
      if (isClosed) return;
      emit(state.copyWith(now: _clock()));
      if (++_ticks % refreshEvery == 0) load(silent: true);
    });
    if (!silent) emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getTrip(tripId);
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        silent
            ? state
            : state.copyWith(loading: false, failure: failure, now: _clock()),
      ),
      (Trip trip) => emit(
        state.copyWith(
          trip: ScheduledTrip(trip),
          loading: false,
          now: _clock(),
          clearFailure: true,
        ),
      ),
    );
  }

  /// Shows the trip returned by the cancel flow.
  void adopt(Trip trip) => emit(state.copyWith(trip: ScheduledTrip(trip)));

  @override
  Future<void> close() async {
    await _clockSub?.cancel();
    return super.close();
  }
}
