import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Cancelled / no-drivers message with a retry action.
class TripEndedView extends StatelessWidget {
  const TripEndedView({
    super.key,
    required this.stage,
    required this.retryLabel,
    required this.onRetry,
    this.copy,
  });

  final TripStage stage;
  final String retryLabel;
  final VoidCallback onRetry;
  final String? copy;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool noDrivers = stage == TripStage.noDrivers;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Center(
          child: IconBox(
            icon: AtaIcons.clock,
            size: AtaSizes.iconBoxHero,
            iconSize: AtaSizes.iconHero,
            background: AtaColors.dangerSoft,
            foreground: AtaColors.danger,
            round: true,
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(
          noDrivers ? l10n.noDriversTitle : l10n.cancelledTitle,
          style: AtaText.headline,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xxs),
        Text(
          copy ?? (noDrivers ? l10n.noDriversCopy : l10n.cancelledCopy),
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xl),
        AtaButton(label: retryLabel, onPressed: onRetry),
      ],
    );
  }
}
