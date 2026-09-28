import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Fare lines (labels localized by the API), discounts with their source,
/// subtotal, total and the VAT included.
class ReceiptLinesCard extends StatelessWidget {
  const ReceiptLinesCard({super.key, required this.receipt});

  final Receipt receipt;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.receiptBreakdown, style: AtaText.section),
          const SizedBox(height: AtaSpacing.md),
          for (final ReceiptLine line in receipt.lines)
            ReceiptAmountRow(
              label: line.label,
              amount: line.amount,
              badge: line.isDiscount ? discountSource(l10n, line.source) : null,
              color: line.amount < 0 ? AtaColors.brand : null,
            ),
          const Divider(height: AtaSpacing.xl, color: AtaColors.line),
          ReceiptAmountRow(
            label: l10n.receiptSubtotal,
            amount: receipt.subtotal,
          ),
          if (receipt.discountTotal > 0)
            ReceiptAmountRow(
              label: l10n.receiptDiscountTotal,
              amount: -receipt.discountTotal,
              color: AtaColors.brand,
            ),
          ReceiptAmountRow(
            label: l10n.fareTotal,
            amount: receipt.total,
            strong: true,
          ),
          if (receipt.vatIncluded > 0)
            Text(
              l10n.receiptVat(
                Money.compact(receipt.vatRate),
                l10n.priceWithCurrency(Money.fixed(receipt.vatIncluded)),
              ),
              style: AtaText.caption,
            ),
        ],
      ),
    );
  }

  /// `عرض ترويجي` / `الكابتن المفضل` for a discount source.
  static String? discountSource(AppLocalizations l10n, String? source) =>
      switch (source) {
        ReceiptDiscount.promotion => l10n.discountSourcePromotion,
        ReceiptDiscount.favoriteDriver => l10n.discountSourceFavoriteDriver,
        _ => null,
      };
}

/// Label / signed amount row of a receipt.
class ReceiptAmountRow extends StatelessWidget {
  const ReceiptAmountRow({
    super.key,
    required this.label,
    required this.amount,
    this.badge,
    this.strong = false,
    this.color,
  });

  final String label;
  final double amount;
  final String? badge;
  final bool strong;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final String value = context.l10n.priceWithCurrency(Money.fixed(amount));
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          Flexible(
            child: Text(label, style: strong ? AtaText.label : AtaText.small),
          ),
          if (badge != null) ...<Widget>[
            const SizedBox(width: AtaSpacing.xs),
            AtaBadge(label: badge!),
          ],
          const Spacer(),
          Text(
            value,
            textDirection: TextDirection.ltr,
            style: (strong ? AtaText.section : AtaText.bodyStrong).copyWith(
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}
