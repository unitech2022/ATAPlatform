import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/rides/data/datasources/rides_remote_data_source.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [RidesRepository] backed by the API.
class RidesRepositoryImpl implements RidesRepository {
  const RidesRepositoryImpl(this._remote);

  final RidesRemoteDataSource _remote;

  @override
  Future<Either<Failure, PageResult<TripSummary>>> getTrips({
    String status = 'all',
    int page = 1,
  }) => guard(() => _remote.trips(status: status, page: page));
}
