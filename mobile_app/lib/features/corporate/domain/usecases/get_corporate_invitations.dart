import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /passenger/corporate/invitations`: the pending invitations that
/// match the rider's verified number.
class GetCorporateInvitations
    implements UseCase<List<CorporateInvitation>, NoParams> {
  const GetCorporateInvitations(this._repository);

  final CorporateRepository _repository;

  @override
  Future<Either<Failure, List<CorporateInvitation>>> call(NoParams params) =>
      _repository.getInvitations();
}
