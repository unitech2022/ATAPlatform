import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /safety/trusted-contacts` (max 5, E.164 phone).
class AddTrustedContact
    implements UseCase<TrustedContact, TrustedContactDraft> {
  const AddTrustedContact(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, TrustedContact>> call(TrustedContactDraft params) =>
      _repository.addTrustedContact(params);
}
