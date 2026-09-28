import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/driver_dashboard/data/models/driver_status_model.dart';
import 'package:ata_app/features/driver_dashboard/data/models/earnings_summary_model.dart';
import 'package:ata_app/features/rides/data/models/trip_summary_model.dart';

/// `/driver/status`, `/driver/earnings/summary`, `/driver/trips`.
class DriverDashboardRemoteDataSource {
  const DriverDashboardRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _statusPath = '/driver/status';
  static const String _earningsPath = '/driver/earnings/summary';
  static const String _tripsPath = '/driver/trips';

  Future<DriverStatusModel> status() async => DriverStatusModel.fromJson(
    await _api.get(_statusPath) as Map<String, dynamic>,
  );

  Future<DriverStatusModel> setOnline(bool isOnline) async =>
      DriverStatusModel.fromJson(
        await _api.put(
              _statusPath,
              body: <String, dynamic>{'isOnline': isOnline},
            )
            as Map<String, dynamic>,
      );

  Future<EarningsSummaryModel> earnings() async =>
      EarningsSummaryModel.fromJson(
        await _api.get(_earningsPath) as Map<String, dynamic>,
      );

  Future<PageResult<TripSummaryModel>> trips({required int page}) async =>
      PageResult<TripSummaryModel>.fromJson(
        await _api.get(_tripsPath, query: <String, dynamic>{'page': page})
            as Map<String, dynamic>,
        TripSummaryModel.fromJson,
      );
}
