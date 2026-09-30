import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/usecases/get_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/join_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/leave_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/watch_airport_queue.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The driver's airport queue (`docs/11` §F17.8): position in the queue,
/// updated by the hub's `AirportQueueUpdated` and by polling every 30 s,
/// with manual join / leave.
class AirportQueueCubit extends Cubit<AirportQueueState> {
  AirportQueueCubit({
    required this._getQueue,
    required this._join,
    required this._leave,
    required this._watch,
    this._ticker = periodicTicker,
  }) : super(const AirportQueueState());

  static const Duration pollInterval = Duration(seconds: 30);

  final GetAirportQueue _getQueue;
  final JoinAirportQueue _join;
  final LeaveAirportQueue _leave;
  final WatchAirportQueue _watch;
  final Ticker _ticker;

  StreamSubscription<AirportQueuePosition>? _updates;
  StreamSubscription<void>? _poll;

  /// Loads the status and keeps it fresh. Idempotent.
  Future<void> start() async {
    _updates ??= _watch().listen(_onUpdate);
    _poll ??= _ticker(pollInterval).listen((_) => refresh(silent: true));
    await refresh();
  }

  Future<void> refresh({bool silent = false}) async {
    if (!silent) emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getQueue(const NoParams());
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        silent ? state : state.copyWith(loading: false, failure: failure),
      ),
      (AirportQueueStatus queue) => emit(
        state.copyWith(loading: false, queue: queue, clearFailure: true),
      ),
    );
  }

  /// Joins the queue from [position] (`422 not_in_airport_waiting_area`).
  Future<void> join(GeoPoint position) async {
    if (state.busy || state.inQueue) return;
    emit(state.copyWith(busy: true, clearActionFailure: true));
    final result = await _join(position);
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) =>
            state.copyWith(busy: false, actionFailure: failure),
        (AirportQueueStatus queue) => state.copyWith(busy: false, queue: queue),
      ),
    );
  }

  Future<void> leave() async {
    final AirportQueueStatus? current = state.queue;
    if (state.busy || !state.inQueue || current == null) return;
    emit(state.copyWith(busy: true, clearActionFailure: true));
    final result = await _leave(const NoParams());
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) =>
            state.copyWith(busy: false, actionFailure: failure),
        (_) => state.copyWith(
          busy: false,
          queue: AirportQueueStatus(eligibleAirport: current.airport),
        ),
      ),
    );
  }

  void _onUpdate(AirportQueuePosition update) {
    final AirportQueueStatus? current = state.queue;
    if (isClosed) return;
    if (current == null || !current.inQueue) {
      // The API queued the driver on its own (entered the waiting area).
      unawaited(refresh(silent: true));
      return;
    }
    emit(state.copyWith(queue: current.withPosition(update)));
  }

  @override
  Future<void> close() async {
    // Both at once: the poll timer must stop even if the hub stream is slow
    // to acknowledge its cancellation.
    await Future.wait(<Future<void>>[?_poll?.cancel(), ?_updates?.cancel()]);
    return super.close();
  }
}
