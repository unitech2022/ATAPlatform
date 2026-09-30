import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Gives a reservation back (late release costs points).
class ReleaseReservation implements UseCase<Reservation, ReleaseParams> {
  const ReleaseReservation(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, Reservation>> call(ReleaseParams params) =>
      _repository.release(params);
}
