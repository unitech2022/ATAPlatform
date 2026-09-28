import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class CancelTripParams extends Equatable {
  const CancelTripParams({
    required this.tripId,
    required this.actor,
    required this.reason,
    this.note,
  });

  final String tripId;
  final TripActor actor;
  final CancelReason reason;
  final String? note;

  @override
  List<Object?> get props => <Object?>[tripId, actor, reason, note];
}

/// `POST /passenger/trips/{id}/cancel` or `POST /driver/trips/{id}/cancel`.
class CancelTrip implements UseCase<Trip, CancelTripParams> {
  const CancelTrip(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Trip>> call(CancelTripParams params) =>
      _repository.cancelTrip(
        tripId: params.tripId,
        actor: params.actor,
        reason: params.reason,
        note: params.note,
      );
}
