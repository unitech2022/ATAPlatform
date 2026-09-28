import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Small rounded tag (for example "للعميلات", "هذا الأسبوع").
class AtaBadge extends StatelessWidget {
  const AtaBadge({
    super.key,
    required this.label,
    this.background = AtaColors.brandSoft,
    this.foreground = AtaColors.brand,
  });

  final String label;
  final Color background;
  final Color foreground;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.sm,
        vertical: AtaSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: background,
        borderRadius: AtaRadii.pillRadius,
      ),
      child: Text(
        label,
        style: AtaText.captionStrong.copyWith(color: foreground),
      ),
    );
  }
}

/// Pill-shaped action (back button, "الكل", header wallet pill...).
class PillButton extends StatelessWidget {
  const PillButton({
    super.key,
    required this.label,
    required this.onTap,
    this.icon,
    this.background = AtaColors.white,
    this.foreground = AtaColors.ink,
    this.bordered = false,
    this.elevated = true,
    this.mirrorIcon = false,
  });

  /// Back pill: white, soft shadow, arrow icon.
  const PillButton.back({
    super.key,
    required this.label,
    required this.onTap,
    this.background = AtaColors.white,
    this.bordered = false,
    this.elevated = true,
  }) : icon = AtaIcons.arrow,
       foreground = AtaColors.ink,
       mirrorIcon = false;

  final String label;
  final VoidCallback? onTap;
  final AtaIcons? icon;
  final Color background;
  final Color foreground;
  final bool bordered;
  final bool elevated;
  final bool mirrorIcon;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.pillRadius,
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AtaSpacing.md,
            vertical: AtaSpacing.xs + 2,
          ),
          decoration: BoxDecoration(
            color: background,
            borderRadius: AtaRadii.pillRadius,
            border: bordered ? Border.all(color: AtaColors.line) : null,
            boxShadow: elevated ? AtaShadows.soft : null,
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              if (icon != null) ...<Widget>[
                AtaIcon(
                  icon!,
                  size: AtaSizes.iconSmall,
                  color: foreground,
                  mirrored: mirrorIcon,
                ),
                const SizedBox(width: AtaSpacing.xs),
              ],
              Text(label, style: AtaText.label.copyWith(color: foreground)),
            ],
          ),
        ),
      ),
    );
  }
}
