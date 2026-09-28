import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /passenger/trips`.
class RequestTrip implements UseCase<Trip, TripRequest> {
  const RequestTrip(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Trip>> call(TripRequest params) =>
      _repository.requestTrip(params);
}
