import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class VerifyPinParams extends Equatable {
  const VerifyPinParams({required this.tripId, required this.pin});

  final String tripId;
  final String pin;

  @override
  List<Object?> get props => <Object?>[tripId, pin];
}

/// `POST /driver/trips/{id}/verify-pin`.
class VerifyPin implements UseCase<Trip, VerifyPinParams> {
  const VerifyPin(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Trip>> call(VerifyPinParams params) =>
      _repository.verifyPin(tripId: params.tripId, pin: params.pin);
}
