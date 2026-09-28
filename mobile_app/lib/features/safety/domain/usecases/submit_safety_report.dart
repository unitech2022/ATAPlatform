import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /safety/reports` (within 7 days of the trip).
class SubmitSafetyReport
    implements UseCase<SafetyCaseSummary, SafetyReportDraft> {
  const SubmitSafetyReport(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, SafetyCaseSummary>> call(SafetyReportDraft params) =>
      _repository.submitReport(params);
}
