import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// The driver's active or past reservations.
class GetMyReservations
    implements UseCase<PageResult<Reservation>, ReservationsQuery> {
  const GetMyReservations(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, PageResult<Reservation>>> call(
    ReservationsQuery params,
  ) => _repository.getReservations(params);
}
