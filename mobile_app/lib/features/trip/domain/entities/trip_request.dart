import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:equatable/equatable.dart';

/// `fixed` (category price) or `offer` (passenger proposes a price).
enum PricingMode {
  fixed('fixed'),
  offer('offer');

  const PricingMode(this.apiValue);

  final String apiValue;
}

/// Body of `POST /passenger/trips` (the estimate uses a subset).
class TripRequest extends Equatable {
  const TripRequest({
    required this.pickup,
    required this.dropoff,
    required this.rideCategoryId,
    this.stops = const <TripStop>[],
    this.bookingType = 'now',
    this.scheduledAt,
    this.paymentMethod = 'cash',
    this.preferFemaleDriver = false,
    this.pricingMode = PricingMode.fixed,
    this.offeredPrice,
    this.quoteId,
    this.riderNote,
    this.promoCode,
    this.favoriteDriverId,
  });

  final TripStop pickup;
  final TripStop dropoff;
  final List<TripStop> stops;
  final String rideCategoryId;
  final String bookingType;
  final DateTime? scheduledAt;
  final String paymentMethod;
  final bool preferFemaleDriver;
  final PricingMode pricingMode;
  final double? offeredPrice;

  /// Locks the price of a `POST /pricing/quote` result (F10).
  final String? quoteId;
  final String? riderNote;

  /// Validated promo code (F15); reserved when the trip is created. Promo
  /// codes do not apply to `pricingMode: offer`.
  final String? promoCode;

  /// Favourite driver asked for first (F16); must be one of my favourites.
  /// Not offered with `pricingMode: offer`.
  final String? favoriteDriverId;

  TripRequest copyWith({
    PricingMode? pricingMode,
    double? offeredPrice,
    String? quoteId,
    bool clearQuoteId = false,
    bool clearPromoCode = false,
    bool clearFavoriteDriver = false,
  }) => TripRequest(
    pickup: pickup,
    dropoff: dropoff,
    rideCategoryId: rideCategoryId,
    stops: stops,
    bookingType: bookingType,
    scheduledAt: scheduledAt,
    paymentMethod: paymentMethod,
    preferFemaleDriver: preferFemaleDriver,
    pricingMode: pricingMode ?? this.pricingMode,
    offeredPrice: offeredPrice ?? this.offeredPrice,
    quoteId: clearQuoteId ? null : quoteId ?? this.quoteId,
    riderNote: riderNote,
    promoCode: clearPromoCode ? null : promoCode,
    favoriteDriverId: clearFavoriteDriver ? null : favoriteDriverId,
  );

  @override
  List<Object?> get props => <Object?>[
    pickup,
    dropoff,
    stops,
    rideCategoryId,
    bookingType,
    scheduledAt,
    paymentMethod,
    preferFemaleDriver,
    pricingMode,
    offeredPrice,
    quoteId,
    riderNote,
    promoCode,
    favoriteDriverId,
  ];
}
