import 'package:ata_app/core/errors/app_exception.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of the F15 promotion payloads (`docs/10` §F15.6).
abstract final class PromotionModels {
  static const String notEligible = 'promo_not_eligible';

  static Promotion promotion(Map<String, dynamic> json) => Promotion(
    code: JsonReaders.string(json, 'code'),
    name: JsonReaders.string(json, 'name'),
    description: JsonReaders.string(json, 'description'),
    type: PromotionType.parse(JsonReaders.optionalString(json, 'type')),
    value: JsonReaders.number(json, 'value'),
    maxDiscount: JsonReaders.optionalNumber(json, 'maxDiscount'),
    minFare: JsonReaders.optionalNumber(json, 'minFare'),
    validTo: JsonReaders.date(json, 'validTo'),
    firstTripOnly: json['firstTripOnly'] == true,
    rideCategoryCodes: _strings(json['rideCategoryCodes']),
    paymentMethods: _strings(json['paymentMethods']),
    status: PromotionStatus.parse(JsonReaders.optionalString(json, 'status')),
  );

  /// A `200` with `valid: false` is turned into the matching error.
  static PromoValidation validation(Map<String, dynamic> json) {
    if (json['valid'] == false) {
      final String reason =
          JsonReaders.optionalString(json, 'reason') ?? notEligible;
      throw AppException(
        code: reason.startsWith('promo_') ? reason : notEligible,
        message: '',
        details: <String, dynamic>{'reason': reason},
      );
    }
    final Map<String, dynamic> promo =
        JsonReaders.object(json, 'promotion') ?? const <String, dynamic>{};
    return PromoValidation(
      code: JsonReaders.string(promo, 'code'),
      name: JsonReaders.string(promo, 'name'),
      type: PromotionType.parse(JsonReaders.optionalString(promo, 'type')),
      value: JsonReaders.number(promo, 'value'),
      maxDiscount: JsonReaders.optionalNumber(promo, 'maxDiscount'),
      isStackable: promo['isStackable'] == true,
      discountAmount: JsonReaders.optionalNumber(json, 'discountAmount'),
      totalBefore: JsonReaders.optionalNumber(json, 'totalBefore'),
      totalAfter: JsonReaders.optionalNumber(json, 'totalAfter'),
    );
  }

  static Map<String, dynamic> validationBody(PromoValidationParams p) =>
      <String, dynamic>{
        'code': p.code,
        'quoteId': ?p.quoteId,
        'rideCategoryId': ?p.rideCategoryId,
        'paymentMethod': ?p.paymentMethod,
        'bookingType': ?p.bookingType,
      };

  static List<String>? _strings(Object? value) => value is List<dynamic>
      ? value.map((Object? e) => e.toString()).toList(growable: false)
      : null;
}
