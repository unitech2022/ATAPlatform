import 'package:equatable/equatable.dart';

/// Lifecycle bucket of a trip as shown in lists.
enum TripStatus {
  completed('completed'),
  cancelled('cancelled'),
  active('active');

  const TripStatus(this.apiValue);

  final String apiValue;

  static TripStatus parse(String? value) {
    for (final TripStatus status in values) {
      if (status.apiValue == value) return status;
    }
    return active;
  }
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
    this.scheduledAt,
    this.completedAt,
  });

  final String id;
  final String destinationName;
  final String pickupName;
  final TripStatus status;
  final double fare;
  final String categoryName;
  final DateTime? scheduledAt;
  final DateTime? completedAt;

  /// Best timestamp to display.
  DateTime? get displayDate => completedAt ?? scheduledAt;

  @override
  List<Object?> get props => <Object?>[
    id,
    destinationName,
    pickupName,
    status,
    fare,
    categoryName,
    scheduledAt,
    completedAt,
  ];
}
