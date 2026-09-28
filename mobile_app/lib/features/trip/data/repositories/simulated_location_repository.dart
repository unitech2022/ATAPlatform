import 'dart:math';

import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';

/// Fake positions around Riyadh for emulators and tests
/// (`--dart-define=SIMULATE_LOCATION=true`).
class SimulatedLocationRepository implements LocationRepository {
  const SimulatedLocationRepository({
    this.origin = GeoPoint.riyadh,
    this.interval = const Duration(seconds: 3),
  });

  final GeoPoint origin;
  final Duration interval;

  /// ~11 m per tick, slowly circling the origin.
  static const double _stepDegrees = 0.0001;
  static const double _speedMetersPerSecond = 8;

  @override
  Future<LocationAccess> requestAccess() async => LocationAccess.granted;

  @override
  Stream<DriverPosition> positions() =>
      Stream<int>.periodic(interval, (int tick) => tick).map(_positionAt);

  DriverPosition _positionAt(int tick) {
    final double angle = tick * pi / 12;
    return DriverPosition(
      point: GeoPoint(
        lat: origin.lat + _stepDegrees * tick * cos(angle),
        lng: origin.lng + _stepDegrees * tick * sin(angle),
      ),
      heading: (angle * 180 / pi) % 360,
      speed: _speedMetersPerSecond,
      accuracy: 5,
      timestamp: DateTime.now(),
    );
  }
}
