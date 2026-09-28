import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `DELETE /safety/trusted-contacts/{id}`.
class DeleteTrustedContact implements UseCase<Unit, String> {
  const DeleteTrustedContact(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String params) =>
      _repository.deleteTrustedContact(params);
}
