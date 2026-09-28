import 'package:ata_app/features/trip/domain/entities/driver_location.dart';

/// Device positioning (geolocator or the simulator).
abstract interface class LocationRepository {
  /// Checks and requests the foreground location permission.
  Future<LocationAccess> requestAccess();

  /// Continuous positions, already filtered by distance.
  Stream<DriverPosition> positions();
}
