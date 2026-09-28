import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:equatable/equatable.dart';

/// A dispatch offer sent to a driver (`GET /driver/offers/active`).
class Offer extends Equatable {
  const Offer({
    required this.id,
    required this.tripId,
    required this.pickup,
    required this.dropoff,
    required this.expiresAt,
    this.stops = const <TripStop>[],
    this.distanceToPickupMeters = 0,
    this.etaSeconds = 0,
    this.tripDistanceMeters = 0,
    this.passengerPrice = 0,
    this.driverNetEarnings = 0,
    this.passengerFirstName = '',
    this.passengerRating,
  });

  final String id;
  final String tripId;
  final TripStop pickup;
  final TripStop dropoff;
  final List<TripStop> stops;
  final int distanceToPickupMeters;
  final int etaSeconds;
  final int tripDistanceMeters;
  final double passengerPrice;
  final double driverNetEarnings;
  final DateTime expiresAt;
  final String passengerFirstName;
  final double? passengerRating;

  int get etaMinutes => (etaSeconds / 60).ceil();

  @override
  List<Object?> get props => <Object?>[
    id,
    tripId,
    pickup,
    dropoff,
    stops,
    distanceToPickupMeters,
    etaSeconds,
    tripDistanceMeters,
    passengerPrice,
    driverNetEarnings,
    expiresAt,
    passengerFirstName,
    passengerRating,
  ];
}
