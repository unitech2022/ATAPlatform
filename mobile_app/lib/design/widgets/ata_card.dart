import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// White rounded card (24px) with the soft shadow.
class AtaCard extends StatelessWidget {
  const AtaCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(AtaSpacing.xl),
    this.onTap,
    this.borderColor,
    this.shadow,
    this.color = AtaColors.white,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final Color? borderColor;
  final List<BoxShadow>? shadow;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final Widget body = Padding(padding: padding, child: child);
    return DecoratedBox(
      decoration: BoxDecoration(
        color: color,
        borderRadius: AtaRadii.cardRadius,
        border: Border.all(
          color: borderColor ?? Colors.transparent,
          width: AtaSizes.borderThick,
        ),
        boxShadow: shadow ?? AtaShadows.soft,
      ),
      child: onTap == null
          ? body
          : Material(
              type: MaterialType.transparency,
              borderRadius: AtaRadii.cardRadius,
              clipBehavior: Clip.antiAlias,
              child: InkWell(onTap: onTap, child: body),
            ),
    );
  }
}
