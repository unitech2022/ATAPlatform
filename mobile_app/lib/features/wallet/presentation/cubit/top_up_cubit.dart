import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/usecases/get_payment_methods.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_wallet.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_with_method.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fpdart/fpdart.dart';

/// Amount and source selection (saved card or sandbox), confirmation,
/// 3-D Secure hand-off and success for a wallet top-up.
class TopUpCubit extends Cubit<TopUpState> {
  TopUpCubit({
    required this._topUpWallet,
    this._topUpWithMethod,
    this._getPaymentMethods,
    this._kind = WalletKind.passenger,
    double? initialAmount,
  }) : super(TopUpState(amount: initialAmount ?? TopUpState.defaultAmount));

  final TopUpWallet _topUpWallet;
  final TopUpWithMethod? _topUpWithMethod;
  final GetPaymentMethods? _getPaymentMethods;
  final WalletKind _kind;

  /// Loads the usable saved cards and preselects the default one.
  Future<void> loadCards() async {
    final GetPaymentMethods? getCards = _getPaymentMethods;
    if (getCards == null) return;
    final result = await getCards(const NoParams());
    result.fold((_) {}, (List<SavedCard> all) {
      final List<SavedCard> cards = all
          .where((SavedCard c) => c.isUsable)
          .toList(growable: false);
      final SavedCard? preferred = cards
          .where((SavedCard c) => c.isDefault)
          .firstOrNull;
      emit(state.copyWith(cards: cards, cardId: preferred?.id));
    });
  }

  void selectAmount(double amount) {
    if (state.submitting) return;
    emit(state.copyWith(amount: amount, clearFailure: true));
  }

  /// Chooses a saved card, or the sandbox when [cardId] is `null`.
  void selectCard(String? cardId) {
    if (state.submitting) return;
    emit(
      state.copyWith(
        cardId: cardId,
        clearCard: cardId == null,
        clearFailure: true,
      ),
    );
  }

  Future<void> confirm() async {
    if (!state.canConfirm) return;
    emit(state.copyWith(submitting: true, clearFailure: true));
    final Either<Failure, TopUpResult> result = await _submit();
    emit(
      result.fold(
        (failure) => state.copyWith(submitting: false, failure: failure),
        (TopUpResult topUp) => topUp.requiresAction
            ? state.copyWith(
                submitting: false,
                step: TopUpStep.action,
                action: topUp.action,
              )
            : state.copyWith(
                submitting: false,
                step: TopUpStep.success,
                newBalance: topUp.balance,
              ),
      ),
    );
  }

  Future<Either<Failure, TopUpResult>> _submit() {
    final TopUpWithMethod? withMethod = _topUpWithMethod;
    final bool plainSandbox = !state.usesCard && _kind == WalletKind.passenger;
    if (plainSandbox || withMethod == null) return _topUpWallet(state.amount);
    return withMethod(
      TopUpParams(
        amount: state.amount,
        method: state.usesCard ? TopUpMethod.card : TopUpMethod.sandbox,
        paymentMethodId: state.cardId,
        kind: _kind,
      ),
    );
  }
}
