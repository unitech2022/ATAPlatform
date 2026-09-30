import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /passenger/corporate`: the employee profile, `null` for a rider
/// who is not a member of a company account.
class GetCorporateMembership implements UseCase<CorporateProfile?, NoParams> {
  const GetCorporateMembership(this._repository);

  final CorporateRepository _repository;

  @override
  Future<Either<Failure, CorporateProfile?>> call(NoParams params) =>
      _repository.getProfile();
}
