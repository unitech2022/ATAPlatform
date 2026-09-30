import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/airport/domain/entities/flight_number.dart';
import 'package:ata_app/features/airport/domain/usecases/get_airports.dart';
import 'package:ata_app/features/airport/domain/usecases/resolve_airport.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Airport choice of the request sheet (`docs/11` §F17.7): detects an
/// airport around the pickup / dropoff through `resolve`, or lets the rider
/// pick an airport and its end of the trip, then the pickup zone / terminal
/// and an optional flight number. The result feeds `HomeCubit`.
class AirportPickupCubit extends Cubit<AirportPickupState> {
  AirportPickupCubit({required this._getAirports, required this._resolve})
    : super(const AirportPickupState());

  final GetAirports _getAirports;
  final ResolveAirport _resolve;

  Future<void> loadAirports() async {
    emit(
      state.copyWith(status: AirportCatalogStatus.loading, clearFailure: true),
    );
    final result = await _getAirports(const NoParams());
    if (isClosed) return;
    result.fold(
      (failure) => emit(
        state.copyWith(status: AirportCatalogStatus.failure, failure: failure),
      ),
      (List<Airport> airports) => emit(
        state.copyWith(status: AirportCatalogStatus.ready, airports: airports),
      ),
    );
  }

  /// Asks the API whether [pickup] (then [dropoff]) is inside an airport and
  /// selects it. A choice made by the rider is never overridden.
  Future<void> detect(GeoPoint pickup, {GeoPoint? dropoff}) async {
    if (state.hasSelection && !state.detected) return;
    var direction = AirportDirection.pickup;
    var point = pickup;
    var resolution = (await _resolve(pickup)).getOrElse((_) => null);
    if (resolution == null && dropoff != null) {
      direction = AirportDirection.dropoff;
      point = dropoff;
      resolution = (await _resolve(dropoff)).getOrElse((_) => null);
    }
    if (isClosed) return;
    if (resolution == null) {
      if (state.detected) clear();
      return;
    }
    final AirportResolution found = resolution;
    final Airport known =
        state.airports
            .where((Airport a) => a.id == found.airportId)
            .firstOrNull ??
        Airport(
          id: found.airportId,
          code: found.code,
          name: found.name,
          point: point,
          terminals: found.terminals,
          pickupZones: found.pickupZones,
        );
    emit(
      state.copyWith(
        selection: AirportSelection(
          airport: known.copyWith(requiresPickupZone: found.requiresPickupZone),
          direction: direction,
          flightNumber: FlightNumber.normalize(state.flightInput),
        ),
        detected: true,
      ),
    );
  }

  /// The rider picks [airport] as the pickup or the dropoff.
  void choose(Airport airport, AirportDirection direction) => emit(
    state.copyWith(
      selection: AirportSelection(
        airport: airport,
        direction: direction,
        flightNumber: FlightNumber.normalize(state.flightInput),
      ),
      detected: false,
    ),
  );

  /// Switches the chosen airport between pickup and dropoff.
  void setDirection(AirportDirection direction) {
    final AirportSelection? current = state.selection;
    if (current == null || current.direction == direction) return;
    choose(current.airport, direction);
  }

  void selectZone(AirportZone zone) {
    final AirportSelection? current = state.selection;
    if (current == null) return;
    emit(
      state.copyWith(
        selection: current.copyWith(
          zone: zone,
          terminalCode: zone.terminalCode,
        ),
      ),
    );
  }

  /// Terminal of an airport dropoff; `null` clears it.
  void selectTerminal(String? terminalCode) {
    final AirportSelection? current = state.selection;
    if (current == null) return;
    emit(
      state.copyWith(
        selection: terminalCode == null
            ? current.copyWith(clearTerminal: true)
            : current.copyWith(terminalCode: terminalCode),
      ),
    );
  }

  void setFlightNumber(String input) {
    final AirportSelection? current = state.selection;
    final String? normalized = FlightNumber.normalize(input);
    emit(
      state.copyWith(
        flightInput: input,
        selection: current == null
            ? null
            : normalized == null
            ? current.copyWith(clearFlight: true)
            : current.copyWith(flightNumber: normalized),
      ),
    );
  }

  /// Back to a plain city trip.
  void clear() => emit(
    state.copyWith(clearSelection: true, flightInput: '', detected: false),
  );
}
