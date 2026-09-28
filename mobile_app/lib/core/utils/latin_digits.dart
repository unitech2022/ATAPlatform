/// Replaces Arabic-Indic digits with Latin digits (design rule: numbers are
/// always 0-9).
String latinDigits(String input) {
  const int arabicZero = 0x0660;
  const int arabicNine = 0x0669;
  const int latinZero = 0x30;
  final StringBuffer buffer = StringBuffer();
  for (final int unit in input.codeUnits) {
    if (unit >= arabicZero && unit <= arabicNine) {
      buffer.writeCharCode(latinZero + unit - arabicZero);
    } else {
      buffer.writeCharCode(unit);
    }
  }
  return buffer.toString();
}
