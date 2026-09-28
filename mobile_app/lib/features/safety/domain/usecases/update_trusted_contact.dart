import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class UpdateTrustedContactParams extends Equatable {
  const UpdateTrustedContactParams({required this.id, required this.draft});

  final String id;
  final TrustedContactDraft draft;

  @override
  List<Object?> get props => <Object?>[id, draft];
}

/// `PUT /safety/trusted-contacts/{id}`.
class UpdateTrustedContact
    implements UseCase<TrustedContact, UpdateTrustedContactParams> {
  const UpdateTrustedContact(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, TrustedContact>> call(
    UpdateTrustedContactParams params,
  ) => _repository.updateTrustedContact(params.id, params.draft);
}
