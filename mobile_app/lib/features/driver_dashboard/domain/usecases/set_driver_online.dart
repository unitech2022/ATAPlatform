import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Toggles availability (`PUT /driver/status`).
class SetDriverOnline implements UseCase<DriverStatus, bool> {
  const SetDriverOnline(this._repository);

  final DriverDashboardRepository _repository;

  @override
  Future<Either<Failure, DriverStatus>> call(bool isOnline) =>
      _repository.setOnline(isOnline: isOnline);
}
