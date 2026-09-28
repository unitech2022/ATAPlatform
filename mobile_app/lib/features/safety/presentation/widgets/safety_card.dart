import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:flutter/material.dart';

/// Feature card with a large brand-soft icon and "learn more" link.
class SafetyCard extends StatelessWidget {
  const SafetyCard({
    super.key,
    required this.icon,
    required this.title,
    required this.copy,
  });

  final AtaIcons icon;
  final String title;
  final String copy;

  @override
  Widget build(BuildContext context) {
    return AtaCard(
      onTap: () {},
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          IconBox(
            icon: icon,
            size: AtaSizes.iconBoxLarge,
            iconSize: AtaSizes.iconLarge,
            radius: AtaSpacing.md,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          Text(title, style: AtaText.headline),
          const SizedBox(height: AtaSpacing.sm),
          Text(copy, style: AtaText.small),
          const SizedBox(height: AtaSpacing.xl),
          Row(
            children: <Widget>[
              Text(
                context.l10n.learnMore,
                style: AtaText.label.copyWith(color: AtaColors.brand),
              ),
              const SizedBox(width: AtaSpacing.xs),
              const AtaIcon(
                AtaIcons.arrow,
                size: AtaSizes.iconSmall,
                color: AtaColors.brand,
              ),
            ],
          ),
        ],
      ),
    );
  }
}
