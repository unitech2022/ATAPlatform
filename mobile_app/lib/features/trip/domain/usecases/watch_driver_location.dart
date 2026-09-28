import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';

/// Live driver position for the passenger's trip (`DriverLocation` event).
class WatchDriverLocation {
  const WatchDriverLocation(this._repository);

  final TripRepository _repository;

  Stream<DriverLocationUpdate> call() => _repository.watchDriverLocation();
}
