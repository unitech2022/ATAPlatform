import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /passenger/corporate/invitations/{id}/accept`
/// (`invitation_expired`, `corporate_member_elsewhere`).
class AcceptCorporateInvitation implements UseCase<Unit, String> {
  const AcceptCorporateInvitation(this._repository);

  final CorporateRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String invitationId) =>
      _repository.acceptInvitation(invitationId);
}
