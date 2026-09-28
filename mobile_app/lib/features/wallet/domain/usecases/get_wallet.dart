import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the balance and payment methods.
class GetWallet implements UseCase<WalletSummary, NoParams> {
  const GetWallet(this._repository);

  final WalletRepository _repository;

  @override
  Future<Either<Failure, WalletSummary>> call(NoParams params) =>
      _repository.getWallet();
}
