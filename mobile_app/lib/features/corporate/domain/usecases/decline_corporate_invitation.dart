import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /passenger/corporate/invitations/{id}/decline`.
class DeclineCorporateInvitation implements UseCase<Unit, String> {
  const DeclineCorporateInvitation(this._repository);

  final CorporateRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String invitationId) =>
      _repository.declineInvitation(invitationId);
}
