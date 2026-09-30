import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of `SafetyCaseSummary`.
abstract final class SafetyCaseModel {
  static SafetyCaseSummary fromJson(Map<String, dynamic> json) =>
      SafetyCaseSummary(
        id:
            JsonReaders.optionalString(json, 'id') ??
            JsonReaders.string(json, 'caseId'),
        caseNumber: JsonReaders.string(json, 'caseNumber'),
        type: JsonReaders.optionalString(json, 'type') ?? 'sos',
        status: JsonReaders.optionalString(json, 'status') ?? 'open',
        priority: JsonReaders.optionalString(json, 'priority') ?? 'medium',
        tripId: JsonReaders.optionalString(json, 'tripId'),
        tripNumber: JsonReaders.optionalString(json, 'tripNumber'),
        openedAt: JsonReaders.date(json, 'openedAt'),
        resolvedAt: JsonReaders.date(json, 'resolvedAt'),
        supportTicketId: JsonReaders.optionalString(json, 'supportTicketId'),
        publicNotes: JsonReaders.objects(json, 'publicNotes')
            .map(
              (Map<String, dynamic> n) => SafetyCaseNote(
                body: JsonReaders.string(n, 'body'),
                createdAt: JsonReaders.date(n, 'createdAt'),
              ),
            )
            .toList(growable: false),
      );
}

/// JSON mapping of `SafetyAlert` (REST, hub and push payloads).
abstract final class SafetyAlertModel {
  static SafetyAlert fromJson(Map<String, dynamic> json) => SafetyAlert(
    id:
        JsonReaders.optionalString(json, 'id') ??
        JsonReaders.string(json, 'alertId'),
    tripId: JsonReaders.optionalString(json, 'tripId'),
    type:
        JsonReaders.optionalString(json, 'type') ??
        JsonReaders.optionalString(json, 'alertType') ??
        '',
    status:
        JsonReaders.optionalString(json, 'status') ?? SafetyAlert.pendingRider,
    detectedAt: JsonReaders.date(json, 'detectedAt'),
    respondBy: JsonReaders.date(json, 'respondBy'),
  );

  /// `safety.check` push data: `alertId` or the id at the end of
  /// `ata://safety/check/{alertId}`.
  static SafetyAlert? fromPush(Map<String, dynamic> data) {
    final String? id =
        data['alertId']?.toString() ??
        Uri.tryParse(
          data['deepLink']?.toString() ?? '',
        )?.pathSegments.lastOrNull;
    if (id == null || id.isEmpty) return null;
    return fromJson(<String, dynamic>{...data, 'id': id});
  }
}

/// JSON mapping of lost item reports.
abstract final class LostItemModel {
  static LostItemReport fromJson(Map<String, dynamic> json) => LostItemReport(
    id: JsonReaders.string(json, 'id'),
    reportNumber: JsonReaders.string(json, 'reportNumber'),
    tripId: JsonReaders.optionalString(json, 'tripId'),
    tripNumber: JsonReaders.string(json, 'tripNumber'),
    itemCategory: LostItemCategory.parse(
      JsonReaders.optionalString(json, 'itemCategory'),
    ),
    description: JsonReaders.string(json, 'description'),
    status: JsonReaders.optionalString(json, 'status') ?? 'open',
    driverResponse: JsonReaders.optionalString(json, 'driverResponse'),
    supportTicketId: JsonReaders.optionalString(json, 'supportTicketId'),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  static Map<String, dynamic> draftToJson(LostItemDraft d) => <String, dynamic>{
    'itemCategory': d.category.apiValue,
    'description': d.description.trim(),
    'contactPhone': ?d.contactPhone,
  };
}
