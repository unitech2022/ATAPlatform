import 'package:ata_app/core/utils/latin_digits.dart';
import 'package:intl/intl.dart';

/// Human-friendly date strings with Latin digits.
abstract final class DateText {
  static const String _arabicComma = '، ';
  static const String _latinComma = ', ';

  /// `18 يونيو 2027` / `June 18, 2027`.
  static String longDate(DateTime date, String localeCode) =>
      latinDigits(DateFormat.yMMMMd(localeCode).format(date.toLocal()));

  /// `الأحد، 8:20 م` / `Sun, 8:20 PM`.
  static String dayAndTime(DateTime date, String localeCode) {
    final DateTime local = date.toLocal();
    final String day = DateFormat.E(localeCode).format(local);
    final String time = DateFormat.jm(localeCode).format(local);
    final String comma = localeCode.startsWith('ar')
        ? _arabicComma
        : _latinComma;
    return latinDigits('$day$comma$time');
  }
}
