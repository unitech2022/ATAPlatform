import 'package:ata_app/features/trip/data/models/trip_stop_model.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';

/// Request bodies for the estimate and request endpoints.
abstract final class TripRequestMapper {
  static Map<String, dynamic> estimateBody(
    TripRequest request,
  ) => <String, dynamic>{
    'pickup': TripStopModel.toJsonOf(request.pickup),
    'dropoff': TripStopModel.toJsonOf(request.dropoff),
    'stops': request.stops.map(TripStopModel.toJsonOf).toList(growable: false),
    'rideCategoryId': request.rideCategoryId,
    'bookingType': request.bookingType,
    'scheduledAt': ?request.scheduledAt?.toUtc().toIso8601String(),
  };

  static Map<String, dynamic> requestBody(TripRequest request) =>
      <String, dynamic>{
        ...estimateBody(request),
        'paymentMethod': request.paymentMethod,
        'preferFemaleDriver': request.preferFemaleDriver,
        'pricingMode': request.pricingMode.apiValue,
        'offeredPrice': ?request.offeredPrice,
        'quoteId': ?request.quoteId,
        'riderNote': ?request.riderNote,
        'promoCode': ?request.promoCode,
        'favoriteDriverId': ?request.favoriteDriverId,
        'airportPickupZoneId': ?request.airportPickupZoneId,
        'airportTerminalCode': ?request.airportTerminalCode,
        'flightNumber': ?request.flightNumber,
      };
}
