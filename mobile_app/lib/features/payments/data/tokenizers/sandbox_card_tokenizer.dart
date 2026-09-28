import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/repositories/card_tokenizer.dart';
import 'package:fpdart/fpdart.dart';

/// Development tokenizer: maps test card numbers to the sandbox tokens of
/// `GET /payments/config` without any network call.
///
/// * `4000 0000 0000 0002` → `tok_sandbox_declined`
/// * `4000 0000 0000 3220` → `tok_sandbox_3ds` (3-D Secure challenge)
/// * a mada BIN (e.g. `4406 4700 0000 0007`) → `tok_sandbox_mada`
/// * anything else → `tok_sandbox_visa`
class SandboxCardTokenizer implements CardTokenizer {
  const SandboxCardTokenizer();

  static const String declinedNumber = '4000000000000002';
  static const String threeDsNumber = '4000000000003220';

  static const String madaToken = 'tok_sandbox_mada';
  static const String visaToken = 'tok_sandbox_visa';
  static const String threeDsToken = 'tok_sandbox_3ds';
  static const String declinedToken = 'tok_sandbox_declined';

  static const List<String> _madaBins = <String>[
    '440647',
    '440795',
    '446404',
    '457865',
    '968208',
    '588845',
    '636120',
    '417633',
    '468540',
    '504300',
    '543357',
    '588848',
  ];

  @override
  Future<Either<Failure, CardToken>> tokenize(CardDetails card) async {
    final String number = card.number;
    final bool mada = _madaBins.any(number.startsWith);
    final String token = switch (number) {
      declinedNumber => declinedToken,
      threeDsNumber => threeDsToken,
      _ when mada => madaToken,
      _ => visaToken,
    };
    return Right<Failure, CardToken>(
      CardToken(token: token, brand: mada ? 'mada' : 'visa', last4: card.last4),
    );
  }
}
