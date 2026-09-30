import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// The dispute status card of a ticket: reason, amounts and the status
/// (open / under review / approved / partially approved / rejected) with
/// the approved refund once decided.
class DisputeCard extends StatelessWidget {
  const DisputeCard({super.key, required this.dispute});

  final FareDispute dispute;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final (Color background, Color foreground) = switch (dispute.status) {
      DisputeStatus.approved ||
      DisputeStatus.partiallyApproved => (AtaColors.brandSoft, AtaColors.brand),
      DisputeStatus.rejected => (AtaColors.dangerSoft, AtaColors.danger),
      DisputeStatus.underReview => (AtaColors.warningSoft, AtaColors.warning),
      DisputeStatus.open => (AtaColors.cloud, AtaColors.ink),
    };
    final double? requested = dispute.requestedRefundAmount;
    final double? approved = dispute.approvedRefundAmount;
    String money(double v) => l10n.priceWithCurrency(Money.fixed(v));
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(l10n.disputeCardTitle, style: AtaText.bodyStrong),
              ),
              AtaBadge(
                label: SupportText.disputeStatus(l10n, dispute.status),
                background: background,
                foreground: foreground,
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.xs),
          Text(
            SupportText.disputeReason(l10n, dispute.reason),
            style: AtaText.small,
          ),
          if (dispute.chargedAmount > 0)
            Text(
              l10n.disputeCharged(money(dispute.chargedAmount)),
              style: AtaText.caption,
            ),
          if (requested != null)
            Text(
              l10n.disputeRequested(money(requested)),
              style: AtaText.caption,
            ),
          if (dispute.status.isResolved)
            Padding(
              padding: const EdgeInsets.only(top: AtaSpacing.xs),
              child: Text(
                approved != null && approved > 0
                    ? l10n.disputeApproved(money(approved))
                    : l10n.disputeNoRefund,
                style: AtaText.label.copyWith(color: foreground),
              ),
            ),
        ],
      ),
    );
  }
}
