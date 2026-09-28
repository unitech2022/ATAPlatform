import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class AdvanceTripParams extends Equatable {
  const AdvanceTripParams({required this.tripId, required this.step, this.at});

  final String tripId;
  final TripStep step;

  /// Current device position (sent as `finalLat/finalLng` on complete).
  final GeoPoint? at;

  @override
  List<Object?> get props => <Object?>[tripId, step, at];
}

/// `POST /driver/trips/{id}/en-route|arrived|start|complete`.
class AdvanceTrip implements UseCase<Trip, AdvanceTripParams> {
  const AdvanceTrip(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Trip>> call(AdvanceTripParams params) => _repository
      .advance(tripId: params.tripId, step: params.step, at: params.at);
}
