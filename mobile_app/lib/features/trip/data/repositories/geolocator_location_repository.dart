import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';
import 'package:geolocator/geolocator.dart';

/// [LocationRepository] over `geolocator` with a 10 m distance filter.
class GeolocatorLocationRepository implements LocationRepository {
  const GeolocatorLocationRepository();

  static const int distanceFilterMeters = 10;

  @override
  Future<LocationAccess> requestAccess() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      return LocationAccess.serviceDisabled;
    }
    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    return switch (permission) {
      LocationPermission.always ||
      LocationPermission.whileInUse => LocationAccess.granted,
      LocationPermission.deniedForever => LocationAccess.deniedForever,
      LocationPermission.denied ||
      LocationPermission.unableToDetermine => LocationAccess.denied,
    };
  }

  @override
  Stream<DriverPosition> positions() =>
      Geolocator.getPositionStream(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          distanceFilter: distanceFilterMeters,
        ),
      ).map(
        (Position position) => DriverPosition(
          point: GeoPoint(lat: position.latitude, lng: position.longitude),
          heading: position.heading,
          speed: position.speed,
          accuracy: position.accuracy,
          timestamp: position.timestamp,
        ),
      );
}
