import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Upcoming scheduled requests around the driver.
class GetMarketplaceTrips
    implements UseCase<PageResult<MarketplaceTrip>, MarketplaceQuery> {
  const GetMarketplaceTrips(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, PageResult<MarketplaceTrip>>> call(
    MarketplaceQuery params,
  ) => _repository.getMarketplace(params);
}
