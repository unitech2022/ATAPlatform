import 'package:equatable/equatable.dart';

/// Inclusive local date range (`from` / `to` of the statement endpoint).
class DateRange extends Equatable {
  const DateRange({required this.from, required this.to});

  final DateTime from;
  final DateTime to;

  @override
  List<Object?> get props => <Object?>[from, to];
}

/// Filter of the earnings statement.
enum StatementPeriod {
  today,
  week,
  month;

  static const int _weekDays = 7;

  /// Range ending today: today only, the last 7 days, or since the 1st.
  DateRange rangeAt(DateTime now) {
    final DateTime today = DateTime(now.year, now.month, now.day);
    return switch (this) {
      StatementPeriod.today => DateRange(from: today, to: today),
      StatementPeriod.week => DateRange(
        from: today.subtract(const Duration(days: _weekDays - 1)),
        to: today,
      ),
      StatementPeriod.month => DateRange(
        from: DateTime(now.year, now.month),
        to: today,
      ),
    };
  }
}
