import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_driver_tier.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_incentives.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/opt_in_incentive.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/add_favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/get_available_favorites.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/get_favorite_drivers.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/remove_favorite_driver.dart';
import 'package:ata_app/features/promotions/domain/usecases/get_promotions.dart';
import 'package:ata_app/features/promotions/domain/usecases/validate_promo_code.dart';
import 'package:ata_app/features/rating/domain/usecases/get_pending_ratings.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_summary.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_tags.dart';
import 'package:ata_app/features/rating/domain/usecases/submit_rating.dart';

/// F15 use cases (ratings, promo codes, driver tier and incentives) and F16
/// favourite drivers.
void registerRewardsUseCases() {
  getIt
    // rating
    ..registerLazySingleton<GetRatingTags>(() => GetRatingTags(getIt()))
    ..registerLazySingleton<SubmitRating>(() => SubmitRating(getIt()))
    ..registerLazySingleton<GetPendingRatings>(() => GetPendingRatings(getIt()))
    ..registerLazySingleton<GetRatingSummary>(() => GetRatingSummary(getIt()))
    // promotions
    ..registerLazySingleton<GetPromotions>(() => GetPromotions(getIt()))
    ..registerLazySingleton<ValidatePromoCode>(() => ValidatePromoCode(getIt()))
    // favourite drivers
    ..registerLazySingleton<GetFavoriteDrivers>(
      () => GetFavoriteDrivers(getIt()),
    )
    ..registerLazySingleton<AddFavoriteDriver>(() => AddFavoriteDriver(getIt()))
    ..registerLazySingleton<RemoveFavoriteDriver>(
      () => RemoveFavoriteDriver(getIt()),
    )
    ..registerLazySingleton<GetAvailableFavorites>(
      () => GetAvailableFavorites(getIt()),
    )
    // driver rewards
    ..registerLazySingleton<GetDriverTier>(() => GetDriverTier(getIt()))
    ..registerLazySingleton<GetIncentives>(() => GetIncentives(getIt()))
    ..registerLazySingleton<GetIncentive>(() => GetIncentive(getIt()))
    ..registerLazySingleton<OptInIncentive>(() => OptInIncentive(getIt()));
}
