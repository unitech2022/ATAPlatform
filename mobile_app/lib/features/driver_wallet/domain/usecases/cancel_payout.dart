import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Cancels a payout that is still `requested`.
class CancelPayout implements UseCase<Payout, String> {
  const CancelPayout(this._repository);

  final DriverWalletRepository _repository;

  @override
  Future<Either<Failure, Payout>> call(String id) =>
      _repository.cancelPayout(id);
}
