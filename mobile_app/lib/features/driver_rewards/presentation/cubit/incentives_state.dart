import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:equatable/equatable.dart';

/// State of [IncentivesCubit].
class IncentivesState extends Equatable {
  const IncentivesState({
    this.tab = IncentiveTab.active,
    this.items = const <IncentiveTab, List<Incentive>>{},
    this.loading = false,
    this.failure,
    this.multiplier = 1,
  });

  final IncentiveTab tab;
  final Map<IncentiveTab, List<Incentive>> items;
  final bool loading;
  final Failure? failure;

  /// Reliability `effects.incentiveMultiplier` (`docs/09`); < 1 reduces
  /// every reward.
  final double multiplier;

  List<Incentive> get current => items[tab] ?? const <Incentive>[];
  bool get isLoaded => items.containsKey(tab);
  bool get isEmpty => isLoaded && !loading && current.isEmpty;
  bool get isReduced => multiplier < 1;

  /// Closest active quest to its target (not achieved yet).
  Incentive? get nearest {
    Incentive? best;
    for (final Incentive i in items[IncentiveTab.active] ?? <Incentive>[]) {
      if (i.isAchieved) continue;
      if (best == null || i.progressValue > best.progressValue) best = i;
    }
    return best;
  }

  IncentivesState copyWith({
    IncentiveTab? tab,
    Map<IncentiveTab, List<Incentive>>? items,
    bool? loading,
    Failure? failure,
    double? multiplier,
    bool clearFailure = false,
  }) => IncentivesState(
    tab: tab ?? this.tab,
    items: items ?? this.items,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    multiplier: multiplier ?? this.multiplier,
  );

  @override
  List<Object?> get props => <Object?>[
    tab,
    items,
    loading,
    failure,
    multiplier,
  ];
}
