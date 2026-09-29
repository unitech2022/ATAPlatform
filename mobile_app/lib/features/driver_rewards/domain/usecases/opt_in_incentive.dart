import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Joins a quest that requires opting in (`409 incentive_opt_in_closed`).
class OptInIncentive implements UseCase<Unit, String> {
  const OptInIncentive(this._repository);

  final DriverRewardsRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String params) =>
      _repository.optIn(params);
}
