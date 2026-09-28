import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/push/push_service.dart';
import 'package:ata_app/features/safety/data/datasources/safety_remote_data_source.dart';
import 'package:ata_app/features/safety/data/models/safety_case_models.dart';
import 'package:ata_app/features/safety/data/models/safety_models.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_watcher.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// [SafetyRepository] over the REST API, the trips hub and push.
class SafetyRepositoryImpl implements SafetyRepository {
  const SafetyRepositoryImpl({
    required this._remote,
    required this._realtime,
    required this._push,
  });

  static const String safetyCheckEvent = 'safety.check';

  final SafetyRemoteDataSource _remote;
  final TripRealtimeDataSource _realtime;
  final PushService _push;

  @override
  Future<Either<Failure, List<TrustedContact>>> getTrustedContacts() =>
      guard(_remote.contacts);

  @override
  Future<Either<Failure, TrustedContact>> addTrustedContact(
    TrustedContactDraft draft,
  ) => guard(() => _remote.addContact(TrustedContactModel.toJson(draft)));

  @override
  Future<Either<Failure, TrustedContact>> updateTrustedContact(
    String id,
    TrustedContactDraft draft,
  ) =>
      guard(() => _remote.updateContact(id, TrustedContactModel.toJson(draft)));

  @override
  Future<Either<Failure, Unit>> deleteTrustedContact(String id) =>
      guard(() async {
        await _remote.deleteContact(id);
        return unit;
      });

  @override
  Future<Either<Failure, List<TripShare>>> createTripShare({
    required String tripId,
    ShareChannel channel = ShareChannel.link,
    List<String> contactIds = const <String>[],
  }) => guard(
    () => _remote.createShare(tripId, <String, dynamic>{
      'channel': channel.apiValue,
      if (contactIds.isNotEmpty) 'contactIds': contactIds,
    }),
  );

  @override
  Future<Either<Failure, List<TripShare>>> getTripShares(String tripId) =>
      guard(() => _remote.shares(tripId));

  @override
  Future<Either<Failure, Unit>> revokeTripShare(String shareId) =>
      guard(() async {
        await _remote.revokeShare(shareId);
        return unit;
      });

  @override
  Future<Either<Failure, SosResult>> triggerSos(SosRequest request) =>
      guard(() => _remote.sos(SosModel.requestToJson(request)));

  @override
  Future<Either<Failure, Unit>> sendSosLocation(
    String caseId,
    GeoPoint point, {
    double? accuracy,
  }) => guard(() async {
    await _remote.sosLocation(caseId, <String, dynamic>{
      'lat': point.lat,
      'lng': point.lng,
      'accuracy': ?accuracy,
    });
    return unit;
  });

  @override
  Future<Either<Failure, SafetyCaseSummary>> cancelSos(
    String caseId,
    SosCancelReason reason,
  ) => guard(() => _remote.cancelSos(caseId, reason.apiValue));

  @override
  Future<Either<Failure, SafetyCaseSummary>> submitReport(
    SafetyReportDraft draft,
  ) => guard(
    () => _remote.report(<String, dynamic>{
      'tripId': draft.tripId,
      'category': draft.category.apiValue,
      'description': draft.description.trim(),
    }),
  );

  @override
  Future<Either<Failure, PageResult<SafetyCaseSummary>>> getCases({
    int page = 1,
  }) => guard(() => _remote.cases(page));

  @override
  Future<Either<Failure, SafetyCaseSummary>> getCase(String id) =>
      guard(() => _remote.safetyCase(id));

  @override
  Future<Either<Failure, SafetyAlert?>> getPendingAlert() =>
      guard(_remote.pendingAlert);

  @override
  Future<Either<Failure, SafetyAlert>> respondToAlert(
    String alertId,
    SafetyCheckResponse response, {
    GeoPoint? at,
  }) => guard(
    () => _remote.respondAlert(alertId, <String, dynamic>{
      'response': response.apiValue,
      'lat': ?at?.lat,
      'lng': ?at?.lng,
    }),
  );

  @override
  Stream<SafetyAlert> watchSafetyChecks() {
    final Stream<SafetyAlert> hub = _realtime.safetyCheck.map(
      SafetyAlertModel.fromJson,
    );
    final Stream<SafetyAlert> pushes = _push.received
        .where((PushEvent e) => e.eventCode == safetyCheckEvent)
        .map((PushEvent e) => SafetyAlertModel.fromPush(e.data))
        .where((SafetyAlert? a) => a != null)
        .cast<SafetyAlert>();
    late final StreamController<SafetyAlert> controller;
    StreamSubscription<SafetyAlert>? sub;
    controller = StreamController<SafetyAlert>(
      onListen: () async {
        sub = mergeStreams<SafetyAlert>(<Stream<SafetyAlert>>[
          hub,
          pushes,
        ]).listen(controller.add);
        await _realtime.acquire();
      },
      onCancel: () async {
        await sub?.cancel();
        await _realtime.release();
      },
    );
    return controller.stream;
  }

  @override
  Future<Either<Failure, LostItemReport>> reportLostItem(LostItemDraft draft) =>
      guard(
        () => _remote.reportLostItem(
          draft.tripId,
          LostItemModel.draftToJson(draft),
        ),
      );

  @override
  Future<Either<Failure, PageResult<LostItemReport>>> getMyLostItems({
    int page = 1,
  }) => guard(() => _remote.myLostItems(page));

  @override
  Future<Either<Failure, PageResult<LostItemReport>>> getDriverLostItems({
    String? status,
    int page = 1,
  }) => guard(() => _remote.driverLostItems(status, page));

  @override
  Future<Either<Failure, Unit>> respondToLostItem(
    String id, {
    required bool found,
    String? note,
  }) => guard(() async {
    await _remote.respondLostItem(id, <String, dynamic>{
      'found': found,
      'note': ?note,
    });
    return unit;
  });
}
