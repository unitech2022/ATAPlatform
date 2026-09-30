import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /pricing/quote`. Every category is priced, so the selected
/// category is optional.
class QuoteRequest extends Equatable {
  const QuoteRequest({
    required this.pickup,
    required this.dropoff,
    this.stops = const <GeoPoint>[],
    this.rideCategoryId,
    this.bookingType = 'now',
    this.scheduledAt,
    this.promoCode,
    this.favoriteDriverId,
    this.airportPickupZoneId,
    this.airportTerminalCode,
    this.flightNumber,
  });

  final GeoPoint pickup;
  final GeoPoint dropoff;
  final List<GeoPoint> stops;
  final String? rideCategoryId;
  final String bookingType;
  final DateTime? scheduledAt;

  /// Applied promo code (F15); never sent with "offer your price".
  final String? promoCode;

  /// Selected favourite driver (F16): the quote assumes they accept.
  final String? favoriteDriverId;

  /// Airport fields (F17): the pickup point is the zone's point.
  final String? airportPickupZoneId;
  final String? airportTerminalCode;
  final String? flightNumber;

  @override
  List<Object?> get props => <Object?>[
    pickup,
    dropoff,
    stops,
    rideCategoryId,
    bookingType,
    scheduledAt,
    promoCode,
    favoriteDriverId,
    airportPickupZoneId,
    airportTerminalCode,
    flightNumber,
  ];
}
