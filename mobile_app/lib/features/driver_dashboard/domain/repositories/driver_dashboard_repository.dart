import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:fpdart/fpdart.dart';

/// `/driver/status`, `/driver/earnings/summary`, `/driver/trips`.
abstract interface class DriverDashboardRepository {
  Future<Either<Failure, DriverStatus>> getStatus();
  Future<Either<Failure, DriverStatus>> setOnline({required bool isOnline});
  Future<Either<Failure, EarningsSummary>> getEarningsSummary();
  Future<Either<Failure, PageResult<TripSummary>>> getTrips({int page = 1});
}
