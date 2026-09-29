import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/usecases/validate_promo_code.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The home sheet's promo code: input, validation against the current quote,
/// applied code (sent to `/pricing/quote` and `POST /passenger/trips`),
/// localized errors and removal.
class PromoCodeCubit extends Cubit<PromoCodeState> {
  PromoCodeCubit({required this._validate, String? initialCode})
    : super(
        PromoCodeState(input: ValidatePromoCode.normalize(initialCode ?? '')),
      );

  final ValidatePromoCode _validate;

  static const String notEligible = 'promo_not_eligible';
  static const String reasonKey = 'reason';

  void inputChanged(String input) => emit(
    state.copyWith(
      input: input,
      status: state.applied == null
          ? PromoCodeStatus.empty
          : PromoCodeStatus.applied,
      clearFailure: true,
    ),
  );

  /// Replaces the input (code picked on the promotions page).
  void prefill(String code) => emit(
    state.copyWith(
      input: ValidatePromoCode.normalize(code),
      inputVersion: state.inputVersion + 1,
      clearFailure: true,
    ),
  );

  /// Validates [PromoCodeState.input] for the trip draft in [context]
  /// (`quoteId`, category, payment method, booking type).
  Future<void> validate(PromoValidationParams context) async {
    if (state.isValidating) return;
    emit(
      state.copyWith(status: PromoCodeStatus.validating, clearFailure: true),
    );
    final result = await _validate(context.withCode(state.input));
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) =>
            state.copyWith(status: PromoCodeStatus.error, failure: failure),
        (PromoValidation valid) => state.copyWith(
          status: PromoCodeStatus.applied,
          applied: valid,
          input: valid.code,
          clearFailure: true,
        ),
      ),
    );
  }

  /// Drops the applied code.
  void remove() => emit(PromoCodeState(inputVersion: state.inputVersion + 1));

  /// The trip request (or a later check) refused the applied code.
  void rejected(Failure failure) => emit(
    state.copyWith(
      status: PromoCodeStatus.error,
      failure: failure,
      clearApplied: true,
    ),
  );

  /// `/pricing/quote` answered `promotion: { valid: false, reason }`.
  void rejectedByQuote(String? reason) {
    if (state.applied == null) return;
    final String code = reason != null && reason.startsWith('promo_')
        ? reason
        : notEligible;
    rejected(
      ServerFailure(
        code: code,
        message: '',
        details: reason == null ? null : <String, dynamic>{reasonKey: reason},
      ),
    );
  }
}
