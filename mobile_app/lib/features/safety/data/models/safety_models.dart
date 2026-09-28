import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// Tolerant list / page readers (bare arrays or `{ items }`).
abstract final class SafetyJson {
  static List<Map<String, dynamic>> list(Object? body, {String key = 'items'}) {
    final Object? items = body is Map<String, dynamic> ? body[key] : body;
    if (items is! List<dynamic>) return const <Map<String, dynamic>>[];
    return items.whereType<Map<String, dynamic>>().toList(growable: false);
  }

  static PageResult<T> page<T>(
    Object? body,
    T Function(Map<String, dynamic> json) parse,
  ) {
    if (body is Map<String, dynamic>) {
      return PageResult<T>.fromJson(body, parse);
    }
    final List<T> items = list(body).map(parse).toList(growable: false);
    return PageResult<T>(
      items: items,
      page: 1,
      pageSize: items.length,
      total: items.length,
    );
  }
}

/// JSON mapping of trusted contacts.
abstract final class TrustedContactModel {
  static TrustedContact fromJson(Map<String, dynamic> json) => TrustedContact(
    id: JsonReaders.string(json, 'id'),
    name: JsonReaders.string(json, 'name'),
    phoneNumber: JsonReaders.string(json, 'phoneNumber'),
    relationship: JsonReaders.optionalString(json, 'relationship'),
    autoShare: json['autoShare'] as bool? ?? false,
    notifyOnSos: json['notifyOnSos'] as bool? ?? true,
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  static Map<String, dynamic> toJson(TrustedContactDraft draft) =>
      <String, dynamic>{
        'name': draft.name.trim(),
        'phoneNumber': draft.phoneNumber,
        'relationship': ?(draft.relationship?.trim().isEmpty ?? true
            ? null
            : draft.relationship!.trim()),
        'autoShare': draft.autoShare,
        'notifyOnSos': draft.notifyOnSos,
      };
}

/// JSON mapping of trip shares (create and list shapes).
abstract final class TripShareModel {
  static TripShare fromJson(Map<String, dynamic> json) => TripShare(
    id: JsonReaders.string(json, 'id'),
    url: JsonReaders.string(json, 'url'),
    channel: JsonReaders.optionalString(json, 'channel') ?? 'link',
    trustedContactId: JsonReaders.optionalString(json, 'trustedContactId'),
    trustedContactName: JsonReaders.optionalString(json, 'trustedContactName'),
    viewCount: JsonReaders.integer(json, 'viewCount'),
    expiresAt: JsonReaders.date(json, 'expiresAt'),
    revokedAt: JsonReaders.date(json, 'revokedAt'),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );
}

/// JSON mapping of the SOS request / result.
abstract final class SosModel {
  static Map<String, dynamic> requestToJson(SosRequest r) => <String, dynamic>{
    'tripId': ?r.tripId,
    'role': ?r.role,
    'lat': r.point.lat,
    'lng': r.point.lng,
    'accuracy': ?r.accuracy,
    'note': ?r.note,
    'notifyTrustedContacts': r.notifyTrustedContacts,
  };

  static SosResult resultFromJson(Map<String, dynamic> json) => SosResult(
    caseId: JsonReaders.string(json, 'caseId'),
    caseNumber: JsonReaders.string(json, 'caseNumber'),
    status: JsonReaders.optionalString(json, 'status') ?? 'open',
    emergencyNumber:
        JsonReaders.optionalString(json, 'emergencyNumber') ??
        SosResult.defaultEmergencyNumber,
    contactsNotified: JsonReaders.integer(json, 'contactsNotified'),
  );
}
