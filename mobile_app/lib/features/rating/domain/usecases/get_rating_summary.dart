import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// Average, distribution, top tags and anonymous comments of the user.
class GetRatingSummary implements UseCase<RatingSummary, TripActor> {
  const GetRatingSummary(this._repository);

  final RatingRepository _repository;

  @override
  Future<Either<Failure, RatingSummary>> call(TripActor role) =>
      _repository.getSummary(role);
}
