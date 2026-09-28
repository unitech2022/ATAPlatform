import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:equatable/equatable.dart';

/// Passenger part of `GET /me`.
class PassengerProfile extends Equatable {
  const PassengerProfile({
    required this.ratingAvg,
    required this.ratingCount,
    required this.preferFemaleDriver,
    required this.defaultPaymentMethod,
    required this.memberSince,
  });

  final double ratingAvg;
  final int ratingCount;
  final bool preferFemaleDriver;
  final String defaultPaymentMethod;
  final String memberSince;

  @override
  List<Object?> get props => <Object?>[
    ratingAvg,
    ratingCount,
    preferFemaleDriver,
    defaultPaymentMethod,
    memberSince,
  ];
}

/// Full `GET /me` payload.
class Profile extends Equatable {
  const Profile({required this.user, this.passenger, this.driver});

  final User user;
  final PassengerProfile? passenger;
  final DriverSummary? driver;

  @override
  List<Object?> get props => <Object?>[user, passenger, driver];
}
