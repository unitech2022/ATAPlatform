import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /passenger|driver/trips/{id}/messages`.
class GetTripMessages implements UseCase<List<TripMessage>, ChatTarget> {
  const GetTripMessages(this._repository);

  final TripChatRepository _repository;

  @override
  Future<Either<Failure, List<TripMessage>>> call(ChatTarget params) =>
      _repository.getMessages(params);
}
