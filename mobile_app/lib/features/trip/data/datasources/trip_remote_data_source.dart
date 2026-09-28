import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/trip/data/models/estimate_model.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';

/// REST endpoints of F8: `/passenger/trips/*` and `/driver/*`.
class TripRemoteDataSource {
  const TripRemoteDataSource(this._api);

  final ApiClient _api;

  static const String passengerTripsPath = '/passenger/trips';
  static const String driverLocationPath = '/driver/location';
  static const String driverOffersPath = '/driver/offers';
  static const String driverTripsPath = '/driver/trips';
  static const String cancelAction = 'cancel';
  static const String verifyPinAction = 'verify-pin';

  // Passenger

  Future<EstimateModel> estimate(Map<String, dynamic> body) async =>
      EstimateModel.fromJson(
        await _api.post('$passengerTripsPath/estimate', body: body)
            as Map<String, dynamic>,
      );

  Future<TripModel> requestTrip(Map<String, dynamic> body) async =>
      TripModel.fromJson(
        await _api.post(passengerTripsPath, body: body) as Map<String, dynamic>,
      );

  Future<TripModel?> passengerActiveTrip() async =>
      _tripOrNull(await _api.get('$passengerTripsPath/active'));

  Future<TripModel> passengerTrip(String id) async => TripModel.fromJson(
    await _api.get('$passengerTripsPath/$id') as Map<String, dynamic>,
  );

  Future<TripModel> passengerCancel(
    String id,
    Map<String, dynamic> body,
  ) async => TripModel.fromJson(
    await _api.post('$passengerTripsPath/$id/$cancelAction', body: body)
        as Map<String, dynamic>,
  );

  // Driver

  Future<void> sendLocation(Map<String, dynamic> body) =>
      _api.put(driverLocationPath, body: body);

  Future<OfferModel?> activeOffer() async {
    final Object? body = await _api.get('$driverOffersPath/active');
    return body is Map<String, dynamic> ? OfferModel.fromJson(body) : null;
  }

  Future<TripModel> acceptOffer(String id) async => TripModel.fromJson(
    await _api.post('$driverOffersPath/$id/accept') as Map<String, dynamic>,
  );

  Future<void> rejectOffer(String id, Map<String, dynamic> body) =>
      _api.post('$driverOffersPath/$id/reject', body: body);

  Future<TripModel?> driverActiveTrip() async =>
      _tripOrNull(await _api.get('$driverTripsPath/active'));

  /// `en-route`, `arrived`, `verify-pin`, `start`, `complete` or `cancel`.
  Future<TripModel> driverTripAction(
    String id,
    String action, {
    Map<String, dynamic>? body,
  }) async => TripModel.fromJson(
    await _api.post('$driverTripsPath/$id/$action', body: body)
        as Map<String, dynamic>,
  );

  TripModel? _tripOrNull(Object? body) =>
      body is Map<String, dynamic> ? TripModel.fromJson(body) : null;
}
