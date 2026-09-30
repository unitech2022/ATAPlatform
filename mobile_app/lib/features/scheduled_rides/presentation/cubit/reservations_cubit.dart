import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/confirm_reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_my_reservations.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/release_reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The driver's scheduled reservations with the confirmation countdowns
/// (T-60 first confirmation, T-15 final one), the confirmation itself and
/// the release. The clock ticks every second; the list is reloaded every
/// minute and on every foreground `scheduled.*` push.
class ReservationsCubit extends Cubit<ReservationsState> {
  ReservationsCubit({
    required this._getReservations,
    required this._confirm,
    required this._release,
    this._watchIncoming,
    this._ticker = periodicTicker,
    DateTime Function()? now,
  }) : _clock = now ?? DateTime.now,
       super(ReservationsState(now: (now ?? DateTime.now)()));

  static const Duration tick = Duration(seconds: 1);
  static const int reloadEvery = 60;
  static const String pushPrefix = 'scheduled.';

  final GetMyReservations _getReservations;
  final ConfirmReservation _confirm;
  final ReleaseReservation _release;
  final WatchIncomingNotifications? _watchIncoming;
  final Ticker _ticker;
  final DateTime Function() _clock;

  StreamSubscription<void>? _clockSub;
  StreamSubscription<PushEvent>? _pushes;
  int _ticks = 0;

  /// Loads the active list and starts the clock and push listeners.
  Future<void> load({bool silent = false}) async {
    _clockSub ??= _ticker(tick).listen((_) => _onTick());
    _pushes ??= _watchIncoming?.call().listen((PushEvent event) {
      if (event.eventCode?.startsWith(pushPrefix) ?? false) {
        load(silent: true);
      }
    });
    if (!silent) emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getReservations(
      const ReservationsQuery(list: ReservationList.active),
    );
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        silent
            ? state
            : state.copyWith(loading: false, loaded: true, failure: failure),
      ),
      (PageResult<Reservation> page) => emit(
        state.copyWith(
          active: page.items,
          loading: false,
          loaded: true,
          now: _clock(),
          clearFailure: true,
        ),
      ),
    );
  }

  /// Loads the active list and, when [tripId] is not in it (released,
  /// finished), the past reservations too.
  Future<void> openTrip(String tripId) async {
    await load();
    if (!isClosed && state.find(tripId) == null) await _loadHistory();
  }

  /// Switches between the active and the past reservations.
  Future<void> selectList(ReservationList list) async {
    if (list == state.list) return;
    emit(state.copyWith(list: list));
    if (list == ReservationList.history && state.history.isEmpty) {
      await _loadHistory();
    }
  }

  Future<void> _loadHistory() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getReservations(
      const ReservationsQuery(list: ReservationList.history),
    );
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) => state.copyWith(loading: false, failure: failure),
        (PageResult<Reservation> page) =>
            state.copyWith(loading: false, history: page.items),
      ),
    );
  }

  /// Runs the confirmation that is due for [tripId]. The final one assigns
  /// the driver and returns the trip ([ReservationsState.assignedTrip]).
  Future<void> confirm(String tripId) async {
    final Reservation? reservation = state.find(tripId);
    if (state.isBusy || reservation == null) return;
    final bool isFinal =
        reservation.pendingAt(_clock()) == ConfirmationKind.finalStep;
    emit(
      state.copyWith(busyTripId: tripId, clearAction: true, clearEvent: true),
    );
    final result = await _confirm(tripId);
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        state.copyWith(clearBusy: true, actionFailure: failure, now: _clock()),
      ),
      (ConfirmResult done) => emit(
        state.copyWith(
          clearBusy: true,
          active: _replace(done.reservation),
          event: isFinal
              ? ReservationEvent.finalConfirmed
              : ReservationEvent.confirmed,
          assignedTrip: done.trip,
          now: _clock(),
        ),
      ),
    );
  }

  /// Gives [tripId] back; a late release costs reliability points, which
  /// the API returns in `penaltyPoints`.
  Future<void> release(String tripId, {String? reason}) async {
    if (state.isBusy || state.find(tripId) == null) return;
    emit(
      state.copyWith(busyTripId: tripId, clearAction: true, clearEvent: true),
    );
    final result = await _release(
      ReleaseParams(tripId: tripId, reason: reason),
    );
    if (isClosed) return;
    result.fold(
      (Failure failure) =>
          emit(state.copyWith(clearBusy: true, actionFailure: failure)),
      (Reservation released) => emit(
        state.copyWith(
          clearBusy: true,
          active: state.active
              .where((Reservation r) => r.tripId != tripId)
              .toList(growable: false),
          event: ReservationEvent.released,
          releasedPenaltyPoints: released.penaltyPoints,
        ),
      ),
    );
  }

  /// The page announced the last event / error.
  void clearNotice() =>
      emit(state.copyWith(clearEvent: true, clearAction: true));

  List<Reservation> _replace(Reservation updated) => <Reservation>[
    for (final Reservation r in state.active)
      if (r.tripId == updated.tripId) updated else r,
  ];

  void _onTick() {
    if (isClosed) return;
    emit(state.copyWith(now: _clock()));
    if (++_ticks % reloadEvery == 0) load(silent: true);
  }

  @override
  Future<void> close() async {
    await Future.wait(<Future<void>>[?_clockSub?.cancel(), ?_pushes?.cancel()]);
    return super.close();
  }
}
