import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads today's and this week's numbers.
class GetEarningsSummary implements UseCase<EarningsSummary, NoParams> {
  const GetEarningsSummary(this._repository);

  final DriverDashboardRepository _repository;

  @override
  Future<Either<Failure, EarningsSummary>> call(NoParams params) =>
      _repository.getEarningsSummary();
}
