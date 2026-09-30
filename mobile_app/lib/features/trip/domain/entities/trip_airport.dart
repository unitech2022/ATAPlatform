import 'package:equatable/equatable.dart';

/// Which end of the trip is at the airport.
enum AirportDirection {
  pickup('pickup'),
  dropoff('dropoff');

  const AirportDirection(this.apiValue);

  final String apiValue;

  static AirportDirection? parse(String? value) {
    for (final AirportDirection d in values) {
      if (d.apiValue == value) return d;
    }
    return null;
  }
}

/// `Trip.airport` / `Offer.airport` (`docs/11` §F17.8). Offers never carry
/// the flight number.
class TripAirport extends Equatable {
  const TripAirport({
    required this.code,
    required this.direction,
    this.zoneName,
    this.terminalCode,
    this.flightNumber,
    this.freeWaitingMinutes,
  });

  final String code;
  final AirportDirection direction;
  final String? zoneName;
  final String? terminalCode;
  final String? flightNumber;
  final int? freeWaitingMinutes;

  bool get isPickup => direction == AirportDirection.pickup;

  @override
  List<Object?> get props => <Object?>[
    code,
    direction,
    zoneName,
    terminalCode,
    flightNumber,
    freeWaitingMinutes,
  ];
}
