import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:ata_app/features/corporate/domain/usecases/check_corporate_eligibility.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Inside the request sheet: the corporate payment's trip purpose and cost
/// center, and whether the company policy allows the current category and
/// time (device pre-check merged with the API's evaluation of the quote).
/// `HomeSync` feeds it the request draft and hands its [booking] to the
/// trip request.
class CorporatePaymentCubit extends Cubit<CorporatePaymentState> {
  CorporatePaymentCubit({required this._checkEligibility})
    : super(const CorporatePaymentState());

  final CheckCorporateEligibility _checkEligibility;

  /// Re-evaluates for [draft]; [selected] says the corporate payment is the
  /// chosen method. A cost center that is no longer offered is dropped.
  void sync(EligibilityDraft draft, {required bool selected}) {
    final String? center = state.costCenterId;
    final bool keepCenter =
        center != null && draft.profile?.costCenterById(center) != null;
    emit(
      state.copyWith(
        draft: draft,
        selected: selected,
        eligibility: _checkEligibility(draft),
        clearCostCenter: !keepCenter,
      ),
    );
  }

  void purposeChanged(String purpose) => emit(state.copyWith(purpose: purpose));

  /// Picks a cost center; `null` (or the chosen one again) clears it.
  void selectCostCenter(String? id) => emit(
    id == null || id == state.costCenterId
        ? state.copyWith(clearCostCenter: true)
        : state.copyWith(costCenterId: id),
  );
}
