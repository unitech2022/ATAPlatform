import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/pricing/data/models/demand_model.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';

/// REST endpoints of F10: `/pricing/quote` and `/pricing/demand`.
class PricingRemoteDataSource {
  const PricingRemoteDataSource(this._api);

  final ApiClient _api;

  static const String quotePath = '/pricing/quote';
  static const String demandPath = '/pricing/demand';

  Future<QuoteModel> quote(Map<String, dynamic> body) async =>
      QuoteModel.fromJson(
        await _api.post(quotePath, body: body) as Map<String, dynamic>,
      );

  Future<DemandModel> demand({required double lat, required double lng}) async {
    final Object? body = await _api.get(
      demandPath,
      query: <String, dynamic>{'lat': lat, 'lng': lng},
    );
    return DemandModel.fromJson(
      body is Map<String, dynamic> ? body : const <String, dynamic>{},
    );
  }
}
