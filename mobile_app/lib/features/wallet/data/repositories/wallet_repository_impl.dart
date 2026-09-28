import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/wallet/data/datasources/wallet_remote_data_source.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:fpdart/fpdart.dart';
import 'package:uuid/uuid.dart';

/// [WalletRepository] backed by the API.
class WalletRepositoryImpl implements WalletRepository {
  const WalletRepositoryImpl(this._remote, {this._uuid = const Uuid()});

  final WalletRemoteDataSource _remote;
  final Uuid _uuid;

  @override
  Future<Either<Failure, WalletSummary>> getWallet() => guard(_remote.wallet);

  @override
  Future<Either<Failure, PageResult<WalletTransaction>>> getTransactions({
    int page = 1,
  }) => guard(() => _remote.transactions(page: page));

  @override
  Future<Either<Failure, TopUpResult>> topUp({required double amount}) =>
      topUpWith(TopUpParams(amount: amount));

  @override
  Future<Either<Failure, TopUpResult>> topUpWith(TopUpParams params) =>
      guard(() => _remote.topUp(params, idempotencyKey: _uuid.v4()));
}
