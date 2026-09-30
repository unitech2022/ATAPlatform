import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Runs the confirmation that is due (first or final).
class ConfirmReservation implements UseCase<ConfirmResult, String> {
  const ConfirmReservation(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, ConfirmResult>> call(String params) =>
      _repository.confirm(params);
}
