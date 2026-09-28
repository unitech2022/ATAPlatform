import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/entities/card_validation.dart';
import 'package:ata_app/features/payments/domain/usecases/add_payment_method.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Card form: client-side validation, then tokenise → save → (3-D Secure).
class AddCardCubit extends Cubit<AddCardState> {
  AddCardCubit({required this._addPaymentMethod, DateTime Function()? now})
    : _now = now ?? DateTime.now,
      super(const AddCardState(errors: _emptyFormErrors));

  /// Every field is invalid while the form is empty.
  static const Set<CardField> _emptyFormErrors = <CardField>{
    CardField.number,
    CardField.expiry,
    CardField.cvc,
  };

  final AddPaymentMethod _addPaymentMethod;
  final DateTime Function() _now;

  void numberChanged(String value) => _edit(state.copyWith(number: value));
  void expiryChanged(String value) => _edit(state.copyWith(expiry: value));
  void cvcChanged(String value) => _edit(state.copyWith(cvc: value));
  void holderChanged(String value) => _edit(state.copyWith(holderName: value));
  void toggleDefault() => _edit(state.copyWith(setDefault: !state.setDefault));

  Future<void> submit() async {
    if (state.submitting) return;
    final CardDetails? card = CardValidation.details(
      number: state.number,
      expiry: state.expiry,
      cvc: state.cvc,
      holderName: state.holderName,
      now: _now(),
    );
    if (card == null) {
      emit(state.copyWith(showErrors: true));
      return;
    }
    emit(
      state.copyWith(
        status: AddCardStatus.submitting,
        showErrors: true,
        clearFailure: true,
      ),
    );
    final result = await _addPaymentMethod(
      AddCardParams(card: card, setDefault: state.setDefault),
    );
    emit(
      result.fold(
        (failure) =>
            state.copyWith(status: AddCardStatus.editing, failure: failure),
        (added) => added.action == null
            ? state.copyWith(status: AddCardStatus.saved)
            : state.copyWith(
                status: AddCardStatus.requiresAction,
                action: added.action,
              ),
      ),
    );
  }

  void _edit(AddCardState next) {
    if (state.submitting) return;
    emit(_validated(next.copyWith(clearFailure: true)));
  }

  AddCardState _validated(AddCardState next) => next.copyWith(
    errors: CardValidation.errors(
      number: next.number,
      expiry: next.expiry,
      cvc: next.cvc,
      now: _now(),
    ),
  );
}
