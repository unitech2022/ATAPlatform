import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/data/datasources/driver_rewards_remote_data_source.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [DriverRewardsRepository] backed by the API.
class DriverRewardsRepositoryImpl implements DriverRewardsRepository {
  const DriverRewardsRepositoryImpl(this._remote);

  final DriverRewardsRemoteDataSource _remote;

  @override
  Future<Either<Failure, DriverTierInfo>> getTier() => guard(_remote.tier);

  @override
  Future<Either<Failure, List<Incentive>>> getIncentives(IncentiveTab tab) =>
      guard(() => _remote.incentives(tab));

  @override
  Future<Either<Failure, Incentive>> getIncentive(String id) =>
      guard(() => _remote.incentive(id));

  @override
  Future<Either<Failure, Unit>> optIn(String id) => guard(() async {
    await _remote.optIn(id);
    return unit;
  });
}
