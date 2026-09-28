import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /safety/sos` (a repeat within 10 min returns the same case).
class TriggerSos implements UseCase<SosResult, SosRequest> {
  const TriggerSos(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, SosResult>> call(SosRequest params) =>
      _repository.triggerSos(params);
}
