import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/favorite_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One favourite driver: name, rating, vehicle, trips together, last trip,
/// the "available now + ETA" badge and the remove action.
class FavoriteDriverTile extends StatelessWidget {
  const FavoriteDriverTile({
    super.key,
    required this.driver,
    required this.onRemove,
    this.available,
    this.removing = false,
  });

  final FavoriteDriver driver;

  /// Set when the driver can take a trip from the pickup now.
  final AvailableFavorite? available;
  final bool removing;
  final VoidCallback? onRemove;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String vehicle = FavoriteText.vehicle(l10n, driver.vehicle);
    final DateTime? last = driver.lastTripAt;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              const IconBox(
                icon: AtaIcons.user,
                size: AtaSizes.iconBoxLarge,
                iconSize: AtaSizes.iconLarge,
                round: true,
              ),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Row(
                      children: <Widget>[
                        Flexible(
                          child: Text(
                            driver.firstName,
                            style: AtaText.bodyStrong,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: AtaSpacing.xxs),
                        const AtaIcon(
                          AtaIcons.heart,
                          size: AtaSizes.iconSmall,
                          color: AtaColors.danger,
                          filled: true,
                        ),
                      ],
                    ),
                    Text(
                      FavoriteText.rating(l10n, driver.ratingAvg),
                      style: AtaText.caption,
                    ),
                    if (vehicle.isNotEmpty)
                      Text(
                        vehicle,
                        style: AtaText.caption,
                        overflow: TextOverflow.ellipsis,
                      ),
                  ],
                ),
              ),
              if (removing)
                const SizedBox.square(
                  dimension: AtaSizes.iconMedium,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              else
                IconButton(
                  key: ValueKey<String>('remove-${driver.driverId}'),
                  tooltip: l10n.favoriteRemove,
                  onPressed: onRemove,
                  icon: const AtaIcon(AtaIcons.heart, color: AtaColors.muted),
                ),
            ],
          ),
          const SizedBox(height: AtaSpacing.sm),
          Wrap(
            spacing: AtaSpacing.xs,
            runSpacing: AtaSpacing.xs,
            children: <Widget>[
              if (available != null)
                AtaBadge(
                  key: ValueKey<String>('available-${driver.driverId}'),
                  label: FavoriteText.availability(l10n, available!),
                ),
              if (driver.tripsTogether > 0)
                AtaBadge(
                  label: l10n.favoriteTripsTogether(driver.tripsTogether),
                  background: AtaColors.cloud,
                  foreground: AtaColors.ink,
                ),
              if (last != null)
                AtaBadge(
                  label: l10n.favoriteLastTrip(
                    DateText.longDate(last, context.localeCode),
                  ),
                  background: AtaColors.cloud,
                  foreground: AtaColors.muted,
                ),
            ],
          ),
        ],
      ),
    );
  }
}
