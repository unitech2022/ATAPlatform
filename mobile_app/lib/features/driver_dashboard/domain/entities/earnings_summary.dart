import 'package:equatable/equatable.dart';

/// `GET /driver/earnings/summary`.
class EarningsSummary extends Equatable {
  const EarningsSummary({
    required this.todayEarnings,
    required this.todayTrips,
    required this.todayOnlineHours,
    required this.weekEarnings,
    required this.weekTarget,
    required this.ratingAvg,
  });

  const EarningsSummary.empty()
    : todayEarnings = 0,
      todayTrips = 0,
      todayOnlineHours = 0,
      weekEarnings = 0,
      weekTarget = defaultWeekTarget,
      ratingAvg = defaultRating;

  static const double defaultWeekTarget = 2500;
  static const double defaultRating = 5;

  final double todayEarnings;
  final int todayTrips;
  final double todayOnlineHours;
  final double weekEarnings;
  final double weekTarget;
  final double ratingAvg;

  /// 0..1 progress toward the weekly target.
  double get weekProgress =>
      weekTarget <= 0 ? 0 : (weekEarnings / weekTarget).clamp(0, 1);

  @override
  List<Object?> get props => <Object?>[
    todayEarnings,
    todayTrips,
    todayOnlineHours,
    weekEarnings,
    weekTarget,
    ratingAvg,
  ];
}
