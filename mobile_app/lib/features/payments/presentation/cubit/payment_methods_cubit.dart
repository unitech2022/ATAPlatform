import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/usecases/get_payment_methods.dart';
import 'package:ata_app/features/payments/domain/usecases/remove_payment_method.dart';
import 'package:ata_app/features/payments/domain/usecases/set_default_payment_method.dart';
import 'package:ata_app/features/payments/presentation/cubit/payment_methods_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Lists the saved cards, sets the default one and deletes cards.
class PaymentMethodsCubit extends Cubit<PaymentMethodsState> {
  PaymentMethodsCubit({
    required this._getPaymentMethods,
    required this._setDefault,
    required this._remove,
  }) : super(const PaymentMethodsState());

  final GetPaymentMethods _getPaymentMethods;
  final SetDefaultPaymentMethod _setDefault;
  final RemovePaymentMethod _remove;

  Future<void> load() async {
    emit(
      state.copyWith(
        loading: true,
        clearFailure: true,
        clearActionFailure: true,
      ),
    );
    final result = await _getPaymentMethods(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (List<SavedCard> cards) => state.copyWith(loading: false, cards: cards),
      ),
    );
  }

  Future<void> setDefault(String id) async {
    if (state.busyId != null) return;
    emit(state.copyWith(busyId: id, clearActionFailure: true));
    final result = await _setDefault(id);
    emit(
      result.fold(
        (failure) => state.copyWith(clearBusy: true, actionFailure: failure),
        (SavedCard updated) => state.copyWith(
          clearBusy: true,
          cards: <SavedCard>[
            for (final SavedCard card in state.cards)
              card.copyWith(isDefault: card.id == updated.id),
          ],
        ),
      ),
    );
  }

  Future<void> remove(String id) async {
    if (state.busyId != null) return;
    emit(state.copyWith(busyId: id, clearActionFailure: true));
    final result = await _remove(id);
    emit(
      result.fold(
        (failure) => state.copyWith(clearBusy: true, actionFailure: failure),
        (_) => state.copyWith(
          clearBusy: true,
          cards: state.cards
              .where((SavedCard card) => card.id != id)
              .toList(growable: false),
        ),
      ),
    );
  }
}
