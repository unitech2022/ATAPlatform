import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// The rider's open scheduled bookings, soonest first.
class GetScheduledTrips implements UseCase<List<ScheduledTrip>, NoParams> {
  const GetScheduledTrips(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, List<ScheduledTrip>>> call(NoParams params) =>
      _repository.getScheduledTrips();
}
