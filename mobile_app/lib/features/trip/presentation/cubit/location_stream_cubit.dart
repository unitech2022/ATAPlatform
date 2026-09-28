import 'dart:async';

import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/usecases/request_location_access.dart';
import 'package:ata_app/features/trip/domain/usecases/send_driver_location.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_device_position.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Streams the device position while the driver is online or on a trip and
/// sends it to `PUT /driver/location` every few seconds.
class LocationStreamCubit extends Cubit<LocationStreamState> {
  LocationStreamCubit({
    required this._requestAccess,
    required this._watchPosition,
    required this._sendLocation,
    this._ticker = periodicTicker,
    this.sendInterval = const Duration(seconds: 4),
    this.minSendGap = const Duration(seconds: 3),
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const LocationStreamState());

  final RequestLocationAccess _requestAccess;
  final WatchDevicePosition _watchPosition;
  final SendDriverLocation _sendLocation;
  final Ticker _ticker;
  final DateTime Function() _now;

  /// Heartbeat: the latest position is re-sent at this interval.
  final Duration sendInterval;

  /// Minimum gap between two sends triggered by movement.
  final Duration minSendGap;

  StreamSubscription<DriverPosition>? _positions;
  StreamSubscription<void>? _heartbeat;
  bool _sending = false;

  /// Requests permission, then streams. Idempotent while active.
  Future<void> start() async {
    if (state.status == LocationStreamStatus.streaming ||
        state.status == LocationStreamStatus.requesting) {
      return;
    }
    emit(state.copyWith(status: LocationStreamStatus.requesting));
    final result = await _requestAccess(const NoParams());
    if (isClosed) return;
    result.fold(
      (failure) => emit(
        state.copyWith(status: LocationStreamStatus.failure, failure: failure),
      ),
      (LocationAccess access) => switch (access) {
        LocationAccess.granted => _listen(),
        LocationAccess.denied => emit(
          state.copyWith(status: LocationStreamStatus.denied),
        ),
        LocationAccess.deniedForever => emit(
          state.copyWith(status: LocationStreamStatus.deniedForever),
        ),
        LocationAccess.serviceDisabled => emit(
          state.copyWith(status: LocationStreamStatus.serviceDisabled),
        ),
      },
    );
  }

  Future<void> stop() async {
    await _positions?.cancel();
    await _heartbeat?.cancel();
    _positions = null;
    _heartbeat = null;
    if (!isClosed) emit(const LocationStreamState());
  }

  void _listen() {
    emit(state.copyWith(status: LocationStreamStatus.streaming));
    _positions = _watchPosition().listen(
      _onPosition,
      onError: (Object error) {
        if (!isClosed) {
          emit(state.copyWith(status: LocationStreamStatus.failure));
        }
      },
    );
    _heartbeat = _ticker(sendInterval).listen((_) {
      final DriverPosition? position = state.position;
      if (position != null) _send(position);
    });
  }

  void _onPosition(DriverPosition position) {
    if (isClosed) return;
    emit(state.copyWith(position: position));
    final DateTime? last = state.lastSentAt;
    if (last == null || _now().difference(last) >= minSendGap) {
      _send(position);
    }
  }

  Future<void> _send(DriverPosition position) async {
    if (_sending) return;
    _sending = true;
    final result = await _sendLocation(position);
    _sending = false;
    if (isClosed) return;
    result.fold(
      (failure) => emit(state.copyWith(failure: failure)),
      (_) => emit(state.copyWith(lastSentAt: _now(), clearFailure: true)),
    );
  }

  @override
  Future<void> close() async {
    await _positions?.cancel();
    await _heartbeat?.cancel();
    return super.close();
  }
}
