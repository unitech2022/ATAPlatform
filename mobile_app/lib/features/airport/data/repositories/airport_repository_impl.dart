import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/data/datasources/airport_remote_data_source.dart';
import 'package:ata_app/features/airport/data/models/airport_models.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// [AirportRepository] backed by the API and the trips hub.
class AirportRepositoryImpl implements AirportRepository {
  const AirportRepositoryImpl({required this._remote, required this._realtime});

  final AirportRemoteDataSource _remote;
  final TripRealtimeDataSource _realtime;

  @override
  Future<Either<Failure, List<Airport>>> getAirports() =>
      guard(_remote.airports);

  @override
  Future<Either<Failure, AirportResolution?>> resolve(GeoPoint point) =>
      guard(() => _remote.resolve(point));

  @override
  Future<Either<Failure, AirportQueueStatus>> getQueue() =>
      guard(_remote.queue);

  @override
  Future<Either<Failure, AirportQueueStatus>> joinQueue(GeoPoint point) =>
      guard(() => _remote.join(point));

  @override
  Future<Either<Failure, Unit>> leaveQueue() => guard(() async {
    await _remote.leave();
    return unit;
  });

  @override
  Stream<AirportQueuePosition> watchQueue() {
    // The hub connection is reference counted; hold it while listening.
    Stream<AirportQueuePosition> events() =>
        _realtime.airportQueueUpdated.map(AirportModels.queuePosition);
    return _held(events);
  }

  Stream<AirportQueuePosition> _held(
    Stream<AirportQueuePosition> Function() source,
  ) async* {
    await _realtime.acquire();
    try {
      yield* source();
    } finally {
      await _realtime.release();
    }
  }
}
