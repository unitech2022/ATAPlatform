import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// Thin rounded progress bar: `brand` fill on a translucent white track.
class ProgressBar extends StatelessWidget {
  const ProgressBar({
    super.key,
    required this.value,
    this.track,
    this.fill = AtaColors.brand,
  });

  /// 0..1
  final double value;
  final Color? track;
  final Color fill;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: AtaRadii.pillRadius,
      child: SizedBox(
        height: AtaSizes.progressBar,
        child: Stack(
          children: <Widget>[
            Positioned.fill(
              child: ColoredBox(color: track ?? AtaColors.white10),
            ),
            FractionallySizedBox(
              widthFactor: value.clamp(0, 1).toDouble(),
              heightFactor: 1,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: fill,
                  borderRadius: AtaRadii.pillRadius,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
