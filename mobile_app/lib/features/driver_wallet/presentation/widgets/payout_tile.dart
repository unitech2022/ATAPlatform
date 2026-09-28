import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/bordered_row.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_wallet_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One payout: number, date, IBAN, amount, status and cancel.
class PayoutTile extends StatelessWidget {
  const PayoutTile({
    super.key,
    required this.payout,
    required this.cancelling,
    required this.onCancel,
  });

  final Payout payout;
  final bool cancelling;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final (String label, Color fg, Color bg) = DriverWalletText.status(
      l10n,
      payout.status,
    );
    final DateTime? date = payout.paidAt ?? payout.requestedAt;
    final List<String> meta = <String>[
      if (date != null) DateText.longDate(date, context.localeCode),
      ?payout.ibanMasked,
      if (payout.rejectedReason != null)
        l10n.payoutRejectedReason(payout.rejectedReason!),
    ];
    return BorderedRow(
      leading: const IconBox.cloud(icon: AtaIcons.wallet),
      title: payout.payoutNumber,
      subtitle: meta.join(' · '),
      trailing: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: <Widget>[
          Text(
            l10n.priceWithCurrency(Money.compact(payout.amount)),
            style: AtaText.bodyStrong,
          ),
          const SizedBox(height: AtaSpacing.xxs),
          AtaBadge(label: label, foreground: fg, background: bg),
          if (payout.canCancel)
            InkWell(
              onTap: cancelling ? null : onCancel,
              child: Padding(
                padding: const EdgeInsets.only(top: AtaSpacing.xxs),
                child: Text(
                  cancelling ? l10n.loading : l10n.cancel,
                  style: AtaText.captionStrong.copyWith(
                    color: AtaColors.danger,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
