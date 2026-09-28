import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Deletes a saved card (`409 payment_method_in_use` during a trip).
class RemovePaymentMethod implements UseCase<Unit, String> {
  const RemovePaymentMethod(this._repository);

  final PaymentsRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String id) => _repository.remove(id);
}
