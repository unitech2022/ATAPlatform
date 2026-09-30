import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:flutter/material.dart';

/// A big action card of the help center ("تذاكري", "تواصل معنا"): icon,
/// title, copy and an optional badge.
class SupportEntryCard extends StatelessWidget {
  const SupportEntryCard({
    super.key,
    required this.icon,
    required this.title,
    required this.copy,
    required this.onTap,
    this.badge,
  });

  final AtaIcons icon;
  final String title;
  final String copy;
  final VoidCallback onTap;

  /// Shown at the end when set (for example the unread count).
  final String? badge;

  @override
  Widget build(BuildContext context) {
    return AtaCard(
      onTap: onTap,
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Row(
        children: <Widget>[
          IconBox(icon: icon),
          const SizedBox(width: AtaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(title, style: AtaText.bodyStrong),
                Text(copy, style: AtaText.caption),
              ],
            ),
          ),
          if (badge != null) ...<Widget>[
            AtaBadge(
              label: badge!,
              background: AtaColors.brand,
              foreground: AtaColors.white,
            ),
            const SizedBox(width: AtaSpacing.xs),
          ],
          const AtaIcon(
            AtaIcons.chevron,
            size: AtaSizes.iconSmall,
            color: AtaColors.muted,
          ),
        ],
      ),
    );
  }
}
