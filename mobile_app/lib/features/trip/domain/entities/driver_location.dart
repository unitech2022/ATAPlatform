import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// A device position sent by the driver (`PUT /driver/location`).
class DriverPosition extends Equatable {
  const DriverPosition({
    required this.point,
    this.heading,
    this.speed,
    this.accuracy,
    this.timestamp,
  });

  final GeoPoint point;
  final double? heading;
  final double? speed;
  final double? accuracy;
  final DateTime? timestamp;

  @override
  List<Object?> get props => <Object?>[
    point,
    heading,
    speed,
    accuracy,
    timestamp,
  ];
}

/// `DriverLocation` hub event received by the passenger.
class DriverLocationUpdate extends Equatable {
  const DriverLocationUpdate({
    required this.tripId,
    required this.point,
    this.heading,
    this.etaSeconds,
  });

  final String tripId;
  final GeoPoint point;
  final double? heading;
  final int? etaSeconds;

  int? get etaMinutes =>
      etaSeconds == null ? null : (etaSeconds! / 60).ceil().clamp(0, 999);

  @override
  List<Object?> get props => <Object?>[tripId, point, heading, etaSeconds];
}

/// Result of asking for location permission.
enum LocationAccess { granted, denied, deniedForever, serviceDisabled }
