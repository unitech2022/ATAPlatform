import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Dark "need urgent help?" bar.
class EmergencyBar extends StatelessWidget {
  const EmergencyBar({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return DarkCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              const IconBox.brand(
                icon: AtaIcons.shield,
                size: AtaSizes.iconBoxLarge,
                iconSize: AtaSizes.iconLarge,
                round: true,
              ),
              const SizedBox(width: AtaSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      l10n.emergencyTitle,
                      style: AtaText.section.copyWith(color: AtaColors.white),
                    ),
                    Text(
                      l10n.emergencyCopy,
                      style: AtaText.small.copyWith(color: AtaColors.white60),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.lg),
          AtaButton(
            label: l10n.contactUs,
            variant: AtaButtonVariant.white,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.go(AppRoutes.accountContact),
          ),
        ],
      ),
    );
  }
}
