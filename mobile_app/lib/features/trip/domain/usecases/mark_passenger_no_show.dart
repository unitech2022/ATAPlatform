import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class NoShowParams extends Equatable {
  const NoShowParams({required this.tripId, this.at});

  final String tripId;
  final GeoPoint? at;

  @override
  List<Object?> get props => <Object?>[tripId, at];
}

/// `POST /driver/trips/{id}/no-show`; `422 no_show_too_early` before the
/// policy wait (`details.secondsRemaining`).
class MarkPassengerNoShow implements UseCase<Trip, NoShowParams> {
  const MarkPassengerNoShow(this._repository);

  final CancellationRepository _repository;

  @override
  Future<Either<Failure, Trip>> call(NoShowParams params) =>
      _repository.markNoShow(tripId: params.tripId, at: params.at);
}
