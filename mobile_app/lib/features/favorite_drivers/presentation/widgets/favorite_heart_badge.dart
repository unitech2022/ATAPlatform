import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Heart shown when the assigned driver is a favourite: with the "مفضل"
/// label, or icon only ([showLabel] false) where space is tight.
class FavoriteHeartBadge extends StatelessWidget {
  const FavoriteHeartBadge({super.key, this.showLabel = true});

  final bool showLabel;

  @override
  Widget build(BuildContext context) {
    final String label = context.l10n.favoriteDriverBadge;
    return Semantics(
      label: label,
      child: Row(
        key: const ValueKey<String>('favorite-heart-badge'),
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          const AtaIcon(
            AtaIcons.heart,
            size: AtaSizes.iconSmall,
            color: AtaColors.danger,
            filled: true,
          ),
          if (showLabel) ...<Widget>[
            const SizedBox(width: AtaSpacing.xxs),
            Text(
              label,
              style: AtaText.captionStrong.copyWith(color: AtaColors.danger),
            ),
          ],
        ],
      ),
    );
  }
}
