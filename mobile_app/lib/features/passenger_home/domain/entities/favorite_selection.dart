import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:equatable/equatable.dart';

/// The favourite driver picked on the request sheet (F16): sent as
/// `favoriteDriverId` with the quote and the trip request.
class FavoriteSelection extends Equatable {
  const FavoriteSelection({
    required this.driverId,
    required this.name,
    this.discount,
  });

  factory FavoriteSelection.from(AvailableFavorite favorite) =>
      FavoriteSelection(
        driverId: favorite.driverId,
        name: favorite.firstName,
        discount: favorite.discount,
      );

  final String driverId;
  final String name;

  /// The rule that applies once the driver accepts (from `/available`).
  final FavoriteDiscount? discount;

  @override
  List<Object?> get props => <Object?>[driverId, name, discount];
}

/// How the favourite discount and the promo code combined in the quote.
enum FavoritePromoOutcome {
  /// Nothing to explain.
  none,

  /// The promo code was not applied (not stackable, favourite wins).
  promoNotApplied,

  /// The favourite discount was not applied (the promo code is larger).
  favoriteNotApplied,
}
