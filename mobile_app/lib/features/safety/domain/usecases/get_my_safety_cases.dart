import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /safety/cases?page=` (cases the user opened).
class GetMySafetyCases implements UseCase<PageResult<SafetyCaseSummary>, int> {
  const GetMySafetyCases(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, PageResult<SafetyCaseSummary>>> call(int params) =>
      _repository.getCases(page: params);
}
