import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `DELETE /safety/shares/{id}`.
class RevokeTripShare implements UseCase<Unit, String> {
  const RevokeTripShare(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String params) =>
      _repository.revokeTripShare(params);
}
