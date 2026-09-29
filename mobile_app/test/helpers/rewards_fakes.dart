import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// In-memory F15 rating repository.
class FakeRatingRepository implements RatingRepository {
  List<PendingRating> pending = const <PendingRating>[];
  final List<RatingDraft> submitted = <RatingDraft>[];

  @override
  Future<Either<Failure, List<RatingTag>>> getTags(
    RatingTargetRole target,
  ) async => Right<Failure, List<RatingTag>>(RatingTag.defaultsFor(target));

  @override
  Future<Either<Failure, SubmittedRating>> submit(RatingDraft draft) async {
    submitted.add(draft);
    return Right<Failure, SubmittedRating>(
      SubmittedRating(id: 'r1', tripId: draft.tripId, stars: draft.stars),
    );
  }

  @override
  Future<Either<Failure, List<PendingRating>>> getPending(
    TripActor role,
  ) async => Right<Failure, List<PendingRating>>(pending);

  @override
  Future<Either<Failure, RatingSummary>> getSummary(TripActor role) async =>
      const Right<Failure, RatingSummary>(RatingSummary());
}

/// In-memory F15 promotions repository.
class FakePromotionsRepository implements PromotionsRepository {
  @override
  Future<Either<Failure, List<Promotion>>> getPromotions(
    PromotionStatus status,
  ) async => const Right<Failure, List<Promotion>>(<Promotion>[]);

  @override
  Future<Either<Failure, PromoValidation>> validate(
    PromoValidationParams params,
  ) async => Right<Failure, PromoValidation>(
    PromoValidation(code: params.code, discountAmount: 5),
  );
}

/// In-memory F15 tier / incentives repository.
class FakeDriverRewardsRepository implements DriverRewardsRepository {
  DriverTierInfo tier = const DriverTierInfo(tier: DriverTier.bronze);
  List<Incentive> incentives = const <Incentive>[];
  Failure? optInFailure;

  @override
  Future<Either<Failure, DriverTierInfo>> getTier() async =>
      Right<Failure, DriverTierInfo>(tier);

  @override
  Future<Either<Failure, List<Incentive>>> getIncentives(
    IncentiveTab tab,
  ) async => Right<Failure, List<Incentive>>(
    tab == IncentiveTab.active ? incentives : const <Incentive>[],
  );

  @override
  Future<Either<Failure, Incentive>> getIncentive(String id) async =>
      Right<Failure, Incentive>(
        incentives.firstWhere(
          (Incentive i) => i.id == id,
          orElse: () => Incentive(id: id, name: ''),
        ),
      );

  @override
  Future<Either<Failure, Unit>> optIn(String id) async {
    final Failure? failure = optInFailure;
    return failure == null
        ? const Right<Failure, Unit>(unit)
        : Left<Failure, Unit>(failure);
  }
}
