import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_wallet/data/datasources/driver_wallet_remote_data_source.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';
import 'package:uuid/uuid.dart';

/// [DriverWalletRepository] backed by the API.
class DriverWalletRepositoryImpl implements DriverWalletRepository {
  const DriverWalletRepositoryImpl(this._remote, {this._uuid = const Uuid()});

  final DriverWalletRemoteDataSource _remote;
  final Uuid _uuid;

  @override
  Future<Either<Failure, EarningsStatement>> getStatement(DateRange range) =>
      guard(() => _remote.statement(range));

  @override
  Future<Either<Failure, PayoutSummary>> getPayoutSummary() =>
      guard(_remote.summary);

  @override
  Future<Either<Failure, Payout>> requestPayout(double amount) => guard(
    () => _remote.requestPayout(amount: amount, idempotencyKey: _uuid.v4()),
  );

  @override
  Future<Either<Failure, PageResult<Payout>>> getPayouts({int page = 1}) =>
      guard(() => _remote.payouts(page: page));

  @override
  Future<Either<Failure, Payout>> cancelPayout(String id) =>
      guard(() => _remote.cancel(id));
}
