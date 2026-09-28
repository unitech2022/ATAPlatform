import 'dart:async';
import 'dart:math';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/usecases/mark_passenger_no_show.dart';
import 'package:ata_app/features/trip/presentation/cubit/no_show_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Driver waiting screen: counts down from `arrivedAt` to the no-show
/// policy wait, then enables "passenger didn't show" (F14). A
/// `422 no_show_too_early` resynchronises the countdown with
/// `details.secondsRemaining`.
class NoShowCubit extends Cubit<NoShowState> {
  NoShowCubit({
    required this._markNoShow,
    this.wait = defaultWait,
    this._ticker = periodicTicker,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const NoShowState());

  /// `Cancellation:NoShowWaitMinutes` (the API enforces the real value).
  static const Duration defaultWait = Duration(minutes: 5);
  static const Duration _tick = Duration(seconds: 1);

  final MarkPassengerNoShow _markNoShow;
  final Duration wait;
  final Ticker _ticker;
  final DateTime Function() _now;

  String? _tripId;
  DateTime? _allowedAt;
  StreamSubscription<void>? _clock;

  /// Starts the countdown for [trip] (from `timeline.arrivedAt`, or now).
  void start(Trip trip) {
    if (_tripId == trip.id) return;
    _tripId = trip.id;
    final DateTime arrivedAt = trip.timeline.arrivedAt ?? _now();
    _allowedAt = arrivedAt.add(wait);
    emit(state.copyWith(started: true, secondsRemaining: _remaining()));
    _clock?.cancel();
    _clock = _ticker(_tick).listen((_) {
      if (isClosed) return;
      final int left = _remaining();
      if (left != state.secondsRemaining) {
        emit(state.copyWith(secondsRemaining: left));
      }
    });
  }

  Future<void> markNoShow({GeoPoint? at}) async {
    final String? tripId = _tripId;
    if (tripId == null || !state.canMarkNoShow) return;
    emit(state.copyWith(busy: true, clearFailure: true));
    final result = await _markNoShow(NoShowParams(tripId: tripId, at: at));
    if (isClosed) return;
    result.fold(_onFailure, (Trip trip) {
      _clock?.cancel();
      emit(state.copyWith(busy: false, result: trip));
    });
  }

  void _onFailure(Failure failure) {
    final int? seconds = failure.code == ErrorCodes.noShowTooEarly
        ? failure.intDetail(ErrorCodes.secondsRemaining)
        : null;
    if (seconds != null) {
      _allowedAt = _now().add(Duration(seconds: seconds));
    }
    emit(
      state.copyWith(
        busy: false,
        failure: failure,
        secondsRemaining: seconds ?? state.secondsRemaining,
      ),
    );
  }

  int _remaining() {
    final DateTime? allowedAt = _allowedAt;
    if (allowedAt == null) return 0;
    return max(0, allowedAt.difference(_now()).inSeconds);
  }

  @override
  Future<void> close() async {
    await _clock?.cancel();
    return super.close();
  }
}
