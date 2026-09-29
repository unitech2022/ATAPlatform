import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Promotions of one tab (available / used / expired).
class GetPromotions implements UseCase<List<Promotion>, PromotionStatus> {
  const GetPromotions(this._repository);

  final PromotionsRepository _repository;

  @override
  Future<Either<Failure, List<Promotion>>> call(PromotionStatus status) =>
      _repository.getPromotions(status);
}
