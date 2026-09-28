import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Square (or round) tinted box holding an icon: `brand-soft` + brand icon,
/// `cloud` + ink icon, `brand` + white icon, etc.
class IconBox extends StatelessWidget {
  const IconBox({
    super.key,
    required this.icon,
    this.size = AtaSizes.iconBox,
    this.iconSize = AtaSizes.iconDefault,
    this.background = AtaColors.brandSoft,
    this.foreground = AtaColors.brand,
    this.round = false,
    this.radius = AtaRadii.small,
  });

  /// `cloud` background with ink icon.
  const IconBox.cloud({
    super.key,
    required this.icon,
    this.size = AtaSizes.iconBox,
    this.iconSize = AtaSizes.iconDefault,
    this.round = false,
    this.radius = AtaRadii.small,
  }) : background = AtaColors.cloud,
       foreground = AtaColors.ink;

  /// `brand` background with white icon.
  const IconBox.brand({
    super.key,
    required this.icon,
    this.size = AtaSizes.iconBox,
    this.iconSize = AtaSizes.iconDefault,
    this.round = false,
    this.radius = AtaRadii.item,
  }) : background = AtaColors.brand,
       foreground = AtaColors.white;

  final AtaIcons icon;
  final double size;
  final double iconSize;
  final Color background;
  final Color foreground;
  final bool round;
  final double radius;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: background,
        shape: round ? BoxShape.circle : BoxShape.rectangle,
        borderRadius: round ? null : BorderRadius.circular(radius),
      ),
      child: AtaIcon(icon, size: iconSize, color: foreground),
    );
  }
}
