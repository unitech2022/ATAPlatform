import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class SendMessageParams extends Equatable {
  const SendMessageParams({
    required this.target,
    this.body,
    this.quickReplyCode,
  });

  final ChatTarget target;
  final String? body;
  final String? quickReplyCode;

  @override
  List<Object?> get props => <Object?>[target, body, quickReplyCode];
}

/// `POST …/messages` (text ≤ 500 chars or a quick reply code).
class SendTripMessage implements UseCase<TripMessage, SendMessageParams> {
  const SendTripMessage(this._repository);

  final TripChatRepository _repository;

  @override
  Future<Either<Failure, TripMessage>> call(SendMessageParams params) =>
      _repository.send(
        params.target,
        body: params.body,
        quickReplyCode: params.quickReplyCode,
      );
}
