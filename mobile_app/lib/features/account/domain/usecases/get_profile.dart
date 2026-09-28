import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads `GET /me`.
class GetProfile implements UseCase<Profile, NoParams> {
  const GetProfile(this._repository);

  final AccountRepository _repository;

  @override
  Future<Either<Failure, Profile>> call(NoParams params) =>
      _repository.getProfile();
}
