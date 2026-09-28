import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `DELETE /me`: deactivates the account and schedules deletion.
class DeleteAccount implements UseCase<Unit, NoParams> {
  const DeleteAccount(this._repository);

  final AccountRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(NoParams params) =>
      _repository.deleteAccount();
}
