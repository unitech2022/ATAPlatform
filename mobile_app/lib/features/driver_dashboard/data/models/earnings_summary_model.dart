import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';

/// JSON mapping for [EarningsSummary].
class EarningsSummaryModel extends EarningsSummary {
  const EarningsSummaryModel({
    required super.todayEarnings,
    required super.todayTrips,
    required super.todayOnlineHours,
    required super.weekEarnings,
    required super.weekTarget,
    required super.ratingAvg,
  });

  factory EarningsSummaryModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic> today =
        json['today'] as Map<String, dynamic>? ?? const <String, dynamic>{};
    final Map<String, dynamic> week =
        json['week'] as Map<String, dynamic>? ?? const <String, dynamic>{};
    return EarningsSummaryModel(
      todayEarnings: (today['earnings'] as num?)?.toDouble() ?? 0,
      todayTrips: (today['trips'] as num?)?.toInt() ?? 0,
      todayOnlineHours: (today['onlineHours'] as num?)?.toDouble() ?? 0,
      weekEarnings: (week['earnings'] as num?)?.toDouble() ?? 0,
      weekTarget:
          (week['target'] as num?)?.toDouble() ??
          EarningsSummary.defaultWeekTarget,
      ratingAvg:
          (json['ratingAvg'] as num?)?.toDouble() ??
          EarningsSummary.defaultRating,
    );
  }
}
