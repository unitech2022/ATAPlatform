import 'dart:async';
import 'dart:math';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/get_pending_safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/respond_to_safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/watch_safety_checks.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// App-wide "هل أنت بخير؟" (F12.5): prompts on the hub `SafetyCheck`
/// event, foreground `safety.check` pushes and `ata://safety/check/{id}`
/// links (including the notification's `ok` / `help` buttons), counts down
/// to `respondBy` and sends the answer.
class SafetyCheckCubit extends Cubit<SafetyCheckState> {
  SafetyCheckCubit({
    required this._watchChecks,
    required this._getPending,
    required this._respond,
    this._ticker = periodicTicker,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const SafetyCheckState());

  final WatchSafetyChecks _watchChecks;
  final GetPendingSafetyAlert _getPending;
  final RespondToSafetyAlert _respond;
  final Ticker _ticker;
  final DateTime Function() _now;

  static const Duration _tick = Duration(seconds: 1);

  StreamSubscription<SafetyAlert>? _checks;
  StreamSubscription<void>? _clock;

  /// Listens for prompts and restores a pending one. Idempotent.
  Future<void> start() async {
    if (_checks != null) return;
    _checks = _watchChecks().listen(_prompt);
    final result = await _getPending(const NoParams());
    if (isClosed) return;
    result.fold((_) {}, (SafetyAlert? alert) {
      if (alert != null && alert.isPending) _prompt(alert);
    });
  }

  Future<void> stop() async {
    await _checks?.cancel();
    await _clock?.cancel();
    _checks = null;
    _clock = null;
    if (!isClosed) emit(const SafetyCheckState());
  }

  /// Opened from a push / deep link; [actionId] is the notification button
  /// (`ok` / `help`) that answers immediately.
  Future<void> open(String alertId, {String? actionId}) async {
    SafetyAlert alert = state.alert?.id == alertId
        ? state.alert!
        : SafetyAlert(id: alertId);
    if (state.alert?.id != alertId) {
      final result = await _getPending(const NoParams());
      if (isClosed) return;
      final SafetyAlert? pending = result.getOrElse((_) => null);
      if (pending != null && pending.id == alertId) alert = pending;
    }
    _prompt(alert);
    final SafetyCheckResponse? response = SafetyCheckResponse.fromAction(
      actionId,
    );
    if (response != null) await respond(response);
  }

  Future<void> respond(SafetyCheckResponse response, {GeoPoint? at}) async {
    final SafetyAlert? alert = state.alert;
    if (alert == null || !state.canRespond) return;
    emit(
      state.copyWith(
        status: SafetyCheckStatus.responding,
        response: response,
        clearFailure: true,
      ),
    );
    final result = await _respond(
      RespondToAlertParams(alertId: alert.id, response: response, at: at),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(
        state.copyWith(
          status: f.code == ErrorCodes.conflict
              ? SafetyCheckStatus.expired
              : SafetyCheckStatus.prompting,
          failure: f,
        ),
      ),
      (SafetyAlert updated) {
        _clock?.cancel();
        emit(
          state.copyWith(status: SafetyCheckStatus.answered, alert: updated),
        );
      },
    );
  }

  /// Closes the prompt (after an answer or when it expired).
  void dismiss() {
    _clock?.cancel();
    emit(const SafetyCheckState());
  }

  void _prompt(SafetyAlert alert) {
    if (isClosed) return;
    if (state.alert?.id == alert.id && state.status != SafetyCheckStatus.idle) {
      return;
    }
    emit(
      SafetyCheckState(
        status: SafetyCheckStatus.prompting,
        alert: alert,
        secondsLeft: _secondsLeft(alert),
      ),
    );
    _clock?.cancel();
    if (alert.respondBy == null) return;
    _clock = _ticker(_tick).listen((_) {
      if (isClosed || state.status != SafetyCheckStatus.prompting) return;
      final int left = _secondsLeft(alert) ?? 0;
      emit(
        state.copyWith(
          secondsLeft: left,
          status: left <= 0 ? SafetyCheckStatus.expired : null,
        ),
      );
      if (left <= 0) _clock?.cancel();
    });
  }

  int? _secondsLeft(SafetyAlert alert) {
    final DateTime? by = alert.respondBy;
    return by == null ? null : max(0, by.difference(_now()).inSeconds);
  }

  @override
  Future<void> close() async {
    await _checks?.cancel();
    await _clock?.cancel();
    return super.close();
  }
}
