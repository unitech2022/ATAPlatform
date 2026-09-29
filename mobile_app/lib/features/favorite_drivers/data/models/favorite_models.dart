import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of the F16 rider payloads (`docs/10` §F16.3).
abstract final class FavoriteModels {
  static FavoriteVehicle vehicle(Map<String, dynamic> json) {
    final Map<String, dynamic>? v = JsonReaders.object(json, 'vehicle');
    if (v == null) return const FavoriteVehicle();
    return FavoriteVehicle(
      make: JsonReaders.string(v, 'make'),
      model: JsonReaders.string(v, 'model'),
      color: JsonReaders.string(v, 'color'),
    );
  }

  static FavoriteDriver driver(Map<String, dynamic> json) => FavoriteDriver(
    driverId: JsonReaders.string(json, 'driverId'),
    firstName: JsonReaders.string(json, 'firstName'),
    photoUrl: JsonReaders.optionalString(json, 'photoUrl'),
    ratingAvg: JsonReaders.number(json, 'ratingAvg'),
    vehicle: vehicle(json),
    rideCategoryCode: JsonReaders.optionalString(json, 'rideCategoryCode'),
    tripsTogether: JsonReaders.integer(json, 'tripsTogether'),
    lastTripAt: JsonReaders.date(json, 'lastTripAt'),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  static AvailableFavorite available(Map<String, dynamic> json) {
    final Map<String, dynamic>? d = JsonReaders.object(json, 'discount');
    return AvailableFavorite(
      driverId: JsonReaders.string(json, 'driverId'),
      firstName: JsonReaders.string(json, 'firstName'),
      photoUrl: JsonReaders.optionalString(json, 'photoUrl'),
      ratingAvg: JsonReaders.number(json, 'ratingAvg'),
      vehicle: vehicle(json),
      etaMinutes: JsonReaders.optionalInteger(json, 'etaMinutes'),
      discount: d == null
          ? null
          : FavoriteDiscount(
              percent: JsonReaders.number(d, 'percent'),
              maxAmount: JsonReaders.optionalNumber(d, 'maxAmount'),
              stackableWithPromotions: d['stackableWithPromotions'] == true,
            ),
    );
  }

  static Map<String, dynamic> addBody(AddFavoriteParams p) => <String, dynamic>{
    'driverId': ?p.driverId,
    'tripId': ?p.tripId,
  };

  static Map<String, dynamic> availableQuery(AvailableFavoritesQuery q) =>
      <String, dynamic>{
        'lat': q.pickup.lat,
        'lng': q.pickup.lng,
        'rideCategoryId': ?q.rideCategoryId,
      };
}
