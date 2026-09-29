import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_driver_tier.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Tier badge / progress (dashboard overview and `/driver/tier`).
class DriverTierCubit extends Cubit<DriverTierState> {
  DriverTierCubit({required this._getTier}) : super(const DriverTierState());

  final GetDriverTier _getTier;

  Future<void> load() async {
    emit(DriverTierState(info: state.info, loading: true));
    final result = await _getTier(const NoParams());
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => DriverTierState(info: state.info, failure: failure),
        (DriverTierInfo info) => DriverTierState(info: info),
      ),
    );
  }
}
