import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// Joins the airport queue from the driver position.
class JoinAirportQueue implements UseCase<AirportQueueStatus, GeoPoint> {
  const JoinAirportQueue(this._repository);

  final AirportRepository _repository;

  @override
  Future<Either<Failure, AirportQueueStatus>> call(GeoPoint params) =>
      _repository.joinQueue(params);
}
