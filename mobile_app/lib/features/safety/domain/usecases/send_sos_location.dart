import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class SosLocationParams extends Equatable {
  const SosLocationParams({
    required this.caseId,
    required this.point,
    this.accuracy,
  });

  final String caseId;
  final GeoPoint point;
  final double? accuracy;

  @override
  List<Object?> get props => <Object?>[caseId, point, accuracy];
}

/// `POST /safety/sos/{caseId}/location` (every 10 s while open).
class SendSosLocation implements UseCase<Unit, SosLocationParams> {
  const SendSosLocation(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(SosLocationParams params) => _repository
      .sendSosLocation(params.caseId, params.point, accuracy: params.accuracy);
}
