/// Saudi mobile number helpers.
///
/// The API accepts `05XXXXXXXX`, `5XXXXXXXX` or `+9665XXXXXXXX` and
/// normalizes to E.164 `+9665XXXXXXXX`.
abstract final class PhoneNumber {
  static const String countryCode = '+966';
  static const int localLength = 9;
  static const String mobilePrefix = '5';

  static final RegExp _digitsOnly = RegExp(r'\D');

  /// True when [local] is exactly nine digits starting with `5`.
  static bool isValidLocal(String local) =>
      local.length == localLength &&
      local.startsWith(mobilePrefix) &&
      !_digitsOnly.hasMatch(local);

  /// Returns the E.164 form or `null` when the input is not a Saudi mobile.
  static String? normalize(String input) {
    String digits = input.replaceAll(_digitsOnly, '');
    if (digits.startsWith('00966')) {
      digits = digits.substring(5);
    } else if (digits.startsWith('966')) {
      digits = digits.substring(3);
    } else if (digits.startsWith('0')) {
      digits = digits.substring(1);
    }
    return isValidLocal(digits) ? '$countryCode$digits' : null;
  }

  /// `+966 512345678` for display; always rendered LTR by callers.
  static String display(String local) => '$countryCode $local';

  /// Groups digits like the placeholder `5X XXX XXXX`.
  static String grouped(String local) {
    final StringBuffer buffer = StringBuffer();
    for (int i = 0; i < local.length; i++) {
      if (i == 2 || i == 5) buffer.write(' ');
      buffer.write(local[i]);
    }
    return buffer.toString();
  }
}
