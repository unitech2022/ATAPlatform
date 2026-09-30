import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/airport/data/models/airport_models.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';

/// `/catalog/airports`, `/passenger/airports/resolve` and
/// `/driver/airport-queue*` (F17.8).
class AirportRemoteDataSource {
  const AirportRemoteDataSource(this._api);

  final ApiClient _api;

  static const String airportsPath = '/catalog/airports';
  static const String resolvePath = '/passenger/airports/resolve';
  static const String queuePath = '/driver/airport-queue';

  Future<List<Airport>> airports() async {
    final Object? body = await _api.get(airportsPath);
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    return items is List<dynamic>
        ? items
              .whereType<Map<String, dynamic>>()
              .map(AirportModels.airport)
              .toList(growable: false)
        : const <Airport>[];
  }

  Future<AirportResolution?> resolve(GeoPoint point) async =>
      AirportModels.resolution(
        await _api.get(
          resolvePath,
          query: <String, dynamic>{'lat': point.lat, 'lng': point.lng},
        ),
      );

  Future<AirportQueueStatus> queue() async =>
      AirportModels.queue(await _api.get(queuePath) as Map<String, dynamic>);

  Future<AirportQueueStatus> join(GeoPoint point) async => AirportModels.queue(
    await _api.post(
          '$queuePath/join',
          body: <String, dynamic>{'lat': point.lat, 'lng': point.lng},
        )
        as Map<String, dynamic>,
  );

  Future<void> leave() => _api.post('$queuePath/leave');
}
