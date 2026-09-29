import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Tag catalog for the rated role (`GET /catalog/rating-tags?target=`).
class GetRatingTags implements UseCase<List<RatingTag>, RatingTargetRole> {
  const GetRatingTags(this._repository);

  final RatingRepository _repository;

  @override
  Future<Either<Failure, List<RatingTag>>> call(RatingTargetRole target) =>
      _repository.getTags(target);
}
