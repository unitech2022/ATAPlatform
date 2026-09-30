/// Flight number rules of `docs/11` §F17.7: optional, stored only.
abstract final class FlightNumber {
  static final RegExp _pattern = RegExp(r'^[A-Z0-9]{2}[0-9]{1,4}[A-Z]?$');

  /// Uppercase without spaces (`sv 1020` -> `SV1020`); `null` when empty.
  static String? normalize(String input) {
    final String cleaned = input.replaceAll(RegExp(r'\s+'), '').toUpperCase();
    return cleaned.isEmpty ? null : cleaned;
  }

  /// `true` for an empty value or a well-formed number.
  static bool isValid(String? normalized) =>
      normalized == null || _pattern.hasMatch(normalized);
}
