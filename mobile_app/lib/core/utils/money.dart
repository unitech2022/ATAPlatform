import 'package:intl/intl.dart';

/// Formats amounts with Latin digits; the currency suffix is added by l10n.
abstract final class Money {
  static final NumberFormat _compact = NumberFormat('#,##0.##', 'en');
  static final NumberFormat _fixed = NumberFormat('#,##0.00', 'en');

  /// `38` or `54.5`.
  static String compact(num amount) => _compact.format(amount);

  /// `125.00`.
  static String fixed(num amount) => _fixed.format(amount);

  /// Whole number without grouping, e.g. `1840`.
  static String integer(num amount) => amount.round().toString();
}
