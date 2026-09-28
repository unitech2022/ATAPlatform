import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the driver's recent trips.
class GetDriverTrips implements UseCase<PageResult<TripSummary>, NoParams> {
  const GetDriverTrips(this._repository);

  final DriverDashboardRepository _repository;

  @override
  Future<Either<Failure, PageResult<TripSummary>>> call(NoParams params) =>
      _repository.getTrips();
}
