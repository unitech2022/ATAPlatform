import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';

/// JSON mapping for the `DriverLocation` hub event.
class DriverLocationModel extends DriverLocationUpdate {
  const DriverLocationModel({
    required super.tripId,
    required super.point,
    super.heading,
    super.etaSeconds,
  });

  factory DriverLocationModel.fromJson(Map<String, dynamic> json) =>
      DriverLocationModel(
        tripId: JsonReaders.string(json, 'tripId'),
        point: GeoPoint(
          lat: JsonReaders.number(json, 'lat'),
          lng: JsonReaders.number(json, 'lng'),
        ),
        heading: JsonReaders.optionalNumber(json, 'heading'),
        etaSeconds: JsonReaders.optionalInteger(json, 'etaSeconds'),
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'tripId': tripId,
    'lat': point.lat,
    'lng': point.lng,
    'heading': heading,
    'etaSeconds': etaSeconds,
  };

  /// Body of `PUT /driver/location`.
  static Map<String, dynamic> positionToJson(DriverPosition position) =>
      <String, dynamic>{
        'lat': position.point.lat,
        'lng': position.point.lng,
        'heading': ?position.heading,
        'speed': ?position.speed,
        'accuracy': ?position.accuracy,
      };
}
