import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/set_driver_online.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Reads and toggles the driver's availability.
class OnlineStatusCubit extends Cubit<OnlineStatusState> {
  OnlineStatusCubit({required this._getStatus, required this._setOnline})
    : super(const OnlineStatusState());

  final GetDriverStatus _getStatus;
  final SetDriverOnline _setOnline;

  Future<void> load() async {
    final result = await _getStatus(const NoParams());
    result.fold((_) {}, _apply);
  }

  Future<void> toggle() async {
    if (state.updating) return;
    final bool target = !state.isOnline;
    emit(state.copyWith(updating: true, isOnline: target, clearFailure: true));
    final result = await _setOnline(target);
    result.fold(
      (failure) => emit(
        state.copyWith(updating: false, isOnline: !target, failure: failure),
      ),
      _apply,
    );
  }

  void _apply(DriverStatus status) => emit(
    state.copyWith(
      isOnline: status.isOnline,
      canGoOnline: status.canGoOnline,
      updating: false,
    ),
  );
}
