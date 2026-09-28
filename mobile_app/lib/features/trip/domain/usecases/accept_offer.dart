import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /driver/offers/{id}/accept`.
class AcceptOffer implements UseCase<Trip, String> {
  const AcceptOffer(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Trip>> call(String params) =>
      _repository.acceptOffer(params);
}
