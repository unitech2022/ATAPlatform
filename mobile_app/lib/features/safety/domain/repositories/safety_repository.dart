import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// `/safety/*`, lost items and the `SafetyCheck` feed (`docs/09` §F12.7).
abstract interface class SafetyRepository {
  // Trusted contacts
  Future<Either<Failure, List<TrustedContact>>> getTrustedContacts();
  Future<Either<Failure, TrustedContact>> addTrustedContact(
    TrustedContactDraft draft,
  );
  Future<Either<Failure, TrustedContact>> updateTrustedContact(
    String id,
    TrustedContactDraft draft,
  );
  Future<Either<Failure, Unit>> deleteTrustedContact(String id);

  // Trip shares
  Future<Either<Failure, List<TripShare>>> createTripShare({
    required String tripId,
    ShareChannel channel = ShareChannel.link,
    List<String> contactIds = const <String>[],
  });
  Future<Either<Failure, List<TripShare>>> getTripShares(String tripId);
  Future<Either<Failure, Unit>> revokeTripShare(String shareId);

  // SOS
  Future<Either<Failure, SosResult>> triggerSos(SosRequest request);
  Future<Either<Failure, Unit>> sendSosLocation(
    String caseId,
    GeoPoint point, {
    double? accuracy,
  });
  Future<Either<Failure, SafetyCaseSummary>> cancelSos(
    String caseId,
    SosCancelReason reason,
  );

  // Reports and cases
  Future<Either<Failure, SafetyCaseSummary>> submitReport(
    SafetyReportDraft draft,
  );
  Future<Either<Failure, PageResult<SafetyCaseSummary>>> getCases({
    int page = 1,
  });
  Future<Either<Failure, SafetyCaseSummary>> getCase(String id);

  // "Are you OK?"
  Future<Either<Failure, SafetyAlert?>> getPendingAlert();
  Future<Either<Failure, SafetyAlert>> respondToAlert(
    String alertId,
    SafetyCheckResponse response, {
    GeoPoint? at,
  });

  /// Hub `SafetyCheck` events merged with foreground `safety.check` pushes.
  Stream<SafetyAlert> watchSafetyChecks();

  // Lost items
  Future<Either<Failure, LostItemReport>> reportLostItem(LostItemDraft draft);
  Future<Either<Failure, PageResult<LostItemReport>>> getMyLostItems({
    int page = 1,
  });
  Future<Either<Failure, PageResult<LostItemReport>>> getDriverLostItems({
    String? status,
    int page = 1,
  });
  Future<Either<Failure, Unit>> respondToLostItem(
    String id, {
    required bool found,
    String? note,
  });
}
