import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/airport/domain/entities/flight_number.dart';
import 'package:equatable/equatable.dart';

/// Loading state of the airports catalog.
enum AirportCatalogStatus { idle, loading, ready, failure }

/// State of `AirportPickupCubit`.
class AirportPickupState extends Equatable {
  const AirportPickupState({
    this.status = AirportCatalogStatus.idle,
    this.airports = const <Airport>[],
    this.selection,
    this.flightInput = '',
    this.detected = false,
    this.failure,
  });

  final AirportCatalogStatus status;
  final List<Airport> airports;

  /// The airport, direction, zone / terminal and flight number chosen.
  final AirportSelection? selection;

  /// Text typed in the flight number field (as typed).
  final String flightInput;

  /// The selection came from `resolve` rather than from the rider.
  final bool detected;
  final Failure? failure;

  bool get hasSelection => selection != null;

  /// The flight field holds something that is not a flight number.
  bool get flightInvalid =>
      !FlightNumber.isValid(FlightNumber.normalize(flightInput));

  /// A pickup at an airport still needs a zone (blocks the request).
  bool get needsZone => selection?.needsZone ?? false;

  AirportPickupState copyWith({
    AirportCatalogStatus? status,
    List<Airport>? airports,
    AirportSelection? selection,
    String? flightInput,
    bool? detected,
    Failure? failure,
    bool clearSelection = false,
    bool clearFailure = false,
  }) => AirportPickupState(
    status: status ?? this.status,
    airports: airports ?? this.airports,
    selection: clearSelection ? null : selection ?? this.selection,
    flightInput: flightInput ?? this.flightInput,
    detected: detected ?? this.detected,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    airports,
    selection,
    flightInput,
    detected,
    failure,
  ];
}
