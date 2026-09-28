import 'package:ata_app/features/pricing/data/models/demand_model.dart';
import 'package:ata_app/features/pricing/data/models/quote_category_model.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';

/// JSON mapping for [FareQuote] (`POST /pricing/quote`).
class QuoteModel extends FareQuote {
  const QuoteModel({
    required super.quoteId,
    required super.expiresAt,
    required super.distanceMeters,
    required super.durationSeconds,
    super.pickupZone,
    super.demand,
    super.categories,
  });

  /// Fallback validity when the API omits `expiresAt`.
  static const Duration defaultValidity = Duration(minutes: 5);

  factory QuoteModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? zone = JsonReaders.object(json, 'pickupZone');
    final Map<String, dynamic>? demand = JsonReaders.object(json, 'demand');
    return QuoteModel(
      quoteId: JsonReaders.string(json, 'quoteId'),
      expiresAt:
          JsonReaders.date(json, 'expiresAt') ??
          DateTime.now().add(defaultValidity),
      distanceMeters: JsonReaders.integer(json, 'distanceMeters'),
      durationSeconds: JsonReaders.integer(json, 'durationSeconds'),
      pickupZone: zone == null
          ? null
          : QuoteZone(
              id: JsonReaders.string(zone, 'id'),
              name: JsonReaders.string(zone, 'name'),
            ),
      demand: demand == null
          ? DemandLevel.normal
          : DemandModel.fromJson(demand),
      categories: JsonReaders.objects(
        json,
        'categories',
      ).map(QuoteCategoryModel.fromJson).toList(growable: false),
    );
  }

  Map<String, dynamic> toJson() => <String, dynamic>{
    'quoteId': quoteId,
    'expiresAt': expiresAt.toIso8601String(),
    'distanceMeters': distanceMeters,
    'durationSeconds': durationSeconds,
    'pickupZone': pickupZone == null
        ? null
        : <String, dynamic>{'id': pickupZone!.id, 'name': pickupZone!.name},
    'demand': <String, dynamic>{
      'code': demand.code.apiValue,
      'name': demand.name,
      'multiplier': demand.multiplier,
      'color': demand.color,
    },
    'categories': categories
        .map(QuoteCategoryModel.toJsonOf)
        .toList(growable: false),
  };
}

/// Request body for `POST /pricing/quote`.
abstract final class QuoteRequestMapper {
  static Map<String, dynamic> body(QuoteRequest request) => <String, dynamic>{
    'pickup': _point(request.pickup),
    'dropoff': _point(request.dropoff),
    'stops': request.stops.map(_point).toList(growable: false),
    'rideCategoryId': ?request.rideCategoryId,
    'bookingType': request.bookingType,
    'scheduledAt': ?request.scheduledAt?.toIso8601String(),
  };

  static Map<String, dynamic> _point(GeoPoint point) => <String, dynamic>{
    'lat': point.lat,
    'lng': point.lng,
  };
}
