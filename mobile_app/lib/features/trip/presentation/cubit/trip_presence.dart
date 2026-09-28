import 'package:ata_app/features/trip/presentation/cubit/active_trip_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:equatable/equatable.dart';

/// What the router needs to know about ongoing trips and offers.
class TripPresence extends Equatable {
  const TripPresence({
    this.hasPassengerTrip = false,
    this.hasDriverTrip = false,
    this.hasOffer = false,
  });

  factory TripPresence.of({
    required ActiveTripState activeTrip,
    required DriverTripState driverTrip,
    required DriverOfferState driverOffer,
  }) => TripPresence(
    hasPassengerTrip: activeTrip.hasTrip,
    hasDriverTrip: driverTrip.hasTrip,
    hasOffer: driverOffer.hasOffer,
  );

  final bool hasPassengerTrip;
  final bool hasDriverTrip;
  final bool hasOffer;

  @override
  List<Object?> get props => <Object?>[
    hasPassengerTrip,
    hasDriverTrip,
    hasOffer,
  ];
}
