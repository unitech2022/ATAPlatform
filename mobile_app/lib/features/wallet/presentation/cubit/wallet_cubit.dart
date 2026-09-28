import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/wallet/domain/usecases/get_wallet.dart';
import 'package:ata_app/features/wallet/presentation/cubit/wallet_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads the balance and payment methods.
class WalletCubit extends Cubit<WalletState> {
  WalletCubit({required this._getWallet}) : super(const WalletState());

  final GetWallet _getWallet;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getWallet(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (wallet) => state.copyWith(loading: false, wallet: wallet),
      ),
    );
  }

  /// Applies a balance returned by a successful top-up.
  void balanceChanged(double balance) {
    final current = state.wallet;
    if (current != null) {
      emit(state.copyWith(wallet: current.copyWith(balance: balance)));
    }
  }
}
