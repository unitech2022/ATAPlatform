import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Input of [RequestPayout]: the amount and the limits it is checked
/// against before the call.
class PayoutRequestParams {
  const PayoutRequestParams({
    required this.amount,
    required this.minAmount,
    required this.available,
  });

  final double amount;
  final double minAmount;
  final double available;
}

/// Requests a bank transfer (`POST /driver/payouts`), rejecting amounts
/// below the minimum or above the available balance locally.
class RequestPayout implements UseCase<Payout, PayoutRequestParams> {
  const RequestPayout(this._repository);

  final DriverWalletRepository _repository;

  static const String belowMinimum = 'payout_below_minimum';
  static const String insufficientBalance = 'insufficient_balance';

  @override
  Future<Either<Failure, Payout>> call(PayoutRequestParams params) async {
    if (params.amount < params.minAmount) {
      return Left<Failure, Payout>(
        ServerFailure(
          code: belowMinimum,
          message: '',
          details: <String, dynamic>{'minAmount': params.minAmount},
        ),
      );
    }
    if (params.amount > params.available) {
      return const Left<Failure, Payout>(
        ServerFailure(code: insufficientBalance, message: ''),
      );
    }
    return _repository.requestPayout(params.amount);
  }
}
