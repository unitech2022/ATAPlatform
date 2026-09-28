import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class MarkReadParams extends Equatable {
  const MarkReadParams({required this.target, required this.upToId});

  final ChatTarget target;
  final String upToId;

  @override
  List<Object?> get props => <Object?>[target, upToId];
}

/// `POST …/messages/read { upToId }` (also silences `trip.message` pushes).
class MarkMessagesRead implements UseCase<Unit, MarkReadParams> {
  const MarkMessagesRead(this._repository);

  final TripChatRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(MarkReadParams params) =>
      _repository.markRead(params.target, params.upToId);
}
