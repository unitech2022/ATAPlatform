import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// The small grab bar at the top of bottom sheets.
class SheetHandle extends StatelessWidget {
  const SheetHandle({super.key});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Container(
        width: AtaSizes.sheetHandleWidth,
        height: AtaSizes.sheetHandleHeight,
        margin: const EdgeInsets.only(bottom: AtaSpacing.md),
        decoration: const BoxDecoration(
          color: AtaColors.line,
          borderRadius: AtaRadii.pillRadius,
        ),
      ),
    );
  }
}
