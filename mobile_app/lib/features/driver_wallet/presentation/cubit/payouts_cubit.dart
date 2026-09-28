import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/cancel_payout.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_payouts.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payouts_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Payout history with cancel for `requested` payouts.
class PayoutsCubit extends Cubit<PayoutsState> {
  PayoutsCubit({required this._getPayouts, required this._cancelPayout})
    : super(const PayoutsState());

  final GetPayouts _getPayouts;
  final CancelPayout _cancelPayout;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getPayouts(1);
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (PageResult<Payout> page) =>
            state.copyWith(loading: false, payouts: page.items),
      ),
    );
  }

  Future<void> cancel(String id) async {
    if (state.cancellingId != null) return;
    emit(state.copyWith(cancellingId: id, clearFailure: true));
    final result = await _cancelPayout(id);
    emit(
      result.fold(
        (failure) =>
            state.copyWith(clearCancelling: true, actionFailure: failure),
        (Payout updated) => state.copyWith(
          clearCancelling: true,
          payouts: <Payout>[
            for (final Payout p in state.payouts) p.id == id ? updated : p,
          ],
        ),
      ),
    );
  }
}
