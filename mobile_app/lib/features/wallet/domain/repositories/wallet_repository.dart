import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:fpdart/fpdart.dart';

/// `/wallet` endpoints.
abstract interface class WalletRepository {
  Future<Either<Failure, WalletSummary>> getWallet();
  Future<Either<Failure, PageResult<WalletTransaction>>> getTransactions({
    int page = 1,
  });

  /// Sandbox top-up; the repository generates the `Idempotency-Key`.
  Future<Either<Failure, TopUpResult>> topUp({required double amount});

  /// Top-up from a saved card or the sandbox, for either wallet.
  Future<Either<Failure, TopUpResult>> topUpWith(TopUpParams params);
}
