import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:fpdart/fpdart.dart';

/// Rider promo codes (F15).
abstract interface class PromotionsRepository {
  Future<Either<Failure, List<Promotion>>> getPromotions(
    PromotionStatus status,
  );

  Future<Either<Failure, PromoValidation>> validate(
    PromoValidationParams params,
  );
}
