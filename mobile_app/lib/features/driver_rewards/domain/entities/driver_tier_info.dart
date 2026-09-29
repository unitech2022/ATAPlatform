import 'package:equatable/equatable.dart';

/// Driver levels (`driver_tier_rules.tier`), lowest first.
enum DriverTier {
  bronze('bronze'),
  silver('silver'),
  gold('gold'),
  platinum('platinum');

  const DriverTier(this.apiValue);

  final String apiValue;

  static DriverTier parse(String? value) => values.firstWhere(
    (DriverTier t) => t.apiValue == value,
    orElse: () => bronze,
  );

  static DriverTier? tryParse(String? value) =>
      value == null ? null : parse(value);
}

/// Metrics of the last `periodDays` (rates 0..1).
class TierMetrics extends Equatable {
  const TierMetrics({
    this.completedTrips = 0,
    this.ratingAvg = 0,
    this.acceptanceRate = 0,
    this.cancellationRate = 0,
  });

  final int completedTrips;
  final double ratingAvg;
  final double acceptanceRate;
  final double cancellationRate;

  @override
  List<Object?> get props => <Object?>[
    completedTrips,
    ratingAvg,
    acceptanceRate,
    cancellationRate,
  ];
}

/// Thresholds of the next tier (a threshold met exactly counts as met).
class TierRequirements extends Equatable {
  const TierRequirements({
    this.minCompletedTrips = 0,
    this.minRatingAvg = 0,
    this.minAcceptanceRate = 0,
    this.maxCancellationRate = 1,
  });

  final int minCompletedTrips;
  final double minRatingAvg;
  final double minAcceptanceRate;
  final double maxCancellationRate;

  @override
  List<Object?> get props => <Object?>[
    minCompletedTrips,
    minRatingAvg,
    minAcceptanceRate,
    maxCancellationRate,
  ];
}

/// What the current tier gives.
class TierBenefits extends Equatable {
  const TierBenefits({this.commissionDiscountPercent = 0, this.text = ''});

  final double commissionDiscountPercent;
  final String text;

  @override
  List<Object?> get props => <Object?>[commissionDiscountPercent, text];
}

/// Criteria compared on the tier page.
enum TierCriterion { trips, rating, acceptance, cancellation }

/// One criterion: current value vs. the next tier's threshold.
class TierCheck extends Equatable {
  const TierCheck({
    required this.criterion,
    required this.current,
    required this.target,
    required this.met,
  });

  final TierCriterion criterion;
  final double current;
  final double target;
  final bool met;

  /// 0..1 towards [target] (1 when met).
  double get progress {
    if (met) return 1;
    if (criterion == TierCriterion.cancellation) {
      return current <= 0 ? 1 : (target / current).clamp(0, 1).toDouble();
    }
    return target <= 0 ? 1 : (current / target).clamp(0, 1).toDouble();
  }

  @override
  List<Object?> get props => <Object?>[criterion, current, target, met];
}

/// `GET /driver/tier`.
class DriverTierInfo extends Equatable {
  const DriverTierInfo({
    required this.tier,
    this.nextTier,
    this.periodDays = 28,
    this.metrics = const TierMetrics(),
    this.nextRequirements,
    this.benefits = const TierBenefits(),
    this.recalculatesAt,
  });

  final DriverTier tier;

  /// `null` at the top tier.
  final DriverTier? nextTier;
  final int periodDays;
  final TierMetrics metrics;
  final TierRequirements? nextRequirements;
  final TierBenefits benefits;
  final DateTime? recalculatesAt;

  bool get isTopTier => nextTier == null || nextRequirements == null;

  /// Every criterion of the next tier; empty at the top tier.
  List<TierCheck> get checks {
    final TierRequirements? r = nextRequirements;
    if (r == null) return const <TierCheck>[];
    final TierMetrics m = metrics;
    return <TierCheck>[
      TierCheck(
        criterion: TierCriterion.trips,
        current: m.completedTrips.toDouble(),
        target: r.minCompletedTrips.toDouble(),
        met: m.completedTrips >= r.minCompletedTrips,
      ),
      TierCheck(
        criterion: TierCriterion.rating,
        current: m.ratingAvg,
        target: r.minRatingAvg,
        met: m.ratingAvg >= r.minRatingAvg,
      ),
      TierCheck(
        criterion: TierCriterion.acceptance,
        current: m.acceptanceRate,
        target: r.minAcceptanceRate,
        met: m.acceptanceRate >= r.minAcceptanceRate,
      ),
      TierCheck(
        criterion: TierCriterion.cancellation,
        current: m.cancellationRate,
        target: r.maxCancellationRate,
        met: m.cancellationRate <= r.maxCancellationRate,
      ),
    ];
  }

  int get metCount => checks.where((TierCheck c) => c.met).length;

  /// Overall progress to the next tier: the mean of the criteria (1 at the
  /// top tier).
  double get progress {
    final List<TierCheck> all = checks;
    if (all.isEmpty) return 1;
    return all.fold(0.0, (double sum, TierCheck c) => sum + c.progress) /
        all.length;
  }

  /// Trips still needed for the next tier.
  int get tripsToNext {
    final TierRequirements? r = nextRequirements;
    if (r == null) return 0;
    final int left = r.minCompletedTrips - metrics.completedTrips;
    return left < 0 ? 0 : left;
  }

  @override
  List<Object?> get props => <Object?>[
    tier,
    nextTier,
    periodDays,
    metrics,
    nextRequirements,
    benefits,
    recalculatesAt,
  ];
}
