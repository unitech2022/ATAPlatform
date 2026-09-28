import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Inline error message on a `danger-soft` background.
class InlineError extends StatelessWidget {
  const InlineError({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.dangerSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Text(
        message,
        style: AtaText.label.copyWith(color: AtaColors.danger),
        textAlign: TextAlign.center,
      ),
    );
  }
}

/// Centered progress indicator with page padding.
class CenteredLoader extends StatelessWidget {
  const CenteredLoader({super.key});

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.all(AtaSpacing.xxl),
      child: Center(child: CircularProgressIndicator(color: AtaColors.brand)),
    );
  }
}
