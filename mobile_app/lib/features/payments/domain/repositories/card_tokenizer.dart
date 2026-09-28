import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:fpdart/fpdart.dart';

/// Turns card details into a gateway token on the device (provider SDK in
/// production, sandbox tokens in development). Card numbers never leave
/// this boundary towards the ATA API.
abstract interface class CardTokenizer {
  Future<Either<Failure, CardToken>> tokenize(CardDetails card);
}
