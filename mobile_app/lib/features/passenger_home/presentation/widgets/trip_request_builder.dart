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
/// favourite driver as `favoriteDriverId` (F16). A scheduled ride carries
/// `scheduledAt` (F17) and an airport trip the airport zone / terminal and
/// the flight number (the airport replaces the pickup or the dropoff). A
/// company-account trip (F19) carries `tripPurpose` and `costCenterId`;
/// its quote asks the API to evaluate the company policy.
TripRequest buildTripRequest(HomeState state, AppLocalizations l10n) =>
    TripRequest(
      pickup: TripStop(
        name: pickupName(state, l10n),
        address: pickupName(state, l10n),
        point: state.pickupPoint,
      ),
      dropoff: TripStop(
        name: dropoffName(state, l10n),
        address: dropoffName(state, l10n),
        point: state.dropoffPoint,
      ),
      stops: <TripStop>[
        for (final String name in state.stops)
          TripStop(name: name, address: name, point: stopPointFor(name, l10n)),
      ],
      rideCategoryId: state.selectedCategory?.id ?? '',
      bookingType: bookingTypeFor(state),
      scheduledAt: state.isScheduled ? state.scheduledAt : null,
      paymentMethod: state.payment.apiValue,
      preferFemaleDriver: state.preferFemaleDriver,
      offeredPrice: state.offeredPrice,
      quoteId: state.quote?.quoteId,
      promoCode: state.effectivePromoCode,
      favoriteDriverId: state.effectiveFavoriteDriverId,
      airportPickupZoneId: state.airport?.pickupZoneId,
      airportTerminalCode: state.airport?.dropoffTerminal,
      flightNumber: state.airport?.flightNumber,
      tripPurpose: state.corporateBooking?.tripPurpose,
      costCenterId: state.corporateBooking?.costCenterId,
    );

/// Name of the pickup row: the airport (and zone) or "my location".
String pickupName(HomeState state, AppLocalizations l10n) =>
    state.airport != null && state.airport!.isPickup
    ? state.airport!.placeName
    : l10n.pickupCurrent;

/// Name of the destination row: the airport or the default destination.
String dropoffName(HomeState state, AppLocalizations l10n) =>
    state.airport != null && !state.airport!.isPickup
    ? state.airport!.placeName
    : l10n.destinationDefault;

/// Builds the `POST /pricing/quote` body for the same route.
QuoteRequest buildQuoteRequest(HomeState state, AppLocalizations l10n) =>
    QuoteRequest(
      pickup: state.pickupPoint,
      dropoff: state.dropoffPoint,
      stops: <GeoPoint>[
        for (final String name in state.stops) stopPointFor(name, l10n),
      ],
      bookingType: bookingTypeFor(state),
      scheduledAt: state.isScheduled ? state.scheduledAt : null,
      promoCode: state.effectivePromoCode,
      favoriteDriverId: state.effectiveFavoriteDriverId,
      airportPickupZoneId: state.airport?.pickupZoneId,
      airportTerminalCode: state.airport?.dropoffTerminal,
      flightNumber: state.airport?.flightNumber,
      paymentMethod: state.isCorporate ? state.payment.apiValue : null,
      tripPurpose: state.corporateBooking?.tripPurpose,
      costCenterId: state.corporateBooking?.costCenterId,
    );

/// Context of `POST /passenger/promotions/validate` for the current draft.
PromoValidationParams buildPromoContext(HomeState state) =>
    PromoValidationParams(
      code: '',
      quoteId: state.quote?.quoteId,
      rideCategoryId: state.selectedCategory?.id,
      paymentMethod: state.payment.apiValue,
      bookingType: bookingTypeFor(state),
    );

/// `scheduled` only once the rider confirmed a time (`scheduledAt` set).
String bookingTypeFor(HomeState state) =>
    state.isScheduled && state.scheduledAt != null ? 'scheduled' : 'now';

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
