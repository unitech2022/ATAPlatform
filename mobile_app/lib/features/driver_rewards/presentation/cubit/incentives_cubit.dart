import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_incentives.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_state.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/get_reliability.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/incentives` tabs (each loaded once, refreshable) plus the
/// reliability multiplier that reduces rewards; also feeds the "nearest
/// quest" card of the overview.
class IncentivesCubit extends Cubit<IncentivesState> {
  IncentivesCubit({
    required this._getIncentives,
    required this._getReliability,
    IncentiveTab initialTab = IncentiveTab.active,
  }) : super(IncentivesState(tab: initialTab));

  final GetIncentives _getIncentives;
  final GetReliability _getReliability;

  /// Loads the current tab and the multiplier.
  Future<void> load() async {
    await Future.wait(<Future<void>>[_loadTab(), _loadMultiplier()]);
  }

  Future<void> selectTab(IncentiveTab tab) async {
    emit(state.copyWith(tab: tab, clearFailure: true));
    if (!state.isLoaded) await _loadTab();
  }

  Future<void> refresh() => _loadTab();

  Future<void> _loadTab() async {
    final IncentiveTab tab = state.tab;
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getIncentives(tab);
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (List<Incentive> list) => state.copyWith(
          loading: false,
          items: <IncentiveTab, List<Incentive>>{...state.items, tab: list},
        ),
      ),
    );
  }

  Future<void> _loadMultiplier() async {
    final result = await _getReliability(TripActor.driver);
    if (isClosed) return;
    result.fold((_) {}, (ReliabilitySummary summary) {
      final double? multiplier = summary.incentiveMultiplier;
      if (multiplier != null) emit(state.copyWith(multiplier: multiplier));
    });
  }
}
