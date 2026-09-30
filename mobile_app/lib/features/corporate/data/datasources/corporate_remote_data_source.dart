import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/corporate/data/models/corporate_invitation_model.dart';
import 'package:ata_app/features/corporate/data/models/corporate_profile_model.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';

/// `/passenger/corporate` (`docs/12` §F19.4, employee side).
class CorporateRemoteDataSource {
  const CorporateRemoteDataSource(this._api);

  final ApiClient _api;

  static const String basePath = '/passenger/corporate';
  static const String invitationsPath = '$basePath/invitations';

  Future<CorporateProfile?> profile() async =>
      CorporateProfileModel.fromBody(await _api.get(basePath));

  Future<List<CorporateInvitation>> invitations() async =>
      CorporateInvitationModel.listFrom(await _api.get(invitationsPath));

  Future<void> accept(String id) => _api.post('$invitationsPath/$id/accept');

  Future<void> decline(String id) => _api.post('$invitationsPath/$id/decline');
}
