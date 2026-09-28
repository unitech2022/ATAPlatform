import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/data/models/trip_stop_model.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';

/// JSON mapping for [Offer] (`GET /driver/offers/active`, `OfferReceived`).
class OfferModel extends Offer {
  const OfferModel({
    required super.id,
    required super.tripId,
    required super.pickup,
    required super.dropoff,
    required super.expiresAt,
    super.stops,
    super.distanceToPickupMeters,
    super.etaSeconds,
    super.tripDistanceMeters,
    super.passengerPrice,
    super.driverNetEarnings,
    super.passengerFirstName,
    super.passengerRating,
    super.passengerOffered,
    super.round,
  });

  static const String offerPricingMode = 'offer';

  factory OfferModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic> passenger =
        JsonReaders.object(json, 'passenger') ?? const <String, dynamic>{};
    return OfferModel(
      id: JsonReaders.string(json, 'id'),
      tripId: JsonReaders.string(json, 'tripId'),
      pickup: TripStopModel.fromJson(
        JsonReaders.object(json, 'pickup') ?? const <String, dynamic>{},
      ),
      dropoff: TripStopModel.fromJson(
        JsonReaders.object(json, 'dropoff') ?? const <String, dynamic>{},
      ),
      stops: TripStopModel.listFromJson(json, 'stops'),
      distanceToPickupMeters: JsonReaders.integer(
        json,
        'distanceToPickupMeters',
      ),
      etaSeconds: JsonReaders.integer(json, 'etaSeconds'),
      tripDistanceMeters: JsonReaders.integer(json, 'tripDistanceMeters'),
      passengerPrice: JsonReaders.number(json, 'passengerPrice'),
      driverNetEarnings: JsonReaders.number(json, 'driverNetEarnings'),
      expiresAt:
          JsonReaders.date(json, 'expiresAt') ??
          DateTime.now().add(const Duration(seconds: 20)),
      passengerFirstName: JsonReaders.string(passenger, 'firstName'),
      passengerRating: JsonReaders.optionalNumber(passenger, 'ratingAvg'),
      passengerOffered:
          json['passengerOffered'] == true ||
          JsonReaders.optionalString(json, 'pricingMode') == offerPricingMode,
      round: JsonReaders.optionalInteger(json, 'round') ?? 1,
    );
  }

  Map<String, dynamic> toJson() => <String, dynamic>{
    'id': id,
    'tripId': tripId,
    'pickup': TripStopModel.toJsonOf(pickup),
    'dropoff': TripStopModel.toJsonOf(dropoff),
    'stops': stops.map(TripStopModel.toJsonOf).toList(growable: false),
    'distanceToPickupMeters': distanceToPickupMeters,
    'etaSeconds': etaSeconds,
    'tripDistanceMeters': tripDistanceMeters,
    'passengerPrice': passengerPrice,
    'driverNetEarnings': driverNetEarnings,
    'expiresAt': expiresAt.toIso8601String(),
    'passengerOffered': passengerOffered,
    'round': round,
    'passenger': <String, dynamic>{
      'firstName': passengerFirstName,
      'ratingAvg': passengerRating,
    },
  };
}
