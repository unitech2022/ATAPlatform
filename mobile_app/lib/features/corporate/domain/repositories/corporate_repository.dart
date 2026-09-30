import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:fpdart/fpdart.dart';

/// The employee side of corporate accounts (F19, `docs/12` §F19.4).
abstract interface class CorporateRepository {
  /// `null` when the user is not a member of a company account.
  Future<Either<Failure, CorporateProfile?>> getProfile();

  Future<Either<Failure, List<CorporateInvitation>>> getInvitations();

  Future<Either<Failure, Unit>> acceptInvitation(String invitationId);

  Future<Either<Failure, Unit>> declineInvitation(String invitationId);
}
