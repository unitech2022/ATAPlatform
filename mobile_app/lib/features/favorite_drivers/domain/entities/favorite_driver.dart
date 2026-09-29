import 'package:equatable/equatable.dart';

/// Vehicle of a favourite driver (`{ make, model, color }`).
class FavoriteVehicle extends Equatable {
  const FavoriteVehicle({this.make = '', this.model = '', this.color = ''});

  final String make;
  final String model;
  final String color;

  bool get isEmpty => make.isEmpty && model.isEmpty && color.isEmpty;

  @override
  List<Object?> get props => <Object?>[make, model, color];
}

/// A driver in my favourites (`GET /passenger/favorite-drivers`, F16.3).
class FavoriteDriver extends Equatable {
  const FavoriteDriver({
    required this.driverId,
    required this.firstName,
    this.photoUrl,
    this.ratingAvg = 0,
    this.vehicle = const FavoriteVehicle(),
    this.rideCategoryCode,
    this.tripsTogether = 0,
    this.lastTripAt,
    this.createdAt,
  });

  final String driverId;
  final String firstName;

  /// Authenticated endpoint (`…/{driverId}/photo`); shown as a placeholder
  /// until the app can fetch protected images.
  final String? photoUrl;
  final double ratingAvg;
  final FavoriteVehicle vehicle;
  final String? rideCategoryCode;
  final int tripsTogether;
  final DateTime? lastTripAt;
  final DateTime? createdAt;

  @override
  List<Object?> get props => <Object?>[
    driverId,
    firstName,
    photoUrl,
    ratingAvg,
    vehicle,
    rideCategoryCode,
    tripsTogether,
    lastTripAt,
    createdAt,
  ];
}
