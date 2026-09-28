import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';

/// JSON mapping for [TripSummary].
class TripSummaryModel extends TripSummary {
  const TripSummaryModel({
    required super.id,
    required super.destinationName,
    required super.pickupName,
    required super.status,
    required super.fare,
    required super.categoryName,
    super.scheduledAt,
    super.completedAt,
  });

  factory TripSummaryModel.fromJson(Map<String, dynamic> json) =>
      TripSummaryModel(
        id: json['id'] as String,
        destinationName: json['destinationName'] as String? ?? '',
        pickupName: json['pickupName'] as String? ?? '',
        status: TripStatus.parse(json['status'] as String?),
        fare: (json['fare'] as num?)?.toDouble() ?? 0,
        categoryName: json['categoryName'] as String? ?? '',
        scheduledAt: DateTime.tryParse(json['scheduledAt'] as String? ?? ''),
        completedAt: DateTime.tryParse(json['completedAt'] as String? ?? ''),
      );
}
