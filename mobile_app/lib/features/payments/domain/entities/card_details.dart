import 'package:equatable/equatable.dart';

/// Fields of the card form.
enum CardField { number, expiry, cvc }

/// Raw card input. It only ever reaches the [CardTokenizer]; the API
/// receives the resulting token, never these digits.
class CardDetails extends Equatable {
  const CardDetails({
    required this.number,
    required this.expiryMonth,
    required this.expiryYear,
    required this.cvc,
    this.holderName = '',
  });

  /// Digits only.
  final String number;
  final int expiryMonth;

  /// Four digits.
  final int expiryYear;
  final String cvc;
  final String holderName;

  String get last4 =>
      number.length >= 4 ? number.substring(number.length - 4) : number;

  @override
  List<Object?> get props => <Object?>[
    number,
    expiryMonth,
    expiryYear,
    cvc,
    holderName,
  ];
}

/// A card token returned by the tokenizer (`tok_…`).
class CardToken extends Equatable {
  const CardToken({required this.token, required this.brand, this.last4});

  final String token;
  final String brand;
  final String? last4;

  @override
  List<Object?> get props => <Object?>[token, brand, last4];
}
