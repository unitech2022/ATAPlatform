import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Booking window and cancellation terms (`params` = ride category id).
class GetSchedulingRules implements UseCase<SchedulingRules, String?> {
  const GetSchedulingRules(this._repository);

  final ScheduledRepository _repository;

  @override
  Future<Either<Failure, SchedulingRules>> call(String? params) =>
      _repository.getRules(rideCategoryId: params);
}
