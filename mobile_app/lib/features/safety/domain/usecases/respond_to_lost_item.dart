import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class LostItemAnswer extends Equatable {
  const LostItemAnswer({required this.id, required this.found, this.note});

  final String id;
  final bool found;
  final String? note;

  @override
  List<Object?> get props => <Object?>[id, found, note];
}

/// `POST /driver/lost-items/{id}/respond` (`409` if already answered).
class RespondToLostItem implements UseCase<Unit, LostItemAnswer> {
  const RespondToLostItem(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(LostItemAnswer params) => _repository
      .respondToLostItem(params.id, found: params.found, note: params.note);
}
