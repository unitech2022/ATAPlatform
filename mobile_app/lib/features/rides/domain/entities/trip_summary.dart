import 'package:ata_app/features/trip/domain/entities/trip_rewards.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle bucket of a trip as shown in lists.
enum TripStatus {
  completed('completed'),
  cancelled('cancelled'),

  /// A booking waiting for its time (F17).
  scheduled('scheduled'),
  active('active');

  const TripStatus(this.apiValue);

  final String apiValue;

  /// Maps the raw lifecycle status (`completed`, `cancelled`, `no_drivers`,
  /// `searching`, `in_trip`, ...) to a list bucket.
  static TripStatus parse(String? value) => switch (value) {
    'completed' => completed,
    'scheduled' => scheduled,
    'cancelled' || 'no_drivers' => cancelled,
    _ => active,
  };
}

/// `TripSummary` of the API contract (passenger and driver lists).
class TripSummary extends Equatable {
  const TripSummary({
    required this.id,
    required this.destinationName,
    required this.pickupName,
    required this.status,
    required this.fare,
    required this.categoryName,
    this.tripNumber = '',
    this.scheduledAt,
    this.completedAt,
    this.requestedAt,
    this.earning,
    this.driverName = '',
    this.rating = const TripRatingInfo(),
  });

  final String id;
  final String tripNumber;
  final String destinationName;
  final String pickupName;
  final TripStatus status;
  final double fare;
  final String categoryName;
  final DateTime? scheduledAt;
  final DateTime? completedAt;
  final DateTime? requestedAt;

  /// Driver net earning (driver list only).
  final double? earning;

  /// Driver first name when the list includes it (rating sheet title).
  final String driverName;

  /// `myRating` / `canRate` / `rateUntil` (F15).
  final TripRatingInfo rating;

  /// A completed trip that can still be rated at [now] (72 h window).
  bool canRateAt(DateTime now) =>
      status == TripStatus.completed &&
      rating.canRateAt(now, completedAt: completedAt);

  /// Best timestamp to display.
  DateTime? get displayDate => completedAt ?? scheduledAt ?? requestedAt;

  /// Amount shown to the current role: the driver's earning when present.
  double get displayAmount => earning ?? fare;

  @override
  List<Object?> get props => <Object?>[
    id,
    tripNumber,
    destinationName,
    pickupName,
    status,
    fare,
    categoryName,
    scheduledAt,
    completedAt,
    requestedAt,
    earning,
    driverName,
    rating,
  ];
}
