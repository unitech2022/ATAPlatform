import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class RespondToAlertParams extends Equatable {
  const RespondToAlertParams({
    required this.alertId,
    required this.response,
    this.at,
  });

  final String alertId;
  final SafetyCheckResponse response;
  final GeoPoint? at;

  @override
  List<Object?> get props => <Object?>[alertId, response, at];
}

/// `POST /safety/alerts/{id}/respond` (`409` when no longer pending).
class RespondToSafetyAlert
    implements UseCase<SafetyAlert, RespondToAlertParams> {
  const RespondToSafetyAlert(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, SafetyAlert>> call(RespondToAlertParams params) =>
      _repository.respondToAlert(
        params.alertId,
        params.response,
        at: params.at,
      );
}
