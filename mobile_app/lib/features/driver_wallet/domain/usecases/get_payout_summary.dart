import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the balance, cash debt, payout limits and IBAN.
class GetPayoutSummary implements UseCase<PayoutSummary, NoParams> {
  const GetPayoutSummary(this._repository);

  final DriverWalletRepository _repository;

  @override
  Future<Either<Failure, PayoutSummary>> call(NoParams params) =>
      _repository.getPayoutSummary();
}
