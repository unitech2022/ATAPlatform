import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/data/models/trip_parties_model.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';

/// JSON mapping of the F17 fields of `Trip` / `Offer`: `scheduling` and
/// `airport`.
abstract final class TripSchedulingModel {
  static TripScheduling? scheduling(Map<String, dynamic> json) {
    final Map<String, dynamic>? s = JsonReaders.object(json, 'scheduling');
    if (s == null) return null;
    final Map<String, dynamic>? r = JsonReaders.object(s, 'reservation');
    final Map<String, dynamic>? vehicle = r == null
        ? null
        : JsonReaders.object(r, 'vehicle');
    return TripScheduling(
      freeCancelUntil: JsonReaders.date(s, 'freeCancelUntil'),
      searchStartsAt: JsonReaders.date(s, 'searchStartsAt'),
      reservation: r == null
          ? null
          : TripReservationInfo(
              status: ReservationStatus.parse(
                JsonReaders.optionalString(r, 'status'),
              ),
              driverFirstName: JsonReaders.string(r, 'driverFirstName'),
              driverPhotoUrl: JsonReaders.optionalString(r, 'driverPhotoUrl'),
              ratingAvg: JsonReaders.optionalNumber(r, 'ratingAvg'),
              vehicle: vehicle == null
                  ? null
                  : TripVehicleModel.fromJson(vehicle),
              reservedAt: JsonReaders.date(r, 'reservedAt'),
            ),
    );
  }

  static Map<String, dynamic>? schedulingJson(TripScheduling? s) {
    if (s == null) return null;
    final TripReservationInfo? r = s.reservation;
    return <String, dynamic>{
      'freeCancelUntil': s.freeCancelUntil?.toIso8601String(),
      'searchStartsAt': s.searchStartsAt?.toIso8601String(),
      'reservation': r == null
          ? null
          : <String, dynamic>{
              'status': r.status.apiValue,
              'driverFirstName': r.driverFirstName,
              'driverPhotoUrl': r.driverPhotoUrl,
              'ratingAvg': r.ratingAvg,
              'vehicle': r.vehicle == null
                  ? null
                  : TripVehicleModel(
                      make: r.vehicle!.make,
                      model: r.vehicle!.model,
                      color: r.vehicle!.color,
                      plateNumber: r.vehicle!.plateNumber,
                    ).toJson(),
              'reservedAt': r.reservedAt?.toIso8601String(),
            },
    };
  }

  static TripAirport? airport(Map<String, dynamic> json) {
    final Map<String, dynamic>? a = JsonReaders.object(json, 'airport');
    if (a == null) return null;
    final AirportDirection? direction = AirportDirection.parse(
      JsonReaders.optionalString(a, 'direction'),
    );
    if (direction == null) return null;
    return TripAirport(
      code: JsonReaders.string(a, 'code'),
      direction: direction,
      zoneName: JsonReaders.optionalString(a, 'zoneName'),
      terminalCode: JsonReaders.optionalString(a, 'terminalCode'),
      flightNumber: JsonReaders.optionalString(a, 'flightNumber'),
      freeWaitingMinutes: JsonReaders.optionalInteger(a, 'freeWaitingMinutes'),
    );
  }

  static Map<String, dynamic>? airportJson(TripAirport? a) => a == null
      ? null
      : <String, dynamic>{
          'code': a.code,
          'direction': a.direction.apiValue,
          'zoneName': a.zoneName,
          'terminalCode': a.terminalCode,
          'flightNumber': a.flightNumber,
          'freeWaitingMinutes': a.freeWaitingMinutes,
        };
}
