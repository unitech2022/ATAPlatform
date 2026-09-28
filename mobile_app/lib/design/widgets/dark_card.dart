import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// Dark `ink` card with white text and the button shadow. Optionally shows
/// the decorative brand circle in a corner (wallet balance card).
class DarkCard extends StatelessWidget {
  const DarkCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(AtaSpacing.xl),
    this.decorated = false,
    this.onTap,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final bool decorated;
  final VoidCallback? onTap;

  static const double _circleSize = 160;
  static const double _circleOffset = -48;

  @override
  Widget build(BuildContext context) {
    final Widget body = Padding(padding: padding, child: child);
    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: AtaColors.ink,
        borderRadius: AtaRadii.cardRadius,
        boxShadow: AtaShadows.button,
      ),
      child: DefaultTextStyle.merge(
        style: const TextStyle(color: AtaColors.white),
        child: IconTheme.merge(
          data: const IconThemeData(color: AtaColors.white),
          child: Stack(
            children: <Widget>[
              if (decorated)
                PositionedDirectional(
                  start: _circleOffset,
                  top: _circleOffset,
                  child: Container(
                    width: _circleSize,
                    height: _circleSize,
                    decoration: BoxDecoration(
                      color: AtaColors.brand20,
                      shape: BoxShape.circle,
                    ),
                  ),
                ),
              if (onTap == null)
                body
              else
                Material(
                  type: MaterialType.transparency,
                  child: InkWell(onTap: onTap, child: body),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
