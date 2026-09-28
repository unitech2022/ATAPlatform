import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /safety/trusted-contacts`.
class GetTrustedContacts implements UseCase<List<TrustedContact>, NoParams> {
  const GetTrustedContacts(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, List<TrustedContact>>> call(NoParams params) =>
      _repository.getTrustedContacts();
}
