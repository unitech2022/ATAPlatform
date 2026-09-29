import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/promotions/data/models/promotion_models.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';

/// `/passenger/promotions` and `/passenger/promotions/validate`.
class PromotionsRemoteDataSource {
  const PromotionsRemoteDataSource(this._api);

  final ApiClient _api;

  static const String listPath = '/passenger/promotions';
  static const String validatePath = '/passenger/promotions/validate';

  /// The documented list holds the available promotions; the used / expired
  /// tabs pass `?status=` and keep only rows whose `status` matches.
  Future<List<Promotion>> promotions(PromotionStatus status) async {
    final Object? body = await _api.get(
      listPath,
      query: status == PromotionStatus.available
          ? null
          : <String, dynamic>{'status': status.apiValue},
    );
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    if (items is! List<dynamic>) return const <Promotion>[];
    return items
        .whereType<Map<String, dynamic>>()
        .map(PromotionModels.promotion)
        .where((Promotion p) => p.status == status)
        .toList(growable: false);
  }

  Future<PromoValidation> validate(PromoValidationParams params) async =>
      PromotionModels.validation(
        await _api.post(
              validatePath,
              body: PromotionModels.validationBody(params),
            )
            as Map<String, dynamic>,
      );
}
