import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:fpdart/fpdart.dart';

/// Driver tier and incentives (F15).
abstract interface class DriverRewardsRepository {
  Future<Either<Failure, DriverTierInfo>> getTier();

  Future<Either<Failure, List<Incentive>>> getIncentives(IncentiveTab tab);

  Future<Either<Failure, Incentive>> getIncentive(String id);

  Future<Either<Failure, Unit>> optIn(String id);
}
