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

  /// `الأحد` / `Sun`.
  static String weekday(DateTime date, String localeCode) =>
      DateFormat.E(localeCode).format(date.toLocal());

  /// `4 أكتوبر` / `Oct 4`.
  static String dayMonth(DateTime date, String localeCode) =>
      latinDigits(DateFormat.MMMd(localeCode).format(date.toLocal()));

  /// `8:20 م` / `8:20 PM`.
  static String time(DateTime date, String localeCode) =>
      latinDigits(DateFormat.jm(localeCode).format(date.toLocal()));

  /// `الأحد 4 أكتوبر، 8:20 م` / `Sun, Oct 4, 8:20 PM`: the full booking
  /// time shown before a scheduled ride is confirmed.
  static String fullDayAndTime(DateTime date, String localeCode) {
    final String comma = localeCode.startsWith('ar')
        ? _arabicComma
        : _latinComma;
    return latinDigits(
      '${weekday(date, localeCode)} ${dayMonth(date, localeCode)}'
      '$comma${time(date, localeCode)}',
    );
  }
}
