import 'package:equatable/equatable.dart';

/// A frequent tag in the ratee's summary.
class RatingTagCount extends Equatable {
  const RatingTagCount({
    required this.code,
    this.name = '',
    this.count = 0,
    this.positive = true,
  });

  final String code;
  final String name;
  final int count;
  final bool positive;

  @override
  List<Object?> get props => <Object?>[code, name, count, positive];
}

/// An anonymous recent comment (week precision only, no trip or name).
class RatingComment extends Equatable {
  const RatingComment({required this.stars, this.comment = '', this.week = ''});

  final int stars;
  final String comment;

  /// ISO week such as `2026-W39`.
  final String week;

  @override
  List<Object?> get props => <Object?>[stars, comment, week];
}

/// `GET /driver|passenger/ratings/summary`.
class RatingSummary extends Equatable {
  const RatingSummary({
    this.ratingAvg = 5,
    this.ratingCount = 0,
    this.distribution = const <int, int>{},
    this.topTags = const <RatingTagCount>[],
    this.recentComments = const <RatingComment>[],
  });

  final double ratingAvg;
  final int ratingCount;

  /// Count per star (1..5).
  final Map<int, int> distribution;
  final List<RatingTagCount> topTags;
  final List<RatingComment> recentComments;

  int countFor(int stars) => distribution[stars] ?? 0;

  /// Share of [stars] among all counted ratings (0..1).
  double shareOf(int stars) {
    final int total = distribution.values.fold(0, (int a, int b) => a + b);
    return total == 0 ? 0 : countFor(stars) / total;
  }

  @override
  List<Object?> get props => <Object?>[
    ratingAvg,
    ratingCount,
    distribution,
    topTags,
    recentComments,
  ];
}
