import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Registers this device (`PUT /me/devices`) with the optional push
/// subscription id. Delivery itself relies on External ID = user id.
class RegisterPushDevice implements UseCase<Unit, String?> {
  const RegisterPushDevice(this._repository);

  final AccountRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String? pushToken) =>
      _repository.registerDevice(pushToken: pushToken);
}
