import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_dashboard/data/datasources/driver_dashboard_remote_data_source.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:fpdart/fpdart.dart';

/// [DriverDashboardRepository] backed by the API.
class DriverDashboardRepositoryImpl implements DriverDashboardRepository {
  const DriverDashboardRepositoryImpl(this._remote);

  final DriverDashboardRemoteDataSource _remote;

  @override
  Future<Either<Failure, DriverStatus>> getStatus() => guard(_remote.status);

  @override
  Future<Either<Failure, DriverStatus>> setOnline({required bool isOnline}) =>
      guard(() => _remote.setOnline(isOnline));

  @override
  Future<Either<Failure, EarningsSummary>> getEarningsSummary() =>
      guard(_remote.earnings);

  @override
  Future<Either<Failure, PageResult<TripSummary>>> getTrips({int page = 1}) =>
      guard(() => _remote.trips(page: page));
}
