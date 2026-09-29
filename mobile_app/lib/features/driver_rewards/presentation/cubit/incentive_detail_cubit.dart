import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/opt_in_incentive.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentive_detail_state.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/get_reliability.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fpdart/fpdart.dart';

/// `/driver/incentives/:id`: the quest, its zones and window, opt-in.
class IncentiveDetailCubit extends Cubit<IncentiveDetailState> {
  IncentiveDetailCubit({
    required this.incentiveId,
    required this._getIncentive,
    required this._optIn,
    required this._getReliability,
  }) : super(const IncentiveDetailState());

  final String incentiveId;
  final GetIncentive _getIncentive;
  final OptInIncentive _optIn;
  final GetReliability _getReliability;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailures: true));
    final Future<Either<Failure, ReliabilitySummary>> reliability =
        _getReliability(TripActor.driver);
    final Either<Failure, Incentive> result = await _getIncentive(incentiveId);
    final double? multiplier = (await reliability).fold(
      (_) => null,
      (ReliabilitySummary s) => s.incentiveMultiplier,
    );
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) => state.copyWith(
          loading: false,
          failure: failure,
          multiplier: multiplier,
        ),
        (Incentive i) => state.copyWith(
          loading: false,
          incentive: i,
          multiplier: multiplier,
        ),
      ),
    );
  }

  Future<void> optIn() async {
    final Incentive? incentive = state.incentive;
    if (incentive == null || !incentive.needsOptIn || state.joining) return;
    emit(state.copyWith(joining: true, clearFailures: true));
    final result = await _optIn(incentive.id);
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(joining: false, actionFailure: failure),
        (_) => state.copyWith(joining: false, incentive: incentive.joined()),
      ),
    );
  }
}
