import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized labels of favourite drivers.
abstract final class FavoriteText {
  /// "Toyota Camry · أبيض", empty without vehicle data.
  static String vehicle(AppLocalizations l10n, FavoriteVehicle v) =>
      v.isEmpty ? '' : l10n.vehicleLine(v.make, v.model, v.color);

  static String rating(AppLocalizations l10n, double avg) =>
      l10n.ratingValue(Money.compact(avg));

  /// "متاح الآن · يصل خلال 4 دقائق" (or just "متاح الآن" without an ETA).
  static String availability(AppLocalizations l10n, AvailableFavorite a) =>
      a.etaMinutes == null
      ? l10n.favoriteAvailableNow
      : l10n.favoriteAvailableEta(l10n.minutesLabel(a.etaMinutes!));

  /// "خصم 10%".
  static String discount(AppLocalizations l10n, FavoriteDiscount d) =>
      l10n.favoriteDiscountBadge(Money.compact(d.percent));
}
