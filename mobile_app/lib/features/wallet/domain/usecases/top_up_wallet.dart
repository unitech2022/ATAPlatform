import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Adds funds to the wallet (sandbox in Step 1).
class TopUpWallet implements UseCase<TopUpResult, double> {
  const TopUpWallet(this._repository);

  final WalletRepository _repository;

  /// API limits.
  static const double minAmount = 10;
  static const double maxAmount = 5000;

  @override
  Future<Either<Failure, TopUpResult>> call(double amount) {
    if (amount < minAmount || amount > maxAmount) {
      return Future<Either<Failure, TopUpResult>>.value(
        const Left<Failure, TopUpResult>(
          ServerFailure(code: 'validation_failed', message: ''),
        ),
      );
    }
    return _repository.topUp(amount: amount);
  }
}
