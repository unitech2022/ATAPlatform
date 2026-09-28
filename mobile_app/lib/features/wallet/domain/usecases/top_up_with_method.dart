import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_wallet.dart';
import 'package:fpdart/fpdart.dart';

/// Tops up from a saved card (or the sandbox) for the passenger or driver
/// wallet. A card top-up needs the card id.
class TopUpWithMethod implements UseCase<TopUpResult, TopUpParams> {
  const TopUpWithMethod(this._repository);

  final WalletRepository _repository;

  static const String _validation = 'validation_failed';

  @override
  Future<Either<Failure, TopUpResult>> call(TopUpParams params) {
    final bool outOfRange =
        params.amount < TopUpWallet.minAmount ||
        params.amount > TopUpWallet.maxAmount;
    final bool missingCard =
        params.method == TopUpMethod.card && params.paymentMethodId == null;
    if (outOfRange || missingCard) {
      return Future<Either<Failure, TopUpResult>>.value(
        const Left<Failure, TopUpResult>(
          ServerFailure(code: _validation, message: ''),
        ),
      );
    }
    return _repository.topUpWith(params);
  }
}
