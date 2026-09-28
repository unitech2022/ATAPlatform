import 'package:ata_app/features/wallet/domain/usecases/top_up_wallet.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Amount selection, confirmation and success for a wallet top-up.
class TopUpCubit extends Cubit<TopUpState> {
  TopUpCubit({required this._topUpWallet}) : super(const TopUpState());

  final TopUpWallet _topUpWallet;

  void selectAmount(double amount) {
    if (state.submitting) return;
    emit(state.copyWith(amount: amount, clearFailure: true));
  }

  Future<void> confirm() async {
    if (!state.canConfirm) return;
    emit(state.copyWith(submitting: true, clearFailure: true));
    final result = await _topUpWallet(state.amount);
    emit(
      result.fold(
        (failure) => state.copyWith(submitting: false, failure: failure),
        (topUp) => state.copyWith(
          submitting: false,
          step: TopUpStep.success,
          newBalance: topUp.balance,
        ),
      ),
    );
  }
}
