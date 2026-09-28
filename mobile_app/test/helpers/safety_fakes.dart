import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:fpdart/fpdart.dart';

import 'trip_fakes.dart';

Right<Failure, T> _ok<T>(T value) => Right<Failure, T>(value);

/// In-memory F12 repository (no network).
class FakeSafetyRepository implements SafetyRepository {
  final StreamController<SafetyAlert> checks =
      StreamController<SafetyAlert>.broadcast();
  final List<TrustedContact> contacts = <TrustedContact>[];

  @override
  Future<Either<Failure, List<TrustedContact>>> getTrustedContacts() async =>
      _ok(List<TrustedContact>.of(contacts));

  @override
  Future<Either<Failure, TrustedContact>> addTrustedContact(
    TrustedContactDraft d,
  ) async => _ok(
    TrustedContact(
      id: 'c${contacts.length}',
      name: d.name,
      phoneNumber: d.phoneNumber,
    ),
  );

  @override
  Future<Either<Failure, TrustedContact>> updateTrustedContact(
    String id,
    TrustedContactDraft d,
  ) async => _ok(
    TrustedContact(
      id: id,
      name: d.name,
      phoneNumber: d.phoneNumber,
      autoShare: d.autoShare,
    ),
  );

  @override
  Future<Either<Failure, Unit>> deleteTrustedContact(String id) async =>
      _ok(unit);

  @override
  Future<Either<Failure, List<TripShare>>> createTripShare({
    required String tripId,
    ShareChannel channel = ShareChannel.link,
    List<String> contactIds = const <String>[],
  }) async =>
      _ok(<TripShare>[const TripShare(id: 's1', url: 'https://ata.sa/t/abc')]);

  @override
  Future<Either<Failure, List<TripShare>>> getTripShares(String tripId) async =>
      _ok(const <TripShare>[]);

  @override
  Future<Either<Failure, Unit>> revokeTripShare(String shareId) async =>
      _ok(unit);

  @override
  Future<Either<Failure, SosResult>> triggerSos(SosRequest request) async =>
      _ok(const SosResult(caseId: 'k1', caseNumber: 'SC-1'));

  @override
  Future<Either<Failure, Unit>> sendSosLocation(
    String caseId,
    GeoPoint point, {
    double? accuracy,
  }) async => _ok(unit);

  @override
  Future<Either<Failure, SafetyCaseSummary>> cancelSos(
    String caseId,
    SosCancelReason reason,
  ) async => _ok(SafetyCaseSummary(id: caseId, caseNumber: 'SC-1'));

  @override
  Future<Either<Failure, SafetyCaseSummary>> submitReport(
    SafetyReportDraft draft,
  ) async => _ok(const SafetyCaseSummary(id: 'k2', caseNumber: 'SC-2'));

  @override
  Future<Either<Failure, PageResult<SafetyCaseSummary>>> getCases({
    int page = 1,
  }) async => _ok(const PageResult<SafetyCaseSummary>.empty());

  @override
  Future<Either<Failure, SafetyCaseSummary>> getCase(String id) async =>
      _ok(SafetyCaseSummary(id: id, caseNumber: 'SC-1'));

  @override
  Future<Either<Failure, SafetyAlert?>> getPendingAlert() async =>
      _ok<SafetyAlert?>(null);

  @override
  Future<Either<Failure, SafetyAlert>> respondToAlert(
    String alertId,
    SafetyCheckResponse response, {
    GeoPoint? at,
  }) async => _ok(SafetyAlert(id: alertId, status: 'resolved_ok'));

  @override
  Stream<SafetyAlert> watchSafetyChecks() => checks.stream;

  @override
  Future<Either<Failure, LostItemReport>> reportLostItem(
    LostItemDraft draft,
  ) async => _ok(const LostItemReport(id: 'l1', reportNumber: 'LI-1'));

  @override
  Future<Either<Failure, PageResult<LostItemReport>>> getMyLostItems({
    int page = 1,
  }) async => _ok(const PageResult<LostItemReport>.empty());

  @override
  Future<Either<Failure, PageResult<LostItemReport>>> getDriverLostItems({
    String? status,
    int page = 1,
  }) async => _ok(const PageResult<LostItemReport>.empty());

  @override
  Future<Either<Failure, Unit>> respondToLostItem(
    String id, {
    required bool found,
    String? note,
  }) async => _ok(unit);
}

/// In-memory chat repository.
class FakeTripChatRepository implements TripChatRepository {
  final StreamController<List<TripMessage>> feed =
      StreamController<List<TripMessage>>.broadcast();

  @override
  Future<Either<Failure, List<TripMessage>>> getMessages(
    ChatTarget target, {
    String? after,
  }) async => _ok(const <TripMessage>[]);

  @override
  Future<Either<Failure, TripMessage>> send(
    ChatTarget target, {
    String? body,
    String? quickReplyCode,
  }) async => _ok(
    TripMessage(
      id: 'm1',
      tripId: target.tripId,
      body: body ?? '',
      isMine: true,
    ),
  );

  @override
  Future<Either<Failure, Unit>> markRead(
    ChatTarget target,
    String upToId,
  ) async => _ok(unit);

  @override
  Future<Either<Failure, List<QuickReply>>> getQuickReplies(
    TripActor role,
  ) async => _ok(const <QuickReply>[]);

  @override
  Future<Either<Failure, MaskedCall>> requestCall(ChatTarget target) async =>
      _ok(const MaskedCall(mode: 'unavailable'));

  @override
  Stream<List<TripMessage>> watchMessages(ChatTarget target) => feed.stream;
}

const ReliabilitySummary testReliability = ReliabilitySummary(
  role: 'passenger',
  tripsAccepted: 22,
  tripsCompleted: 18,
  cancellationRate: 0.18,
  penaltyPoints: 6,
);

/// In-memory F14 repository.
class FakeCancellationRepository implements CancellationRepository {
  @override
  Future<Either<Failure, List<CancellationReason>>> getReasons({
    required TripActor actor,
    String? stage,
  }) async => _ok(const <CancellationReason>[
    CancellationReason(code: 'changed_mind', name: 'غيرت رأيي'),
  ]);

  @override
  Future<Either<Failure, CancelPreview>> preview({
    required String tripId,
    required TripActor actor,
    String? reasonCode,
  }) async => _ok(const CancelPreview(stage: 'after_accept'));

  @override
  Future<Either<Failure, Trip>> markNoShow({
    required String tripId,
    GeoPoint? at,
  }) async => _ok(tripAt(TripStage.cancelled));

  @override
  Future<Either<Failure, ReliabilitySummary>> getReliability(
    TripActor role,
  ) async => _ok(testReliability);
}
