import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Builds the `POST /passenger/trips` body from the home sheet: the Step-1
/// places get their fixed Riyadh coordinates.
TripRequest buildTripRequest(HomeState state, AppLocalizations l10n) {
  final List<String> options = <String>[
    l10n.stopOption1,
    l10n.stopOption2,
    l10n.stopOption3,
  ];
  return TripRequest(
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
        TripStop(
          name: name,
          address: name,
          point: _pointFor(options.indexOf(name)),
        ),
    ],
    rideCategoryId: state.selectedCategory?.id ?? '',
    bookingType: state.rideTime == RideTime.scheduled ? 'scheduled' : 'now',
    paymentMethod: state.payment.apiValue,
    preferFemaleDriver: state.preferFemaleDriver,
    offeredPrice: state.offeredPrice,
  );
}

GeoPoint _pointFor(int index) =>
    index >= 0 && index < TripPlaces.stopOptions.length
    ? TripPlaces.stopOptions[index]
    : GeoPoint.riyadh;
