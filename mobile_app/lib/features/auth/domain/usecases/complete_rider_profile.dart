import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Saves the rider's name and terms acceptance (`PATCH /me`).
class CompleteRiderProfile implements UseCase<User, String> {
  const CompleteRiderProfile(this._repository);

  final AuthRepository _repository;

  @override
  Future<Either<Failure, User>> call(String fullName) =>
      _repository.completeRiderProfile(fullName: fullName);
}
