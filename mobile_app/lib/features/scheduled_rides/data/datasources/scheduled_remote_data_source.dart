import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/scheduled_rides/data/models/scheduled_models.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';

/// `/passenger/scheduling/rules`, `/passenger/trips/scheduled` and
/// `/driver/scheduled*` (F17.4).
class ScheduledRemoteDataSource {
  const ScheduledRemoteDataSource(this._api);

  final ApiClient _api;

  static const String rulesPath = '/passenger/scheduling/rules';
  static const String scheduledTripsPath = '/passenger/trips/scheduled';
  static const String marketplacePath = '/driver/scheduled/marketplace';
  static const String driverScheduledPath = '/driver/scheduled';

  Future<SchedulingRules> rules({String? rideCategoryId}) async =>
      ScheduledModels.rules(
        await _api.get(
              rulesPath,
              query: <String, dynamic>{'rideCategoryId': ?rideCategoryId},
            )
            as Map<String, dynamic>,
      );

  Future<List<Trip>> scheduledTrips() async => _objects(
    await _api.get(scheduledTripsPath),
  ).map(TripModel.fromJson).toList(growable: false);

  Future<PageResult<MarketplaceTrip>> marketplace(MarketplaceQuery q) async =>
      parsePage(
        await _api.get(
          marketplacePath,
          query: <String, dynamic>{
            'lat': q.position.lat,
            'lng': q.position.lng,
            'from': ?q.from?.toUtc().toIso8601String(),
            'to': ?q.to?.toUtc().toIso8601String(),
            'page': q.page,
          },
        ),
        ScheduledModels.marketplace,
      );

  Future<Reservation> reserve(String tripId) async =>
      ScheduledModels.reservation(
        await _api.post('$driverScheduledPath/$tripId/reserve')
            as Map<String, dynamic>,
      );

  Future<ConfirmResult> confirm(String tripId) async =>
      ScheduledModels.confirmResult(
        await _api.post('$driverScheduledPath/$tripId/confirm')
            as Map<String, dynamic>,
      );

  Future<Reservation> release(String tripId, {String? reason}) async =>
      ScheduledModels.reservation(
        await _api.post(
              '$driverScheduledPath/$tripId/release',
              body: <String, dynamic>{'reason': ?reason},
            )
            as Map<String, dynamic>,
      );

  Future<PageResult<Reservation>> reservations(ReservationsQuery q) async =>
      parsePage(
        await _api.get(
          driverScheduledPath,
          query: <String, dynamic>{'status': q.list.apiValue, 'page': q.page},
        ),
        ScheduledModels.reservation,
      );

  /// Reads a bare array or a `{ items, page, pageSize, total }` page.
  static PageResult<T> parsePage<T>(
    Object? body,
    T Function(Map<String, dynamic>) parse,
  ) {
    if (body is Map<String, dynamic>) {
      return PageResult<T>.fromJson(body, parse);
    }
    final List<T> items = _objects(body).map(parse).toList(growable: false);
    return PageResult<T>(
      items: items,
      page: 1,
      pageSize: items.length,
      total: items.length,
    );
  }

  static List<Map<String, dynamic>> _objects(Object? body) {
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    return items is List<dynamic>
        ? items.whereType<Map<String, dynamic>>().toList(growable: false)
        : const <Map<String, dynamic>>[];
  }
}
