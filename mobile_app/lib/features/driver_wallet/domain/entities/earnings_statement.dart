import 'package:equatable/equatable.dart';

/// Period totals of `GET /driver/earnings/statement`.
class StatementTotals extends Equatable {
  const StatementTotals({
    this.trips = 0,
    this.grossFares = 0,
    this.commission = 0,
    this.earnings = 0,
    this.cashCollected = 0,
    this.incentives = 0,
    this.cancellationCompensation = 0,
    this.adjustments = 0,
    this.payouts = 0,
    this.net = 0,
  });

  final int trips;
  final double grossFares;
  final double commission;
  final double earnings;
  final double cashCollected;
  final double incentives;
  final double cancellationCompensation;
  final double adjustments;
  final double payouts;
  final double net;

  @override
  List<Object?> get props => <Object?>[
    trips,
    grossFares,
    commission,
    earnings,
    cashCollected,
    incentives,
    cancellationCompensation,
    adjustments,
    payouts,
    net,
  ];
}

/// One day of the statement.
class StatementDay extends Equatable {
  const StatementDay({
    required this.date,
    this.trips = 0,
    this.earnings = 0,
    this.cashCollected = 0,
    this.incentives = 0,
    this.onlineHours = 0,
  });

  final DateTime date;
  final int trips;
  final double earnings;
  final double cashCollected;
  final double incentives;
  final double onlineHours;

  @override
  List<Object?> get props => <Object?>[
    date,
    trips,
    earnings,
    cashCollected,
    incentives,
    onlineHours,
  ];
}

/// Earnings statement for a date range.
class EarningsStatement extends Equatable {
  const EarningsStatement({
    required this.from,
    required this.to,
    this.totals = const StatementTotals(),
    this.days = const <StatementDay>[],
  });

  final DateTime from;
  final DateTime to;
  final StatementTotals totals;
  final List<StatementDay> days;

  @override
  List<Object?> get props => <Object?>[from, to, totals, days];
}
