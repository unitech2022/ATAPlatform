import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Sends a rating once (no edits). Stars must be 1–5 and the comment at
/// most 500 characters; a blank comment is dropped.
class SubmitRating implements UseCase<SubmittedRating, RatingDraft> {
  const SubmitRating(this._repository);

  final RatingRepository _repository;

  static const String validationFailed = 'validation_failed';

  @override
  Future<Either<Failure, SubmittedRating>> call(RatingDraft draft) {
    final String comment = draft.comment?.trim() ?? '';
    final RatingDraft clean = RatingDraft(
      tripId: draft.tripId,
      rater: draft.rater,
      stars: draft.stars,
      tags: draft.tags.toSet().toList(growable: false),
      comment: comment.isEmpty ? null : comment,
    );
    if (!clean.hasValidStars) return _invalid('stars');
    if (!clean.hasValidComment) return _invalid('comment');
    return _repository.submit(clean);
  }

  static Future<Either<Failure, SubmittedRating>> _invalid(String field) =>
      Future<Either<Failure, SubmittedRating>>.value(
        Left<Failure, SubmittedRating>(
          ServerFailure(
            code: validationFailed,
            message: '',
            details: <String, dynamic>{field: 'invalid'},
          ),
        ),
      );
}
