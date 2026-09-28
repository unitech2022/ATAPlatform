import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /safety/alerts/pending`.
class GetPendingSafetyAlert implements UseCase<SafetyAlert?, NoParams> {
  const GetPendingSafetyAlert(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, SafetyAlert?>> call(NoParams params) =>
      _repository.getPendingAlert();
}
