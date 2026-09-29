import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Builds the `POST /passenger/trips` body from the home sheet: the Step-1
/// places get their fixed Riyadh coordinates, the usable quote is attached
/// as `quoteId`, the applied promo code as `promoCode` and the selected
/// favourite driver as `favoriteDriverId` (F16).
TripRequest buildTripRequest(HomeState state, AppLocalizations l10n) =>
    TripRequest(
      pickup: TripStop(
        name: l10n.pickupCurrent,
        address: l10n.pickupCurrent,
        point: TripPlaces.currentLocation,
      ),
      dropoff: TripStop(
        name: l10n.destinationDefault,
        address: l10n.destinationDefault,
        point: TripPlaces.defaultDestination,
      ),
      stops: <TripStop>[
        for (final String name in state.stops)
          TripStop(name: name, address: name, point: stopPointFor(name, l10n)),
      ],
      rideCategoryId: state.selectedCategory?.id ?? '',
      bookingType: bookingTypeOf(state.rideTime),
      paymentMethod: state.payment.apiValue,
      preferFemaleDriver: state.preferFemaleDriver,
      offeredPrice: state.offeredPrice,
      quoteId: state.quote?.quoteId,
      promoCode: state.effectivePromoCode,
      favoriteDriverId: state.effectiveFavoriteDriverId,
    );

/// Builds the `POST /pricing/quote` body for the same route.
QuoteRequest buildQuoteRequest(HomeState state, AppLocalizations l10n) =>
    QuoteRequest(
      pickup: TripPlaces.currentLocation,
      dropoff: TripPlaces.defaultDestination,
      stops: <GeoPoint>[
        for (final String name in state.stops) stopPointFor(name, l10n),
      ],
      bookingType: bookingTypeOf(state.rideTime),
      promoCode: state.effectivePromoCode,
      favoriteDriverId: state.effectiveFavoriteDriverId,
    );

/// Context of `POST /passenger/promotions/validate` for the current draft.
PromoValidationParams buildPromoContext(HomeState state) =>
    PromoValidationParams(
      code: '',
      quoteId: state.quote?.quoteId,
      rideCategoryId: state.selectedCategory?.id,
      paymentMethod: state.payment.apiValue,
      bookingType: bookingTypeOf(state.rideTime),
    );

String bookingTypeOf(RideTime time) =>
    time == RideTime.scheduled ? 'scheduled' : 'now';

/// Coordinates of a stop option (`stopOption1..3`), Riyadh centre otherwise.
GeoPoint stopPointFor(String name, AppLocalizations l10n) {
  final int index = <String>[
    l10n.stopOption1,
    l10n.stopOption2,
    l10n.stopOption3,
  ].indexOf(name);
  return index >= 0 && index < TripPlaces.stopOptions.length
      ? TripPlaces.stopOptions[index]
      : GeoPoint.riyadh;
}
