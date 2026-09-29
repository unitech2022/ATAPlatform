import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Quests of one tab (active / upcoming / completed).
class GetIncentives implements UseCase<List<Incentive>, IncentiveTab> {
  const GetIncentives(this._repository);

  final DriverRewardsRepository _repository;

  @override
  Future<Either<Failure, List<Incentive>>> call(IncentiveTab params) =>
      _repository.getIncentives(params);
}
