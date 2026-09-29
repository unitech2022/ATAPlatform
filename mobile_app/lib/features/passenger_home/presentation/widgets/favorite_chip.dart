import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/favorite_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// A favourite captain available now, as a selectable chip of the request
/// sheet: name, rating, ETA and the discount badge.
class FavoriteChip extends StatelessWidget {
  const FavoriteChip({
    super.key,
    required this.name,
    required this.selected,
    required this.onTap,
    this.favorite,
  });

  /// Available favourite; `null` for a selected driver who dropped out of
  /// the availability list (shown as unavailable, still selected).
  final AvailableFavorite? favorite;
  final String name;
  final bool selected;
  final VoidCallback onTap;

  static const double _width = 148;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final AvailableFavorite? f = favorite;
    final int? eta = f?.etaMinutes;
    final FavoriteDiscount? discount = f?.discount;
    return SizedBox(
      width: _width,
      child: SelectableTile(
        selected: selected,
        onTap: onTap,
        radius: AtaRadii.smallRadius,
        padding: const EdgeInsets.all(AtaSpacing.sm),
        unselectedBackground: AtaColors.cloud,
        unselectedBorder: Colors.transparent,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Row(
              children: <Widget>[
                const AtaIcon(
                  AtaIcons.heart,
                  size: AtaSizes.iconSmall,
                  color: AtaColors.danger,
                  filled: true,
                ),
                const SizedBox(width: AtaSpacing.xxs),
                Flexible(
                  child: Text(
                    name,
                    style: AtaText.bodyStrong,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
            Text(
              f == null
                  ? l10n.favoriteChipUnavailable
                  : <String>[
                      FavoriteText.rating(l10n, f.ratingAvg),
                      if (eta != null) l10n.minutesLabel(eta),
                    ].join(' · '),
              style: AtaText.caption,
              overflow: TextOverflow.ellipsis,
            ),
            if (discount != null)
              Text(
                FavoriteText.discount(l10n, discount),
                style: AtaText.captionStrong.copyWith(color: AtaColors.brand),
              ),
          ],
        ),
      ),
    );
  }
}
