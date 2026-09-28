import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the first page of the rider's trips.
class GetPassengerTrips implements UseCase<PageResult<TripSummary>, NoParams> {
  const GetPassengerTrips(this._repository);

  final RidesRepository _repository;

  @override
  Future<Either<Failure, PageResult<TripSummary>>> call(NoParams params) =>
      _repository.getTrips();
}
