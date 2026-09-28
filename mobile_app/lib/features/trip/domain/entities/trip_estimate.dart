import 'package:equatable/equatable.dart';

/// One category line of `POST /passenger/trips/estimate`.
class EstimateCategory extends Equatable {
  const EstimateCategory({
    required this.rideCategoryId,
    required this.code,
    required this.name,
    required this.etaMinutes,
    required this.estimatedFare,
    this.driverNetEarnings = 0,
  });

  final String rideCategoryId;
  final String code;
  final String name;
  final int etaMinutes;
  final double estimatedFare;
  final double driverNetEarnings;

  @override
  List<Object?> get props => <Object?>[
    rideCategoryId,
    code,
    name,
    etaMinutes,
    estimatedFare,
    driverNetEarnings,
  ];
}

/// Estimate of distance, duration and fare per category.
class TripEstimate extends Equatable {
  const TripEstimate({
    required this.distanceMeters,
    required this.durationSeconds,
    this.categories = const <EstimateCategory>[],
  });

  final int distanceMeters;
  final int durationSeconds;
  final List<EstimateCategory> categories;

  EstimateCategory? forCategory(String rideCategoryId) {
    for (final EstimateCategory category in categories) {
      if (category.rideCategoryId == rideCategoryId) return category;
    }
    return null;
  }

  @override
  List<Object?> get props => <Object?>[
    distanceMeters,
    durationSeconds,
    categories,
  ];
}
