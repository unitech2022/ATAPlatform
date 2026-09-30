import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// One upcoming scheduled request of `GET /driver/scheduled/marketplace`:
/// no passenger identity and only an approximate pickup until reserved.
class MarketplaceTrip extends Equatable {
  const MarketplaceTrip({
    required this.tripId,
    required this.scheduledAt,
    this.categoryCode = '',
    this.categoryName = '',
    this.pickupArea = '',
    this.dropoffArea = '',
    this.pickupApprox,
    this.distanceToPickupKm = 0,
    this.tripDistanceMeters = 0,
    this.estimatedFare = 0,
    this.driverNetEarnings = 0,
    this.isAirport = false,
    this.isFavoriteRequest = false,
    this.exclusiveUntil,
  });

  final String tripId;
  final DateTime scheduledAt;
  final String categoryCode;
  final String categoryName;
  final String pickupArea;
  final String dropoffArea;
  final GeoPoint? pickupApprox;
  final double distanceToPickupKm;
  final int tripDistanceMeters;
  final double estimatedFare;
  final double driverNetEarnings;
  final bool isAirport;

  /// A passenger asked for this driver first (favourite window).
  final bool isFavoriteRequest;
  final DateTime? exclusiveUntil;

  @override
  List<Object?> get props => <Object?>[
    tripId,
    scheduledAt,
    categoryCode,
    categoryName,
    pickupArea,
    dropoffArea,
    pickupApprox,
    distanceToPickupKm,
    tripDistanceMeters,
    estimatedFare,
    driverNetEarnings,
    isAirport,
    isFavoriteRequest,
    exclusiveUntil,
  ];
}
