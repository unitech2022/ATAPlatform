import 'package:ata_app/features/payments/domain/entities/card_details.dart';

/// Client-side validation of the card form (Luhn, expiry, CVC) run before
/// tokenisation.
abstract final class CardValidation {
  static const int minNumberLength = 13;
  static const int maxNumberLength = 19;
  static const int _centuryBase = 2000;
  static const int _monthsPerYear = 12;

  /// Keeps the digits of [raw].
  static String digitsOf(String raw) => raw.replaceAll(RegExp(r'\D'), '');

  /// Parses `MM/YY` (or `MMYY`) into `(month, fullYear)`.
  static (int, int)? parseExpiry(String raw) {
    final String digits = digitsOf(raw);
    if (digits.length != 4) return null;
    final int? month = int.tryParse(digits.substring(0, 2));
    final int? year = int.tryParse(digits.substring(2));
    if (month == null || year == null) return null;
    if (month < 1 || month > _monthsPerYear) return null;
    return (month, _centuryBase + year);
  }

  static bool luhn(String digits) {
    int sum = 0;
    bool alternate = false;
    for (int i = digits.length - 1; i >= 0; i--) {
      int digit = digits.codeUnitAt(i) - 48;
      if (alternate) {
        digit *= 2;
        if (digit > 9) digit -= 9;
      }
      sum += digit;
      alternate = !alternate;
    }
    return sum % 10 == 0;
  }

  static bool validNumber(String raw) {
    final String digits = digitsOf(raw);
    return digits.length >= minNumberLength &&
        digits.length <= maxNumberLength &&
        luhn(digits);
  }

  /// The card is valid through the end of its expiry month.
  static bool validExpiry(String raw, DateTime now) {
    final (int, int)? expiry = parseExpiry(raw);
    if (expiry == null) return false;
    final (int month, int year) = expiry;
    return year > now.year || (year == now.year && month >= now.month);
  }

  static bool validCvc(String raw) {
    final String digits = digitsOf(raw);
    return digits.length == raw.trim().length &&
        (digits.length == 3 || digits.length == 4);
  }

  /// Invalid fields of the form.
  static Set<CardField> errors({
    required String number,
    required String expiry,
    required String cvc,
    required DateTime now,
  }) => <CardField>{
    if (!validNumber(number)) CardField.number,
    if (!validExpiry(expiry, now)) CardField.expiry,
    if (!validCvc(cvc)) CardField.cvc,
  };

  /// Builds [CardDetails] when every field is valid.
  static CardDetails? details({
    required String number,
    required String expiry,
    required String cvc,
    required String holderName,
    required DateTime now,
  }) {
    if (errors(number: number, expiry: expiry, cvc: cvc, now: now).isNotEmpty) {
      return null;
    }
    final (int month, int year) = parseExpiry(expiry)!;
    return CardDetails(
      number: digitsOf(number),
      expiryMonth: month,
      expiryYear: year,
      cvc: digitsOf(cvc),
      holderName: holderName.trim(),
    );
  }
}
