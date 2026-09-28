import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/safety/data/models/safety_case_models.dart';
import 'package:ata_app/features/safety/data/models/safety_models.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';

/// REST endpoints of F12 (`docs/09` §F12.7).
class SafetyRemoteDataSource {
  const SafetyRemoteDataSource(this._api);

  final ApiClient _api;

  static const String contactsPath = '/safety/trusted-contacts';
  static const String casesPath = '/safety/cases';
  static const String sosPath = '/safety/sos';

  Future<List<TrustedContact>> contacts() async => SafetyJson.list(
    await _api.get(contactsPath),
  ).map(TrustedContactModel.fromJson).toList(growable: false);

  Future<TrustedContact> addContact(Map<String, dynamic> body) async =>
      TrustedContactModel.fromJson(
        await _api.post(contactsPath, body: body) as Map<String, dynamic>,
      );

  Future<TrustedContact> updateContact(
    String id,
    Map<String, dynamic> body,
  ) async => TrustedContactModel.fromJson(
    await _api.put('$contactsPath/$id', body: body) as Map<String, dynamic>,
  );

  Future<void> deleteContact(String id) => _api.delete('$contactsPath/$id');

  Future<List<TripShare>> createShare(
    String tripId,
    Map<String, dynamic> body,
  ) async => SafetyJson.list(
    await _api.post('/safety/trips/$tripId/shares', body: body),
    key: 'shares',
  ).map(TripShareModel.fromJson).toList(growable: false);

  Future<List<TripShare>> shares(String tripId) async => SafetyJson.list(
    await _api.get('/safety/trips/$tripId/shares'),
  ).map(TripShareModel.fromJson).toList(growable: false);

  Future<void> revokeShare(String id) => _api.delete('/safety/shares/$id');

  Future<SosResult> sos(Map<String, dynamic> body) async =>
      SosModel.resultFromJson(
        await _api.post(sosPath, body: body) as Map<String, dynamic>,
      );

  Future<void> sosLocation(String caseId, Map<String, dynamic> body) =>
      _api.post('$sosPath/$caseId/location', body: body);

  Future<SafetyCaseSummary> cancelSos(String caseId, String reason) async =>
      SafetyCaseModel.fromJson(
        await _api.post(
              '$sosPath/$caseId/cancel',
              body: <String, dynamic>{'reason': reason},
            )
            as Map<String, dynamic>,
      );

  Future<SafetyCaseSummary> report(Map<String, dynamic> body) async =>
      SafetyCaseModel.fromJson(
        await _api.post('/safety/reports', body: body) as Map<String, dynamic>,
      );

  Future<PageResult<SafetyCaseSummary>> cases(int page) async =>
      SafetyJson.page(
        await _api.get(casesPath, query: <String, dynamic>{'page': page}),
        SafetyCaseModel.fromJson,
      );

  Future<SafetyCaseSummary> safetyCase(String id) async =>
      SafetyCaseModel.fromJson(
        await _api.get('$casesPath/$id') as Map<String, dynamic>,
      );

  Future<SafetyAlert?> pendingAlert() async {
    final Object? body = await _api.get('/safety/alerts/pending');
    return body is Map<String, dynamic>
        ? SafetyAlertModel.fromJson(body)
        : null;
  }

  Future<SafetyAlert> respondAlert(
    String id,
    Map<String, dynamic> body,
  ) async => SafetyAlertModel.fromJson(
    await _api.post('/safety/alerts/$id/respond', body: body)
        as Map<String, dynamic>,
  );

  Future<LostItemReport> reportLostItem(
    String tripId,
    Map<String, dynamic> body,
  ) async => LostItemModel.fromJson(
    await _api.post('/passenger/trips/$tripId/lost-items', body: body)
        as Map<String, dynamic>,
  );

  Future<PageResult<LostItemReport>> myLostItems(int page) async =>
      SafetyJson.page(
        await _api.get(
          '/passenger/lost-items',
          query: <String, dynamic>{'page': page},
        ),
        LostItemModel.fromJson,
      );

  Future<PageResult<LostItemReport>> driverLostItems(
    String? status,
    int page,
  ) async => SafetyJson.page(
    await _api.get(
      '/driver/lost-items',
      query: <String, dynamic>{'status': ?status, 'page': page},
    ),
    LostItemModel.fromJson,
  );

  Future<void> respondLostItem(String id, Map<String, dynamic> body) =>
      _api.post('/driver/lost-items/$id/respond', body: body);
}
