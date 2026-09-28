import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Makes a card the default trip payment method.
class SetDefaultPaymentMethod implements UseCase<SavedCard, String> {
  const SetDefaultPaymentMethod(this._repository);

  final PaymentsRepository _repository;

  @override
  Future<Either<Failure, SavedCard>> call(String id) =>
      _repository.setDefault(id);
}
