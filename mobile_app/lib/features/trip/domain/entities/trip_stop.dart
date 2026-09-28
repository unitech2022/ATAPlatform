import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// A named place on the route: pickup, dropoff or an extra stop.
class TripStop extends Equatable {
  const TripStop({
    required this.name,
    required this.address,
    required this.point,
    this.arrivedAt,
  });

  final String name;
  final String address;
  final GeoPoint point;
  final DateTime? arrivedAt;

  double get lat => point.lat;
  double get lng => point.lng;

  @override
  List<Object?> get props => <Object?>[name, address, point, arrivedAt];
}
