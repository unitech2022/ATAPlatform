import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Current tier, metrics, next-tier thresholds and benefits.
class GetDriverTier implements UseCase<DriverTierInfo, NoParams> {
  const GetDriverTier(this._repository);

  final DriverRewardsRepository _repository;

  @override
  Future<Either<Failure, DriverTierInfo>> call(NoParams params) =>
      _repository.getTier();
}
