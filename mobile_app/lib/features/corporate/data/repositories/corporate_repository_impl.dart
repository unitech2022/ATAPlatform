import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/corporate/data/datasources/corporate_remote_data_source.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [CorporateRepository] backed by the API.
class CorporateRepositoryImpl implements CorporateRepository {
  const CorporateRepositoryImpl(this._remote);

  final CorporateRemoteDataSource _remote;

  /// Error code the API may answer instead of a `null` body.
  static const String notMemberCode = 'corporate_not_member';

  @override
  Future<Either<Failure, CorporateProfile?>> getProfile() async {
    final Either<Failure, CorporateProfile?> result = await guard(
      _remote.profile,
    );
    return result.fold(
      (Failure failure) => failure.code == notMemberCode
          ? const Right<Failure, CorporateProfile?>(null)
          : Left<Failure, CorporateProfile?>(failure),
      Right<Failure, CorporateProfile?>.new,
    );
  }

  @override
  Future<Either<Failure, List<CorporateInvitation>>> getInvitations() =>
      guard(_remote.invitations);

  @override
  Future<Either<Failure, Unit>> acceptInvitation(String invitationId) =>
      guard(() async {
        await _remote.accept(invitationId);
        return unit;
      });

  @override
  Future<Either<Failure, Unit>> declineInvitation(String invitationId) =>
      guard(() async {
        await _remote.decline(invitationId);
        return unit;
      });
}
