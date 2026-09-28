import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Reads the online flag.
class GetDriverStatus implements UseCase<DriverStatus, NoParams> {
  const GetDriverStatus(this._repository);

  final DriverDashboardRepository _repository;

  @override
  Future<Either<Failure, DriverStatus>> call(NoParams params) =>
      _repository.getStatus();
}
