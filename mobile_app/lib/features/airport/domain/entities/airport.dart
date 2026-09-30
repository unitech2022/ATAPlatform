import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// A terminal or pickup zone of an airport (`docs/11` §F17.8).
class AirportZone extends Equatable {
  const AirportZone({
    required this.id,
    required this.name,
    this.code = '',
    this.terminalCode,
    this.point,
    this.instructions,
    this.freeWaitingMinutes,
  });

  /// Free waiting minutes assumed when the zone does not define its own
  /// (`docs/11` decision 7: 15 minutes).
  static const int defaultFreeWaitingMinutes = 15;

  final String id;
  final String code;
  final String? terminalCode;
  final String name;

  /// Pickup point of a pickup zone.
  final GeoPoint? point;
  final String? instructions;
  final int? freeWaitingMinutes;

  int get effectiveFreeWaitingMinutes =>
      freeWaitingMinutes ?? defaultFreeWaitingMinutes;

  @override
  List<Object?> get props => <Object?>[
    id,
    code,
    terminalCode,
    name,
    point,
    instructions,
    freeWaitingMinutes,
  ];
}

/// An airport of `GET /catalog/airports` with its terminals and pickup zones.
class Airport extends Equatable {
  const Airport({
    required this.id,
    required this.code,
    required this.name,
    required this.point,
    this.terminals = const <AirportZone>[],
    this.pickupZones = const <AirportZone>[],
    this.requiresPickupZone = true,
  });

  final String id;
  final String code;
  final String name;
  final GeoPoint point;
  final List<AirportZone> terminals;
  final List<AirportZone> pickupZones;

  /// Pickups need a zone (`requires_pickup_zone`, true unless told otherwise).
  final bool requiresPickupZone;

  Airport copyWith({bool? requiresPickupZone}) => Airport(
    id: id,
    code: code,
    name: name,
    point: point,
    terminals: terminals,
    pickupZones: pickupZones,
    requiresPickupZone: requiresPickupZone ?? this.requiresPickupZone,
  );

  /// Pickup zones of one terminal.
  List<AirportZone> zonesOf(String terminalCode) => pickupZones
      .where((AirportZone z) => z.terminalCode == terminalCode)
      .toList(growable: false);

  @override
  List<Object?> get props => <Object?>[
    id,
    code,
    name,
    point,
    terminals,
    pickupZones,
    requiresPickupZone,
  ];
}

/// `GET /passenger/airports/resolve`: the airport around a point.
class AirportResolution extends Equatable {
  const AirportResolution({
    required this.airportId,
    required this.code,
    required this.name,
    this.requiresPickupZone = true,
    this.pickupZones = const <AirportZone>[],
    this.terminals = const <AirportZone>[],
  });

  final String airportId;
  final String code;
  final String name;
  final bool requiresPickupZone;
  final List<AirportZone> pickupZones;
  final List<AirportZone> terminals;

  @override
  List<Object?> get props => <Object?>[
    airportId,
    code,
    name,
    requiresPickupZone,
    pickupZones,
    terminals,
  ];
}
