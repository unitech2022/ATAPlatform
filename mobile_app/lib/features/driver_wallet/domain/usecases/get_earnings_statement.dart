import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the earnings statement of a date range.
class GetEarningsStatement implements UseCase<EarningsStatement, DateRange> {
  const GetEarningsStatement(this._repository);

  final DriverWalletRepository _repository;

  @override
  Future<Either<Failure, EarningsStatement>> call(DateRange range) =>
      _repository.getStatement(range);
}
