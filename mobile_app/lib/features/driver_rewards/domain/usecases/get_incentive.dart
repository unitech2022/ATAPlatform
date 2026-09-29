import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:fpdart/fpdart.dart';

/// One quest with its zones.
class GetIncentive implements UseCase<Incentive, String> {
  const GetIncentive(this._repository);

  final DriverRewardsRepository _repository;

  @override
  Future<Either<Failure, Incentive>> call(String params) =>
      _repository.getIncentive(params);
}
