import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the itemised receipt of a completed trip.
class GetTripReceipt implements UseCase<Receipt, String> {
  const GetTripReceipt(this._repository);

  final PaymentsRepository _repository;

  @override
  Future<Either<Failure, Receipt>> call(String tripId) =>
      _repository.getTripReceipt(tripId);
}
