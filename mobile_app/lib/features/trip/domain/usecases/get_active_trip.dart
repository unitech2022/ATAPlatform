import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /passenger/trips/active` or `GET /driver/trips/active`.
class GetActiveTrip implements UseCase<Trip?, TripActor> {
  const GetActiveTrip(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Trip?>> call(TripActor params) =>
      _repository.getActiveTrip(params);
}
