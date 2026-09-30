import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Reserves a marketplace trip (`params` = trip id).
class ReserveScheduledTrip implements UseCase<Reservation, String> {
  const ReserveScheduledTrip(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, Reservation>> call(String params) =>
      _repository.reserve(params);
}
