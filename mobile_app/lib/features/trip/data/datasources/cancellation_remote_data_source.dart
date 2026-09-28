import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/trip/data/models/cancellation_models.dart';
import 'package:ata_app/features/trip/data/models/reliability_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';

/// REST endpoints of F14 (`docs/09` §F14.4).
class CancellationRemoteDataSource {
  const CancellationRemoteDataSource(this._api);

  final ApiClient _api;

  static const String reasonsPath = '/catalog/cancellation-reasons';
  static const String passengerPrefix = '/passenger';
  static const String driverPrefix = '/driver';

  Future<List<CancellationReason>> reasons({
    required String actor,
    String? stage,
  }) async => CancellationReasonModel.listFromJson(
    await _api.get(
      reasonsPath,
      query: <String, dynamic>{'actor': actor, 'stage': ?stage},
    ),
  );

  /// [prefix] is `/passenger` or `/driver`.
  Future<CancelPreview> preview(
    String prefix,
    String tripId, {
    String? reasonCode,
  }) async => CancelPreviewModel.fromJson(
    await _api.post(
          '$prefix/trips/$tripId/cancel/preview',
          body: <String, dynamic>{'reasonCode': ?reasonCode},
        )
        as Map<String, dynamic>,
  );

  Future<TripModel> noShow(String tripId, Map<String, dynamic> body) async =>
      TripModel.fromJson(
        await _api.post('$driverPrefix/trips/$tripId/no-show', body: body)
            as Map<String, dynamic>,
      );

  Future<ReliabilitySummary> reliability(String prefix) async =>
      ReliabilityModel.fromJson(
        await _api.get('$prefix/reliability') as Map<String, dynamic>,
      );
}
