import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

/// 3-D Secure hand-off: opens the bank page in the browser; the result is
/// confirmed by the API (webhook / return page), never by the app.
class PaymentActionView extends StatelessWidget {
  const PaymentActionView({
    super.key,
    required this.action,
    required this.onDone,
  });

  final PaymentAction action;
  final VoidCallback onDone;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Center(
          child: IconBox(
            icon: AtaIcons.shield,
            size: AtaSizes.iconBoxHero,
            iconSize: AtaSizes.iconHero,
            background: AtaColors.warningSoft,
            foreground: AtaColors.warning,
            round: true,
          ),
        ),
        const SizedBox(height: AtaSpacing.lg),
        Text(
          l10n.paymentActionTitle,
          style: AtaText.headline,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xs),
        Text(
          l10n.paymentActionCopy,
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xl),
        AtaButton(
          label: l10n.paymentActionOpen,
          icon: AtaIcons.shield,
          onPressed: () => launchUrl(
            Uri.parse(action.url),
            mode: LaunchMode.externalApplication,
          ),
        ),
        const SizedBox(height: AtaSpacing.sm),
        AtaButton(
          label: l10n.done,
          variant: AtaButtonVariant.outline,
          onPressed: onDone,
        ),
      ],
    );
  }
}
