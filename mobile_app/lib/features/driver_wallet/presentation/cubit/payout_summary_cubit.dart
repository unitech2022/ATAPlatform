import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_payout_summary.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Loads `GET /driver/payouts/summary` (cash-debt card, transfer button).
class PayoutSummaryCubit extends Cubit<PayoutSummaryState> {
  PayoutSummaryCubit({required this._getSummary})
    : super(const PayoutSummaryState());

  final GetPayoutSummary _getSummary;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getSummary(const NoParams());
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (summary) => state.copyWith(loading: false, summary: summary),
      ),
    );
  }
}
