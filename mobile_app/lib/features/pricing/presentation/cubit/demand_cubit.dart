import 'dart:async';

import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/usecases/get_demand_at_location.dart';
import 'package:ata_app/features/pricing/presentation/cubit/demand_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Demand level at the pickup (`GET /pricing/demand`): refreshed when the
/// pickup moves and every [interval] while the sheet is open.
class DemandCubit extends Cubit<DemandState> {
  DemandCubit({
    required this._getDemand,
    this.interval = defaultInterval,
    this._ticker = periodicTicker,
  }) : super(const DemandState());

  static const Duration defaultInterval = Duration(seconds: 60);

  final GetDemandAtLocation _getDemand;
  final Duration interval;
  final Ticker _ticker;

  StreamSubscription<void>? _tick;

  /// Starts watching [pickup]; a new point refreshes immediately.
  Future<void> watch(GeoPoint pickup) {
    _tick ??= _ticker(interval).listen((_) => _refresh());
    if (pickup == state.location && state.status != DemandStatus.idle) {
      return Future<void>.value();
    }
    emit(state.copyWith(location: pickup));
    return _refresh();
  }

  Future<void> refresh() => _refresh();

  Future<void> _refresh() async {
    final GeoPoint? location = state.location;
    if (location == null) return;
    emit(state.copyWith(status: DemandStatus.loading));
    final result = await _getDemand(location);
    if (isClosed || location != state.location) return;
    emit(
      result.fold(
        (failure) =>
            state.copyWith(status: DemandStatus.failure, failure: failure),
        (DemandLevel level) => state.copyWith(
          status: DemandStatus.ready,
          level: level,
          clearFailure: true,
        ),
      ),
    );
  }

  @override
  Future<void> close() async {
    await _tick?.cancel();
    return super.close();
  }
}
