import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:equatable/equatable.dart';

/// `discount` of an available favourite: the rule that would apply when the
/// driver accepts.
class FavoriteDiscount extends Equatable {
  const FavoriteDiscount({
    required this.percent,
    this.maxAmount,
    this.stackableWithPromotions = false,
  });

  final double percent;
  final double? maxAmount;
  final bool stackableWithPromotions;

  @override
  List<Object?> get props => <Object?>[
    percent,
    maxAmount,
    stackableWithPromotions,
  ];
}

/// A favourite that can take a trip now (`GET
/// /passenger/favorite-drivers/available`).
class AvailableFavorite extends Equatable {
  const AvailableFavorite({
    required this.driverId,
    required this.firstName,
    this.photoUrl,
    this.ratingAvg = 0,
    this.vehicle = const FavoriteVehicle(),
    this.etaMinutes,
    this.discount,
  });

  final String driverId;
  final String firstName;
  final String? photoUrl;
  final double ratingAvg;
  final FavoriteVehicle vehicle;
  final int? etaMinutes;
  final FavoriteDiscount? discount;

  @override
  List<Object?> get props => <Object?>[
    driverId,
    firstName,
    photoUrl,
    ratingAvg,
    vehicle,
    etaMinutes,
    discount,
  ];
}
