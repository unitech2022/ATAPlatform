import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/data/datasources/promotions_remote_data_source.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [PromotionsRepository] backed by the API.
class PromotionsRepositoryImpl implements PromotionsRepository {
  const PromotionsRepositoryImpl(this._remote);

  final PromotionsRemoteDataSource _remote;

  @override
  Future<Either<Failure, List<Promotion>>> getPromotions(
    PromotionStatus status,
  ) => guard(() => _remote.promotions(status));

  @override
  Future<Either<Failure, PromoValidation>> validate(
    PromoValidationParams params,
  ) => guard(() => _remote.validate(params));
}
