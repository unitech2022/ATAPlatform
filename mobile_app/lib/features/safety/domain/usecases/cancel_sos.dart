import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class CancelSosParams extends Equatable {
  const CancelSosParams({
    required this.caseId,
    this.reason = SosCancelReason.accidental,
  });

  final String caseId;
  final SosCancelReason reason;

  @override
  List<Object?> get props => <Object?>[caseId, reason];
}

/// `POST /safety/sos/{caseId}/cancel` ("pressed by mistake").
class CancelSos implements UseCase<SafetyCaseSummary, CancelSosParams> {
  const CancelSos(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, SafetyCaseSummary>> call(CancelSosParams params) =>
      _repository.cancelSos(params.caseId, params.reason);
}
