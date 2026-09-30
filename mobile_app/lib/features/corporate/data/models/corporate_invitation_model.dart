import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for `GET /passenger/corporate/invitations`.
abstract final class CorporateInvitationModel {
  static CorporateInvitation fromJson(Map<String, dynamic> json) =>
      CorporateInvitation(
        id: JsonReaders.string(json, 'id'),
        companyName: JsonReaders.string(json, 'companyName'),
        role: CorporateRole.parse(JsonReaders.optionalString(json, 'role')),
        expiresAt: JsonReaders.date(json, 'expiresAt'),
      );

  /// Accepts the documented bare array or an `{ items, page, … }` envelope.
  static List<CorporateInvitation> listFrom(Object? body) =>
      PageResult<CorporateInvitation>.fromAny(body, fromJson).items;
}
