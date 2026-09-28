import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:fpdart/fpdart.dart';

/// Driver earnings statement and payouts (`docs/08` §F11.5 `/driver`).
abstract interface class DriverWalletRepository {
  Future<Either<Failure, EarningsStatement>> getStatement(DateRange range);
  Future<Either<Failure, PayoutSummary>> getPayoutSummary();

  /// The repository generates the `Idempotency-Key`.
  Future<Either<Failure, Payout>> requestPayout(double amount);
  Future<Either<Failure, PageResult<Payout>>> getPayouts({int page = 1});
  Future<Either<Failure, Payout>> cancelPayout(String id);
}
