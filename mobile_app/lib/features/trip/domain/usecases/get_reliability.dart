import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Reliability summary of the passenger or the driver.
class GetReliability implements UseCase<ReliabilitySummary, TripActor> {
  const GetReliability(this._repository);

  final CancellationRepository _repository;

  @override
  Future<Either<Failure, ReliabilitySummary>> call(TripActor role) =>
      _repository.getReliability(role);
}
