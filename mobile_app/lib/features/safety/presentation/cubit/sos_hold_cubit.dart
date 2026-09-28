import 'dart:async';

import 'package:ata_app/core/utils/countdown.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Progress of the press-and-hold SOS confirmation.
class SosHoldState extends Equatable {
  const SosHoldState({
    this.progress = 0,
    this.holding = false,
    this.confirmed = false,
  });

  /// 0..1 while the button is held.
  final double progress;
  final bool holding;

  /// The hold lasted the full duration: raise the SOS.
  final bool confirmed;

  @override
  List<Object?> get props => <Object?>[progress, holding, confirmed];
}

/// Press-and-hold confirmation of the SOS button, driven by a ticker (no
/// widget state): holding for [holdDuration] confirms, releasing earlier
/// resets the progress.
class SosHoldCubit extends Cubit<SosHoldState> {
  SosHoldCubit({
    this.holdDuration = defaultHold,
    this.tick = defaultTick,
    this._ticker = periodicTicker,
  }) : super(const SosHoldState());

  /// `SosCubit`: hold 2 s to confirm (`docs/09` Flutter section).
  static const Duration defaultHold = Duration(seconds: 2);
  static const Duration defaultTick = Duration(milliseconds: 50);

  final Duration holdDuration;
  final Duration tick;
  final Ticker _ticker;

  StreamSubscription<void>? _timer;
  int _ticks = 0;

  int get _totalTicks =>
      (holdDuration.inMicroseconds / tick.inMicroseconds).ceil();

  void press() {
    if (state.holding || state.confirmed) return;
    _ticks = 0;
    emit(const SosHoldState(holding: true));
    _timer = _ticker(tick).listen((_) => _advance());
  }

  void release() {
    if (state.confirmed || !state.holding) return;
    _stop();
    emit(const SosHoldState());
  }

  /// Ready for another hold (after the SOS was raised or dismissed).
  void reset() {
    _stop();
    emit(const SosHoldState());
  }

  void _advance() {
    if (isClosed || !state.holding) return;
    _ticks++;
    final double progress = (_ticks / _totalTicks).clamp(0, 1).toDouble();
    if (progress >= 1) {
      _stop();
      emit(const SosHoldState(progress: 1, confirmed: true));
      return;
    }
    emit(SosHoldState(progress: progress, holding: true));
  }

  void _stop() {
    _timer?.cancel();
    _timer = null;
  }

  @override
  Future<void> close() async {
    await _timer?.cancel();
    return super.close();
  }
}
