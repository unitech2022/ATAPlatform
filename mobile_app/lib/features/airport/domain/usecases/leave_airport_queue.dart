import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Leaves the airport queue.
class LeaveAirportQueue implements UseCase<Unit, NoParams> {
  const LeaveAirportQueue(this._repository);

  final AirportRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(NoParams params) =>
      _repository.leaveQueue();
}
