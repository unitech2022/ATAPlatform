import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/trip/domain/entities/trip_rewards.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Searching-state copy of a trip that asked for a favourite captain (F16):
/// "نتواصل مع كابتنك المفضل..." while the exclusive offer is pending, the
/// normal copy once the search fell back.
abstract final class FavoriteSearchText {
  static String title(AppLocalizations l10n, TripFavorite? favorite) =>
      favorite?.isPending ?? false
      ? l10n.favoriteSearchingTitle
      : l10n.searchingTitle;

  /// `null` keeps the standard "we are looking for the nearest captain".
  static String? copy(AppLocalizations l10n, TripFavorite? favorite) =>
      favorite?.isPending ?? false
      ? l10n.favoriteSearchingCopy(favorite!.driverName)
      : null;
}

/// "لم يتمكن {name} من الرد، نبحث لك عن كابتن آخر" once the exclusive round
/// ended without the favourite (`rejected` / `expired` / `unavailable`).
class FavoriteFallbackNotice extends StatelessWidget {
  const FavoriteFallbackNotice({super.key, required this.favorite});

  final TripFavorite favorite;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Container(
      key: const ValueKey<String>('favorite-fallback-notice'),
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.warningSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Text(
        favorite.status == FavoriteStatus.unavailable
            ? l10n.favoriteUnavailableNotice(favorite.driverName)
            : l10n.favoriteFallbackNotice(favorite.driverName),
        style: AtaText.label.copyWith(color: AtaColors.warning),
        textAlign: TextAlign.center,
      ),
    );
  }
}
