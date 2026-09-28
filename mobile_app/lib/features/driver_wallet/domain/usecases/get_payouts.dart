import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads a page of the payout history.
class GetPayouts implements UseCase<PageResult<Payout>, int> {
  const GetPayouts(this._repository);

  final DriverWalletRepository _repository;

  @override
  Future<Either<Failure, PageResult<Payout>>> call(int page) =>
      _repository.getPayouts(page: page);
}
