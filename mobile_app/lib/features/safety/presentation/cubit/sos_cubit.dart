import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/usecases/cancel_sos.dart';
import 'package:ata_app/features/safety/domain/usecases/get_safety_case.dart';
import 'package:ata_app/features/safety/domain/usecases/send_sos_location.dart';
import 'package:ata_app/features/safety/domain/usecases/trigger_sos.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_state.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/usecases/request_location_access.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_device_position.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// App-wide SOS (F12.3): reads the position, raises the case with the trip
/// id, then streams the position every [locationInterval] until the case
/// is cancelled, and refreshes the case status every [statusEvery] ticks.
class SosCubit extends Cubit<SosState> {
  SosCubit({
    required this._triggerSos,
    required this._sendLocation,
    required this._cancelSos,
    required this._getCase,
    required this._requestAccess,
    required this._watchPosition,
    this._ticker = periodicTicker,
    this.locationInterval = const Duration(seconds: 10),
    this.positionTimeout = const Duration(seconds: 5),
    this.statusEvery = 3,
  }) : super(const SosState());

  final TriggerSos _triggerSos;
  final SendSosLocation _sendLocation;
  final CancelSos _cancelSos;
  final GetSafetyCase _getCase;
  final RequestLocationAccess _requestAccess;
  final WatchDevicePosition _watchPosition;
  final Ticker _ticker;

  /// `Safety:SosLocationIntervalSeconds`.
  final Duration locationInterval;
  final Duration positionTimeout;
  final int statusEvery;

  StreamSubscription<DriverPosition>? _positions;
  StreamSubscription<void>? _timer;
  DriverPosition? _latest;
  int _ticks = 0;

  /// Raises the SOS. [fallback] (trip pickup, last known position) is used
  /// when the device cannot be located in time.
  Future<void> trigger({
    String? tripId,
    String? role,
    GeoPoint? fallback,
  }) async {
    if (state.isActive || state.isBusy) return;
    emit(const SosState(status: SosStatus.sending));
    await _locate();
    if (isClosed) return;
    final DriverPosition? position = _latest;
    final result = await _triggerSos(
      SosRequest(
        point: position?.point ?? fallback ?? GeoPoint.riyadh,
        accuracy: position?.accuracy,
        tripId: tripId,
        role: role,
      ),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) {
        _stopStreaming();
        emit(SosState(failure: f));
      },
      (SosResult sos) {
        emit(SosState(status: SosStatus.active, result: sos));
        _startStreaming(sos.caseId);
      },
    );
  }

  /// "Pressed by mistake" (or resolved): the case stays with operations.
  Future<void> cancel({
    SosCancelReason reason = SosCancelReason.accidental,
  }) async {
    final SosResult? sos = state.result;
    if (sos == null || !state.isActive) return;
    emit(state.copyWith(status: SosStatus.cancelling, clearFailure: true));
    final result = await _cancelSos(
      CancelSosParams(caseId: sos.caseId, reason: reason),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(status: SosStatus.active, failure: f)),
      (SafetyCaseSummary summary) {
        _stopStreaming();
        emit(
          state.copyWith(
            status: SosStatus.cancelled,
            result: sos.withStatus(summary.status),
          ),
        );
      },
    );
  }

  /// Hides the panel once the SOS was cancelled or failed.
  void dismiss() {
    if (state.isActive || state.isBusy) return;
    emit(const SosState());
  }

  Future<void> _locate() async {
    final access = await _requestAccess(const NoParams());
    final bool granted = access.fold(
      (_) => false,
      (LocationAccess a) => a == LocationAccess.granted,
    );
    if (!granted) return;
    final Completer<void> first = Completer<void>();
    _positions ??= _watchPosition().listen((DriverPosition p) {
      _latest = p;
      if (!first.isCompleted) first.complete();
    }, onError: (Object _) {});
    if (_latest != null) return;
    await first.future.timeout(positionTimeout, onTimeout: () {});
  }

  void _startStreaming(String caseId) {
    _ticks = 0;
    _timer?.cancel();
    _timer = _ticker(locationInterval).listen((_) async {
      _ticks++;
      final DriverPosition? position = _latest;
      if (position != null) {
        final sent = await _sendLocation(
          SosLocationParams(
            caseId: caseId,
            point: position.point,
            accuracy: position.accuracy,
          ),
        );
        if (!isClosed && sent.isRight() && state.isActive) {
          emit(state.copyWith(locationsSent: state.locationsSent + 1));
        }
      }
      if (_ticks % statusEvery == 0) await _refreshStatus(caseId);
    });
  }

  Future<void> _refreshStatus(String caseId) async {
    final result = await _getCase(caseId);
    if (isClosed || !state.isActive) return;
    result.fold((_) {}, (SafetyCaseSummary summary) {
      emit(state.copyWith(result: state.result?.withStatus(summary.status)));
      if (summary.isResolved) {
        _stopStreaming();
        emit(state.copyWith(status: SosStatus.cancelled));
      }
    });
  }

  void _stopStreaming() {
    _timer?.cancel();
    _timer = null;
    _positions?.cancel();
    _positions = null;
  }

  @override
  Future<void> close() async {
    _stopStreaming();
    return super.close();
  }
}
