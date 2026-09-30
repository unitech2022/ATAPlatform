import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduled_trips.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_trips_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The rider's open scheduled bookings (`GET /passenger/trips/scheduled`),
/// soonest first, refreshed every minute while the page is open so a
/// reserved / confirmed driver shows up.
class ScheduledTripsCubit extends Cubit<ScheduledTripsState> {
  ScheduledTripsCubit({
    required this._getScheduled,
    this._ticker = periodicTicker,
  }) : super(const ScheduledTripsState());

  static const Duration refreshInterval = Duration(minutes: 1);

  final GetScheduledTrips _getScheduled;
  final Ticker _ticker;

  StreamSubscription<void>? _refresh;

  Future<void> load({bool silent = false}) async {
    _refresh ??= _ticker(refreshInterval).listen((_) => load(silent: true));
    if (!silent) emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getScheduled(const NoParams());
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        silent
            ? state
            : state.copyWith(loading: false, loaded: true, failure: failure),
      ),
      (List<ScheduledTrip> trips) => emit(
        state.copyWith(
          trips: _sorted(trips),
          loading: false,
          loaded: true,
          clearFailure: true,
        ),
      ),
    );
  }

  /// Drops a booking that was cancelled from the detail page.
  void remove(String tripId) => emit(
    state.copyWith(
      trips: state.trips
          .where((ScheduledTrip t) => t.id != tripId)
          .toList(growable: false),
    ),
  );

  static List<ScheduledTrip> _sorted(List<ScheduledTrip> trips) {
    final List<ScheduledTrip> out = List<ScheduledTrip>.of(trips)
      ..sort((ScheduledTrip a, ScheduledTrip b) {
        final DateTime? x = a.scheduledAt;
        final DateTime? y = b.scheduledAt;
        if (x == null || y == null) return x == null ? (y == null ? 0 : 1) : -1;
        return x.compareTo(y);
      });
    return out;
  }

  @override
  Future<void> close() async {
    await _refresh?.cancel();
    return super.close();
  }
}
