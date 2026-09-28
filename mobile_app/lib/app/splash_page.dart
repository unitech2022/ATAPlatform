import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_logo.dart';
import 'package:ata_app/design/widgets/decorative_background.dart';
import 'package:flutter/material.dart';

/// Shown while the persisted session is being restored.
class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: DecorativeBackground(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              AtaLogo(),
              SizedBox(height: AtaSpacing.xl),
              CircularProgressIndicator(color: AtaColors.brand),
            ],
          ),
        ),
      ),
    );
  }
}
