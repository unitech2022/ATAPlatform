import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads a page of wallet transactions.
class GetWalletTransactions
    implements UseCase<PageResult<WalletTransaction>, int> {
  const GetWalletTransactions(this._repository);

  final WalletRepository _repository;

  @override
  Future<Either<Failure, PageResult<WalletTransaction>>> call(int page) =>
      _repository.getTransactions(page: page);
}
