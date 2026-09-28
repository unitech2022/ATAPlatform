import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/rides/data/models/trip_summary_model.dart';

/// `/passenger/trips`.
class RidesRemoteDataSource {
  const RidesRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _tripsPath = '/passenger/trips';

  Future<PageResult<TripSummaryModel>> trips({
    required String status,
    required int page,
  }) async => PageResult<TripSummaryModel>.fromJson(
    await _api.get(
          _tripsPath,
          query: <String, dynamic>{'status': status, 'page': page},
        )
        as Map<String, dynamic>,
    TripSummaryModel.fromJson,
  );
}
