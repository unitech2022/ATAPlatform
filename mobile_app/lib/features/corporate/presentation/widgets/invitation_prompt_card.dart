import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// In-app prompt on the account page: a company invited the rider. Opens
/// `/account/corporate` where the invitation is accepted or declined.
class InvitationPromptCard extends StatelessWidget {
  const InvitationPromptCard({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      key: const ValueKey<String>('invitation-prompt'),
      borderColor: AtaColors.brand,
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              const IconBox(icon: AtaIcons.building),
              const SizedBox(width: AtaSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(l10n.corpPromptTitle, style: AtaText.bodyStrong),
                    Text(l10n.corpPromptCopy, style: AtaText.caption),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.md),
          AtaButton(
            label: l10n.corpPromptAction,
            variant: AtaButtonVariant.brand,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.go(AppRoutes.accountCorporate),
          ),
        ],
      ),
    );
  }
}
