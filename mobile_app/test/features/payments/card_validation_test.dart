import 'package:ata_app/features/payments/data/tokenizers/sandbox_card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/entities/card_validation.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  final DateTime now = DateTime(2026, 9, 28);

  test('Luhn accepts valid numbers and rejects typos', () {
    expect(CardValidation.validNumber('4111 1111 1111 1111'), isTrue);
    expect(CardValidation.validNumber('4111 1111 1111 1112'), isFalse);
    expect(CardValidation.validNumber('4111'), isFalse);
  });

  test('expiry must be MM/YY and not in the past', () {
    expect(CardValidation.validExpiry('09/26', now), isTrue);
    expect(CardValidation.validExpiry('08/26', now), isFalse);
    expect(CardValidation.validExpiry('13/30', now), isFalse);
    expect(CardValidation.validExpiry('1/3', now), isFalse);
    expect(CardValidation.parseExpiry('0829'), (8, 2029));
  });

  test('CVC has 3 or 4 digits', () {
    expect(CardValidation.validCvc('123'), isTrue);
    expect(CardValidation.validCvc('1234'), isTrue);
    expect(CardValidation.validCvc('12'), isFalse);
    expect(CardValidation.validCvc('12a'), isFalse);
  });

  test('errors lists the invalid fields; details strips formatting', () {
    expect(
      CardValidation.errors(number: '1', expiry: '', cvc: '1', now: now),
      <CardField>{CardField.number, CardField.expiry, CardField.cvc},
    );
    expect(
      CardValidation.details(
        number: '4111 1111 1111 1111',
        expiry: '12/29',
        cvc: '123',
        holderName: ' SARA ',
        now: now,
      ),
      const CardDetails(
        number: '4111111111111111',
        expiryMonth: 12,
        expiryYear: 2029,
        cvc: '123',
        holderName: 'SARA',
      ),
    );
  });

  group('SandboxCardTokenizer', () {
    const SandboxCardTokenizer tokenizer = SandboxCardTokenizer();

    Future<String> tokenOf(String number) async => (await tokenizer.tokenize(
      CardDetails(number: number, expiryMonth: 1, expiryYear: 2030, cvc: '123'),
    )).getOrElse((_) => throw StateError('no token')).token;

    test('maps test cards to the sandbox tokens', () async {
      expect(await tokenOf('4000000000000002'), 'tok_sandbox_declined');
      expect(await tokenOf('4000000000003220'), 'tok_sandbox_3ds');
      expect(await tokenOf('4406470000000000'), 'tok_sandbox_mada');
      expect(await tokenOf('4111111111111111'), 'tok_sandbox_visa');
    });
  });
}
