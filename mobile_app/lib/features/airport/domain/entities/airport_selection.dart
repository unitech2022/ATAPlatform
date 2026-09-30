import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/flight_number.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:equatable/equatable.dart';

/// The rider's airport choice for a request: which airport, which end of
/// the trip, the pickup zone / terminal and the optional flight number.
class AirportSelection extends Equatable {
  const AirportSelection({
    required this.airport,
    required this.direction,
    this.zone,
    this.terminalCode,
    this.flightNumber,
  });

  final Airport airport;
  final AirportDirection direction;

  /// Pickup zone (airport pickups).
  final AirportZone? zone;

  /// Terminal of an airport dropoff (optional).
  final String? terminalCode;

  /// Normalized flight number, `null` when empty.
  final String? flightNumber;

  AirportSelection copyWith({
    AirportZone? zone,
    String? terminalCode,
    String? flightNumber,
    bool clearZone = false,
    bool clearTerminal = false,
    bool clearFlight = false,
  }) => AirportSelection(
    airport: airport,
    direction: direction,
    zone: clearZone ? null : zone ?? this.zone,
    terminalCode: clearTerminal ? null : terminalCode ?? this.terminalCode,
    flightNumber: clearFlight ? null : flightNumber ?? this.flightNumber,
  );

  bool get isPickup => direction == AirportDirection.pickup;

  /// A pickup still needs its zone.
  bool get needsZone => isPickup && airport.requiresPickupZone && zone == null;

  bool get hasValidFlight => FlightNumber.isValid(flightNumber);

  /// Ready to be sent with a quote / request.
  bool get isComplete => !needsZone && hasValidFlight;

  /// Coordinates that replace the pickup / dropoff of the route.
  GeoPoint get point => (isPickup ? zone?.point : null) ?? airport.point;

  /// Name shown on the route row.
  String get placeName => isPickup && zone != null
      ? '${airport.name} · ${zone!.name}'
      : airport.name;

  /// `airportPickupZoneId` of the request (pickups only).
  String? get pickupZoneId => isPickup ? zone?.id : null;

  /// `airportTerminalCode` of the request (dropoffs only).
  String? get dropoffTerminal => isPickup ? null : terminalCode;

  /// Free waiting minutes of the chosen zone (or the default).
  int? get freeWaitingMinutes => isPickup
      ? zone?.effectiveFreeWaitingMinutes
      : AirportZone.defaultFreeWaitingMinutes;

  @override
  List<Object?> get props => <Object?>[
    airport,
    direction,
    zone,
    terminalCode,
    flightNumber,
  ];
}
