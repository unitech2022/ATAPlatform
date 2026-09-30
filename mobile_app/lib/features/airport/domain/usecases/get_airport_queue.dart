import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:fpdart/fpdart.dart';

/// The driver's airport queue status.
class GetAirportQueue implements UseCase<AirportQueueStatus, NoParams> {
  const GetAirportQueue(this._repository);

  final AirportRepository _repository;

  @override
  Future<Either<Failure, AirportQueueStatus>> call(NoParams params) =>
      _repository.getQueue();
}
