import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';

/// Live queue position (`AirportQueueUpdated`).
class WatchAirportQueue {
  const WatchAirportQueue(this._repository);

  final AirportRepository _repository;

  Stream<AirportQueuePosition> call() => _repository.watchQueue();
}
