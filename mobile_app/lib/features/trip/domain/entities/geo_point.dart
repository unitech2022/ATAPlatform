import 'package:equatable/equatable.dart';

/// A WGS-84 coordinate.
class GeoPoint extends Equatable {
  const GeoPoint({required this.lat, required this.lng});

  final double lat;
  final double lng;

  /// Riyadh city centre, used as the default pickup and by the simulator.
  static const GeoPoint riyadh = GeoPoint(lat: 24.7136, lng: 46.6753);

  @override
  List<Object?> get props => <Object?>[lat, lng];
}
