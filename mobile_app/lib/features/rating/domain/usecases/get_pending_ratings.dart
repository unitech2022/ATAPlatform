import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// Recent completed trips the user has not rated yet.
class GetPendingRatings implements UseCase<List<PendingRating>, TripActor> {
  const GetPendingRatings(this._repository);

  final RatingRepository _repository;

  @override
  Future<Either<Failure, List<PendingRating>>> call(TripActor role) =>
      _repository.getPending(role);
}
