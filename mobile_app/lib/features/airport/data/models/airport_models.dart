import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';

/// JSON mapping of the airport endpoints (`docs/11` §F17.8).
abstract final class AirportModels {
  static AirportZone zone(Map<String, dynamic> json) {
    final double? lat = JsonReaders.optionalNumber(json, 'lat');
    final double? lng = JsonReaders.optionalNumber(json, 'lng');
    return AirportZone(
      id: JsonReaders.string(json, 'id'),
      code: JsonReaders.string(json, 'code'),
      terminalCode: JsonReaders.optionalString(json, 'terminalCode'),
      name: JsonReaders.string(json, 'name'),
      point: lat == null || lng == null ? null : GeoPoint(lat: lat, lng: lng),
      instructions: JsonReaders.optionalString(json, 'instructions'),
      freeWaitingMinutes: JsonReaders.optionalInteger(
        json,
        'freeWaitingMinutes',
      ),
    );
  }

  static List<AirportZone> zones(Map<String, dynamic> json, String key) =>
      JsonReaders.objects(json, key).map(zone).toList(growable: false);

  static Airport airport(Map<String, dynamic> json) => Airport(
    id: JsonReaders.string(json, 'id'),
    code: JsonReaders.string(json, 'code'),
    name: JsonReaders.string(json, 'name'),
    point: GeoPoint(
      lat: JsonReaders.number(json, 'lat'),
      lng: JsonReaders.number(json, 'lng'),
    ),
    terminals: zones(json, 'terminals'),
    pickupZones: zones(json, 'pickupZones'),
    requiresPickupZone: json['requiresPickupZone'] != false,
  );

  /// `{ airport: { id, code, name }, requiresPickupZone, pickupZones,
  /// terminals }`; anything else means "not at an airport".
  static AirportResolution? resolution(Object? body) {
    if (body is! Map<String, dynamic>) return null;
    final Map<String, dynamic>? a = JsonReaders.object(body, 'airport');
    if (a == null) return null;
    return AirportResolution(
      airportId: JsonReaders.string(a, 'id'),
      code: JsonReaders.string(a, 'code'),
      name: JsonReaders.string(a, 'name'),
      requiresPickupZone: body['requiresPickupZone'] != false,
      pickupZones: zones(body, 'pickupZones'),
      terminals: zones(body, 'terminals'),
    );
  }

  static AirportRef? _ref(Map<String, dynamic> json, String key) {
    final Map<String, dynamic>? a = JsonReaders.object(json, key);
    return a == null
        ? null
        : AirportRef(
            id: JsonReaders.string(a, 'id'),
            code: JsonReaders.string(a, 'code'),
            name: JsonReaders.string(a, 'name'),
          );
  }

  static AirportQueueStatus queue(Map<String, dynamic> json) =>
      AirportQueueStatus(
        inQueue: json['inQueue'] == true,
        airport: _ref(json, 'airport'),
        position: JsonReaders.optionalInteger(json, 'position'),
        total: JsonReaders.optionalInteger(json, 'total'),
        enteredAt: JsonReaders.date(json, 'enteredAt'),
        estimatedWaitMinutes: JsonReaders.optionalInteger(
          json,
          'estimatedWaitMinutes',
        ),
        eligibleAirport: _ref(json, 'eligibleAirport'),
      );

  static AirportQueuePosition queuePosition(Map<String, dynamic> json) =>
      AirportQueuePosition(
        position: JsonReaders.integer(json, 'position'),
        total: JsonReaders.integer(json, 'total'),
        estimatedWaitMinutes: JsonReaders.optionalInteger(
          json,
          'estimatedWaitMinutes',
        ),
      );
}
