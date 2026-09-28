import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';

/// Live feed of the active trip (hub events merged with polling).
class WatchActiveTrip {
  const WatchActiveTrip(this._repository);

  final TripRepository _repository;

  Stream<Trip?> call(TripActor actor) => _repository.watchActiveTrip(actor);
}
