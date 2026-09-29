import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// Trip ratings (F15).
abstract interface class RatingRepository {
  Future<Either<Failure, List<RatingTag>>> getTags(RatingTargetRole target);

  Future<Either<Failure, SubmittedRating>> submit(RatingDraft draft);

  Future<Either<Failure, List<PendingRating>>> getPending(TripActor role);

  Future<Either<Failure, RatingSummary>> getSummary(TripActor role);
}
