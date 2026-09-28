import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';

/// Hub `TripMessage` + polling fallback every 5 s.
class WatchTripMessages {
  const WatchTripMessages(this._repository);

  final TripChatRepository _repository;

  Stream<List<TripMessage>> call(ChatTarget target) =>
      _repository.watchMessages(target);
}
