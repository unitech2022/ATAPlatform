import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /catalog/chat-quick-replies?role=`.
class GetQuickReplies implements UseCase<List<QuickReply>, TripActor> {
  const GetQuickReplies(this._repository);

  final TripChatRepository _repository;

  @override
  Future<Either<Failure, List<QuickReply>>> call(TripActor params) =>
      _repository.getQuickReplies(params);
}
