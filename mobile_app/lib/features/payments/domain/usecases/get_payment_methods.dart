import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the saved cards.
class GetPaymentMethods implements UseCase<List<SavedCard>, NoParams> {
  const GetPaymentMethods(this._repository);

  final PaymentsRepository _repository;

  @override
  Future<Either<Failure, List<SavedCard>>> call(NoParams params) =>
      _repository.getPaymentMethods();
}
