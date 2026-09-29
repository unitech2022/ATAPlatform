import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/rewards_text.dart';
import 'package:flutter/material.dart';

/// Trophy pill with the tier name in its colors.
class TierBadge extends StatelessWidget {
  const TierBadge({super.key, required this.tier});

  final DriverTier tier;

  static (Color, Color) colorsOf(DriverTier tier) => switch (tier) {
    DriverTier.bronze => (AtaColors.warningSoft, AtaColors.warning),
    DriverTier.silver => (AtaColors.cloud, AtaColors.muted),
    DriverTier.gold => (AtaColors.warning, AtaColors.white),
    DriverTier.platinum => (AtaColors.ink, AtaColors.white),
  };

  @override
  Widget build(BuildContext context) {
    final (Color background, Color foreground) = colorsOf(tier);
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.sm,
        vertical: AtaSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: background,
        borderRadius: AtaRadii.pillRadius,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          AtaIcon(AtaIcons.trophy, size: AtaSizes.iconSmall, color: foreground),
          const SizedBox(width: AtaSpacing.xxs),
          Text(
            RewardsText.tier(context.l10n, tier),
            style: AtaText.captionStrong.copyWith(color: foreground),
          ),
        ],
      ),
    );
  }
}
