import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

const Payout testPayout = Payout(
  id: 'po1',
  payoutNumber: 'PO-20260928-00012',
  amount: 500,
  status: PayoutStatus.requested,
  ibanMasked: 'SA03 **** **** 1234',
);

/// In-memory driver wallet with a cash debt.
class FakeDriverWalletRepository implements DriverWalletRepository {
  PayoutSummary summary = const PayoutSummary(
    balance: 640,
    cashDebt: 320,
    availableForPayout: 640,
    ibanMasked: 'SA03 **** **** 1234',
    canRequest: true,
  );

  @override
  Future<Either<Failure, Payout>> cancelPayout(String id) async =>
      const Right<Failure, Payout>(testPayout);

  @override
  Future<Either<Failure, PayoutSummary>> getPayoutSummary() async =>
      Right<Failure, PayoutSummary>(summary);

  @override
  Future<Either<Failure, PageResult<Payout>>> getPayouts({
    int page = 1,
  }) async => const Right<Failure, PageResult<Payout>>(
    PageResult<Payout>(
      items: <Payout>[testPayout],
      page: 1,
      pageSize: 20,
      total: 1,
    ),
  );

  @override
  Future<Either<Failure, EarningsStatement>> getStatement(
    DateRange range,
  ) async => Right<Failure, EarningsStatement>(
    EarningsStatement(
      from: range.from,
      to: range.to,
      totals: const StatementTotals(
        trips: 12,
        grossFares: 600,
        commission: 120,
        earnings: 480,
        cashCollected: 200,
        net: 280,
      ),
      days: <StatementDay>[
        StatementDay(date: range.to, trips: 3, earnings: 90),
      ],
    ),
  );

  @override
  Future<Either<Failure, Payout>> requestPayout(double amount) async =>
      const Right<Failure, Payout>(testPayout);
}
