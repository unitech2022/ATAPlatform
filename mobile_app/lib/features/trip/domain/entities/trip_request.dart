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
    this.riderNote,
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
  final String? riderNote;

  TripRequest copyWith({PricingMode? pricingMode, double? offeredPrice}) =>
      TripRequest(
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
        riderNote: riderNote,
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
    riderNote,
  ];
}
