import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';

/// Continuous device positions (geolocator or the simulator).
class WatchDevicePosition {
  const WatchDevicePosition(this._repository);

  final LocationRepository _repository;

  Stream<DriverPosition> call() => _repository.positions();
}
