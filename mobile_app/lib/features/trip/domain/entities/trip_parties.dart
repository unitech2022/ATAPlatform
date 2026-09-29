import 'package:equatable/equatable.dart';

/// The assigned driver as shown to the passenger.
class TripDriver extends Equatable {
  const TripDriver({
    required this.id,
    required this.fullName,
    required this.ratingAvg,
    this.photoFileId,
    this.phoneMasked,
    this.gender,
    this.isFavorite = false,
  });

  final String id;
  final String fullName;
  final double ratingAvg;
  final String? photoFileId;
  final String? phoneMasked;
  final String? gender;

  /// The rider has this driver in the favourites (optional API flag).
  final bool isFavorite;

  String get firstName => fullName.trim().split(' ').first;

  @override
  List<Object?> get props => <Object?>[
    id,
    fullName,
    ratingAvg,
    photoFileId,
    phoneMasked,
    gender,
    isFavorite,
  ];
}

/// The driver's vehicle.
class TripVehicle extends Equatable {
  const TripVehicle({
    required this.make,
    required this.model,
    required this.color,
    required this.plateNumber,
  });

  final String make;
  final String model;
  final String color;
  final String plateNumber;

  String get title => '$make $model';

  @override
  List<Object?> get props => <Object?>[make, model, color, plateNumber];
}

/// The passenger as shown to the driver.
class TripPassenger extends Equatable {
  const TripPassenger({
    required this.firstName,
    this.phoneMasked,
    this.ratingAvg,
  });

  final String firstName;
  final String? phoneMasked;
  final double? ratingAvg;

  @override
  List<Object?> get props => <Object?>[firstName, phoneMasked, ratingAvg];
}
