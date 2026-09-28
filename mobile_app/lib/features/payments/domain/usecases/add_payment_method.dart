import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/repositories/card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Input of [AddPaymentMethod].
class AddCardParams {
  const AddCardParams({required this.card, this.setDefault = true});

  final CardDetails card;
  final bool setDefault;
}

/// Tokenises the card on the device, then saves the token
/// (`POST /passenger/payment-methods`). The card number is never sent to
/// the ATA API.
class AddPaymentMethod implements UseCase<AddCardResult, AddCardParams> {
  const AddPaymentMethod(this._tokenizer, this._repository);

  final CardTokenizer _tokenizer;
  final PaymentsRepository _repository;

  @override
  Future<Either<Failure, AddCardResult>> call(AddCardParams params) async {
    final Either<Failure, CardToken> token = await _tokenizer.tokenize(
      params.card,
    );
    return token.fold(
      (Failure failure) async => Left<Failure, AddCardResult>(failure),
      (CardToken token) => _repository.addPaymentMethod(
        token: token.token,
        setDefault: params.setDefault,
      ),
    );
  }
}
