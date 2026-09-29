import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:equatable/equatable.dart';

/// State of [PromotionsCubit].
class PromotionsState extends Equatable {
  const PromotionsState({
    this.tab = PromotionStatus.available,
    this.items = const <PromotionStatus, List<Promotion>>{},
    this.loading = false,
    this.failure,
  });

  final PromotionStatus tab;

  /// Loaded promotions per tab.
  final Map<PromotionStatus, List<Promotion>> items;
  final bool loading;
  final Failure? failure;

  List<Promotion> get current => items[tab] ?? const <Promotion>[];
  bool get isLoaded => items.containsKey(tab);
  bool get isEmpty => isLoaded && !loading && current.isEmpty;

  PromotionsState copyWith({
    PromotionStatus? tab,
    Map<PromotionStatus, List<Promotion>>? items,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => PromotionsState(
    tab: tab ?? this.tab,
    items: items ?? this.items,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[tab, items, loading, failure];
}
