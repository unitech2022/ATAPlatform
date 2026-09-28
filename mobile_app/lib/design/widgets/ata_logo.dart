import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// The ATA logo at its design size (96x44, contain).
class AtaLogo extends StatelessWidget {
  const AtaLogo({
    super.key,
    this.height = AtaSizes.logoHeight,
    this.width = AtaSizes.logoWidth,
  });

  static const String assetPath = 'assets/images/logo.png';

  final double height;
  final double width;

  @override
  Widget build(BuildContext context) {
    return Image.asset(
      assetPath,
      height: height,
      width: width,
      fit: BoxFit.contain,
      semanticLabel: 'ATA',
    );
  }
}
