import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_paid_card.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/presentation/widgets/payment_text.dart';
import 'package:ata_app/features/payments/presentation/widgets/receipt_lines_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Payment method, the card-to-cash fallback notice, refunds and net paid.
/// A company-account trip shows "مدفوعة من حساب الشركة" with the purpose and
/// the cost center instead, and no wallet / card charge lines.
class ReceiptPaymentCard extends StatelessWidget {
  const ReceiptPaymentCard({super.key, required this.receipt});

  final Receipt receipt;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final ReceiptPayment payment = receipt.payment;
    final String method = receipt.isCorporate
        ? l10n.paymentCorporate
        : payment.method == 'card' && payment.last4 != null
        ? l10n.cardMasked(
            PaymentText.brand(l10n, payment.brand),
            payment.last4!,
          )
        : PaymentText.method(l10n, payment.method);
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(l10n.receiptPayment, style: AtaText.section),
              ),
              Flexible(
                child: Text(
                  method,
                  style: AtaText.bodyStrong,
                  textAlign: TextAlign.end,
                ),
              ),
            ],
          ),
          if (receipt.isCorporate) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            CorporatePaidCard(corporate: receipt.corporate),
            const SizedBox(height: AtaSpacing.sm),
            ReceiptAmountRow(
              label: l10n.corpBilledAmount,
              amount: receipt.total,
              strong: true,
            ),
          ] else
            ..._charged(l10n, payment),
          if (receipt.tripNumber.isNotEmpty) ...<Widget>[
            const SizedBox(height: AtaSpacing.xs),
            Text(
              l10n.tripNumberLabel(receipt.tripNumber),
              style: AtaText.caption,
            ),
          ],
        ],
      ),
    );
  }

  /// The card-to-cash notice, the paid amount, refunds and the net paid.
  List<Widget> _charged(AppLocalizations l10n, ReceiptPayment payment) =>
      <Widget>[
        if (payment.fallbackToCash) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          Container(
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: const BoxDecoration(
              color: AtaColors.warningSoft,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Text(
              l10n.paymentFallbackCash,
              style: AtaText.label.copyWith(color: AtaColors.warning),
            ),
          ),
        ],
        const SizedBox(height: AtaSpacing.sm),
        ReceiptAmountRow(label: l10n.receiptPaid, amount: payment.paidAmount),
        if (receipt.hasRefunds)
          ReceiptAmountRow(
            label: l10n.receiptRefunded,
            amount: -receipt.refundedTotal,
            color: AtaColors.brand,
          ),
        if (receipt.netPaid != null && receipt.hasRefunds)
          ReceiptAmountRow(
            label: l10n.receiptNetPaid,
            amount: receipt.netPaid!,
            strong: true,
          ),
      ];
}
