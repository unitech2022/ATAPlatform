import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/data/datasources/rating_remote_data_source.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// [RatingRepository] backed by the API.
class RatingRepositoryImpl implements RatingRepository {
  const RatingRepositoryImpl(this._remote);

  final RatingRemoteDataSource _remote;

  @override
  Future<Either<Failure, List<RatingTag>>> getTags(RatingTargetRole target) =>
      guard(() => _remote.tags(target));

  @override
  Future<Either<Failure, SubmittedRating>> submit(RatingDraft draft) =>
      guard(() => _remote.submit(draft));

  @override
  Future<Either<Failure, List<PendingRating>>> getPending(TripActor role) =>
      guard(() => _remote.pending(role));

  @override
  Future<Either<Failure, RatingSummary>> getSummary(TripActor role) =>
      guard(() => _remote.summary(role));
}
