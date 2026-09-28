import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';

/// JSON mapping for [TripStop] (`{ name, address, lat, lng, arrivedAt? }`).
class TripStopModel extends TripStop {
  const TripStopModel({
    required super.name,
    required super.address,
    required super.point,
    super.arrivedAt,
  });

  factory TripStopModel.fromJson(Map<String, dynamic> json) => TripStopModel(
    name: JsonReaders.string(json, 'name'),
    address: JsonReaders.string(json, 'address'),
    point: GeoPoint(
      lat: JsonReaders.number(json, 'lat'),
      lng: JsonReaders.number(json, 'lng'),
    ),
    arrivedAt: JsonReaders.date(json, 'arrivedAt'),
  );

  factory TripStopModel.fromEntity(TripStop stop) => TripStopModel(
    name: stop.name,
    address: stop.address,
    point: stop.point,
    arrivedAt: stop.arrivedAt,
  );

  static List<TripStopModel> listFromJson(
    Map<String, dynamic> json,
    String key,
  ) => JsonReaders.objects(
    json,
    key,
  ).map(TripStopModel.fromJson).toList(growable: false);

  static Map<String, dynamic> toJsonOf(TripStop stop) => <String, dynamic>{
    'name': stop.name,
    'address': stop.address,
    'lat': stop.lat,
    'lng': stop.lng,
  };

  Map<String, dynamic> toJson() => <String, dynamic>{
    ...toJsonOf(this),
    'arrivedAt': ?arrivedAt?.toIso8601String(),
  };
}
