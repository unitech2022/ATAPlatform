import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/usecases/get_promotions.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promotions_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/promotions`: available / used / expired tabs, each loaded once.
/// Available promotions whose `validTo` has passed move to "expired".
class PromotionsCubit extends Cubit<PromotionsState> {
  PromotionsCubit({required this._getPromotions, DateTime Function()? now})
    : _now = now ?? DateTime.now,
      super(const PromotionsState());

  final GetPromotions _getPromotions;
  final DateTime Function() _now;

  Future<void> selectTab(PromotionStatus tab) async {
    emit(state.copyWith(tab: tab, clearFailure: true));
    if (!state.isLoaded) await load();
  }

  /// (Re)loads the current tab.
  Future<void> load() async {
    final PromotionStatus tab = state.tab;
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getPromotions(tab);
    if (isClosed) return;
    final DateTime now = _now();
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (List<Promotion> list) => state.copyWith(
          loading: false,
          items: <PromotionStatus, List<Promotion>>{
            ...state.items,
            tab: list
                .where((Promotion p) => p.statusAt(now) == tab)
                .toList(growable: false),
          },
        ),
      ),
    );
  }
}
