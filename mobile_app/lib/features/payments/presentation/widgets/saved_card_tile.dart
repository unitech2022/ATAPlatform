import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/bordered_row.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/presentation/widgets/payment_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// A saved card with its status, "make default" and "remove" actions.
class SavedCardTile extends StatelessWidget {
  const SavedCardTile({
    super.key,
    required this.card,
    required this.busy,
    required this.onMakeDefault,
    required this.onRemove,
  });

  final SavedCard card;
  final bool busy;
  final VoidCallback onMakeDefault;
  final VoidCallback onRemove;

  static const double _loaderSize = 20;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String subtitle = card.isExpired
        ? l10n.cardExpired
        : card.isPending
        ? l10n.cardPendingVerification
        : l10n.cardExpiry(card.expiryLabel);
    return BorderedRow(
      leading: card.isDefault
          ? const IconBox.brand(icon: AtaIcons.wallet)
          : const IconBox.cloud(icon: AtaIcons.wallet),
      title: PaymentText.card(l10n, card),
      titleTrailing: card.isDefault ? AtaBadge(label: l10n.cardDefault) : null,
      subtitle: subtitle,
      trailing: busy
          ? const SizedBox(
              width: _loaderSize,
              height: _loaderSize,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: AtaColors.brand,
              ),
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              mainAxisSize: MainAxisSize.min,
              children: <Widget>[
                if (!card.isDefault && card.isUsable)
                  _Action(label: l10n.cardMakeDefault, onTap: onMakeDefault),
                _Action(
                  label: l10n.cardRemove,
                  onTap: onRemove,
                  color: AtaColors.danger,
                ),
              ],
            ),
    );
  }
}

class _Action extends StatelessWidget {
  const _Action({
    required this.label,
    required this.onTap,
    this.color = AtaColors.brand,
  });

  final String label;
  final VoidCallback onTap;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
        child: Text(label, style: AtaText.captionStrong.copyWith(color: color)),
      ),
    );
  }
}
