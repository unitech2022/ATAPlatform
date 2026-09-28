import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST …/call`: never returns the other party's real number.
class RequestMaskedCall implements UseCase<MaskedCall, ChatTarget> {
  const RequestMaskedCall(this._repository);

  final TripChatRepository _repository;

  @override
  Future<Either<Failure, MaskedCall>> call(ChatTarget params) =>
      _repository.requestCall(params);
}
