import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';

/// JSON mapping for [EstimateCategory].
class EstimateCategoryModel extends EstimateCategory {
  const EstimateCategoryModel({
    required super.rideCategoryId,
    required super.code,
    required super.name,
    required super.etaMinutes,
    required super.estimatedFare,
    super.driverNetEarnings,
  });

  factory EstimateCategoryModel.fromJson(Map<String, dynamic> json) =>
      EstimateCategoryModel(
        rideCategoryId: JsonReaders.string(json, 'rideCategoryId'),
        code: JsonReaders.string(json, 'code'),
        name: JsonReaders.string(json, 'name'),
        etaMinutes: JsonReaders.integer(json, 'etaMinutes'),
        estimatedFare: JsonReaders.number(json, 'estimatedFare'),
        driverNetEarnings: JsonReaders.number(json, 'driverNetEarnings'),
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'rideCategoryId': rideCategoryId,
    'code': code,
    'name': name,
    'etaMinutes': etaMinutes,
    'estimatedFare': estimatedFare,
    'driverNetEarnings': driverNetEarnings,
  };
}

/// JSON mapping for [TripEstimate] (`POST /passenger/trips/estimate`).
class EstimateModel extends TripEstimate {
  const EstimateModel({
    required super.distanceMeters,
    required super.durationSeconds,
    super.categories,
  });

  factory EstimateModel.fromJson(Map<String, dynamic> json) => EstimateModel(
    distanceMeters: JsonReaders.integer(json, 'distanceMeters'),
    durationSeconds: JsonReaders.integer(json, 'durationSeconds'),
    categories: JsonReaders.objects(
      json,
      'categories',
    ).map(EstimateCategoryModel.fromJson).toList(growable: false),
  );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'distanceMeters': distanceMeters,
    'durationSeconds': durationSeconds,
    'categories': categories
        .map(
          (category) => <String, dynamic>{
            'rideCategoryId': category.rideCategoryId,
            'code': category.code,
            'name': category.name,
            'etaMinutes': category.etaMinutes,
            'estimatedFare': category.estimatedFare,
            'driverNetEarnings': category.driverNetEarnings,
          },
        )
        .toList(growable: false),
  };
}
