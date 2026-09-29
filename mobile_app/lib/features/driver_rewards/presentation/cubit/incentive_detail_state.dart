import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:equatable/equatable.dart';

/// State of [IncentiveDetailCubit].
class IncentiveDetailState extends Equatable {
  const IncentiveDetailState({
    this.incentive,
    this.loading = false,
    this.failure,
    this.joining = false,
    this.actionFailure,
    this.multiplier = 1,
  });

  final Incentive? incentive;
  final bool loading;
  final Failure? failure;
  final bool joining;

  /// `409 incentive_opt_in_closed` and other opt-in errors.
  final Failure? actionFailure;
  final double multiplier;

  bool get isReduced => multiplier < 1;

  IncentiveDetailState copyWith({
    Incentive? incentive,
    bool? loading,
    Failure? failure,
    bool? joining,
    Failure? actionFailure,
    double? multiplier,
    bool clearFailures = false,
  }) => IncentiveDetailState(
    incentive: incentive ?? this.incentive,
    loading: loading ?? this.loading,
    failure: clearFailures ? null : failure ?? this.failure,
    joining: joining ?? this.joining,
    actionFailure: clearFailures ? null : actionFailure ?? this.actionFailure,
    multiplier: multiplier ?? this.multiplier,
  );

  @override
  List<Object?> get props => <Object?>[
    incentive,
    loading,
    failure,
    joining,
    actionFailure,
    multiplier,
  ];
}
