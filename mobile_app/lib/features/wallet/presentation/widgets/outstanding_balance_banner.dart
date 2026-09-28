import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Warning shown while money is owed: a negative passenger balance (wallet
/// page, blocked ride request) or a driver's cash debt.
class OutstandingBalanceBanner extends StatelessWidget {
  const OutstandingBalanceBanner({
    super.key,
    required this.title,
    required this.message,
    required this.actionLabel,
    required this.onAction,
    this.amount,
  });

  final String title;
  final String message;
  final String actionLabel;
  final VoidCallback onAction;

  /// Owed amount (positive), shown next to the title.
  final double? amount;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final double? owed = amount;
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: AtaColors.dangerSoft,
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(color: AtaColors.danger.withValues(alpha: 0.2)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(
                  title,
                  style: AtaText.label.copyWith(color: AtaColors.danger),
                ),
              ),
              if (owed != null)
                Text(
                  l10n.priceWithCurrency(Money.fixed(owed)),
                  textDirection: TextDirection.ltr,
                  style: AtaText.bodyStrong.copyWith(color: AtaColors.danger),
                ),
            ],
          ),
          const SizedBox(height: AtaSpacing.xxs),
          Text(message, style: AtaText.small),
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: actionLabel,
            icon: AtaIcons.plus,
            variant: AtaButtonVariant.danger,
            height: AtaSizes.buttonCompact,
            onPressed: onAction,
          ),
        ],
      ),
    );
  }
}
