import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// Airports catalog, resolution around a point and the driver queue (F17).
abstract interface class AirportRepository {
  Future<Either<Failure, List<Airport>>> getAirports();

  /// `null` when the point is not inside an active airport.
  Future<Either<Failure, AirportResolution?>> resolve(GeoPoint point);

  Future<Either<Failure, AirportQueueStatus>> getQueue();

  /// `422 not_in_airport_waiting_area` when the driver is elsewhere.
  Future<Either<Failure, AirportQueueStatus>> joinQueue(GeoPoint point);

  Future<Either<Failure, Unit>> leaveQueue();

  /// Hub `AirportQueueUpdated` events.
  Stream<AirportQueuePosition> watchQueue();
}
