import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /safety/cases/{id}`.
class GetSafetyCase implements UseCase<SafetyCaseSummary, String> {
  const GetSafetyCase(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, SafetyCaseSummary>> call(String params) =>
      _repository.getCase(params);
}
