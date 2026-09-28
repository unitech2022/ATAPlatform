import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:flutter/material.dart';

/// White panel used by the phone/OTP/terms steps (panel shadow).
class AuthPanel extends StatelessWidget {
  const AuthPanel({super.key, required this.children, this.centered = false});

  final List<Widget> children;
  final bool centered;

  @override
  Widget build(BuildContext context) {
    return AtaCard(
      shadow: AtaShadows.panel,
      padding: const EdgeInsets.all(AtaSpacing.xl),
      child: Column(
        crossAxisAlignment: centered
            ? CrossAxisAlignment.center
            : CrossAxisAlignment.stretch,
        children: children,
      ),
    );
  }
}
