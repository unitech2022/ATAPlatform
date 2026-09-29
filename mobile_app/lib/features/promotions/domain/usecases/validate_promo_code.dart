import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Checks a code for the current trip draft. The code is normalized to
/// uppercase without spaces; one that cannot exist (4–20 Latin letters and
/// digits) fails locally with `promo_not_found`.
class ValidatePromoCode
    implements UseCase<PromoValidation, PromoValidationParams> {
  const ValidatePromoCode(this._repository);

  final PromotionsRepository _repository;

  static const String notFound = 'promo_not_found';
  static final RegExp _format = RegExp(r'^[A-Z0-9]{4,20}$');

  /// `" ata 10 "` → `ATA10`.
  static String normalize(String code) =>
      code.replaceAll(RegExp(r'\s+'), '').toUpperCase();

  static bool isWellFormed(String code) => _format.hasMatch(normalize(code));

  @override
  Future<Either<Failure, PromoValidation>> call(PromoValidationParams params) {
    final String code = normalize(params.code);
    if (!_format.hasMatch(code)) {
      return Future<Either<Failure, PromoValidation>>.value(
        const Left<Failure, PromoValidation>(
          ServerFailure(code: notFound, message: ''),
        ),
      );
    }
    return _repository.validate(params.withCode(code));
  }
}
