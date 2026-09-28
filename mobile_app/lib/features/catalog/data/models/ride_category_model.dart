import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';

/// JSON mapping for [RideCategory].
class RideCategoryModel extends RideCategory {
  const RideCategoryModel({
    required super.id,
    required super.code,
    required super.name,
    required super.description,
    required super.icon,
    required super.seats,
    required super.maxStops,
    required super.sortOrder,
    super.estimate,
  });

  factory RideCategoryModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? estimate =
        json['estimate'] as Map<String, dynamic>?;
    return RideCategoryModel(
      id: json['id'] as String,
      code: json['code'] as String,
      name: json['name'] as String,
      description: json['description'] as String? ?? '',
      icon: json['icon'] as String? ?? 'car',
      seats: (json['seats'] as num?)?.toInt() ?? 4,
      maxStops: (json['maxStops'] as num?)?.toInt() ?? 0,
      sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
      estimate: estimate == null
          ? null
          : RideEstimate(
              etaMinutes: (estimate['etaMinutes'] as num?)?.toInt() ?? 0,
              price: (estimate['price'] as num?)?.toDouble() ?? 0,
            ),
    );
  }
}
