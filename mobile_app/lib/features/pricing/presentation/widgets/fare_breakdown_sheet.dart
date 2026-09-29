import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/widgets/demand_badge.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "تفاصيل السعر": how the selected category total was computed. Driver net
/// earnings are never shown to passengers.
class FareBreakdownSheet extends StatelessWidget {
  const FareBreakdownSheet({
    super.key,
    required this.quote,
    required this.category,
  });

  final FareQuote quote;
  final QuoteCategory category;

  static Future<void> show(
    BuildContext context, {
    required FareQuote quote,
    required QuoteCategory category,
  }) => showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => FareBreakdownSheet(quote: quote, category: category),
  );

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final FareBreakdown b = category.breakdown;
    return Padding(
      padding: EdgeInsets.fromLTRB(
        AtaSpacing.lg,
        AtaSpacing.lg,
        AtaSpacing.lg,
        AtaSpacing.lg + MediaQuery.paddingOf(context).bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const SheetHandle(),
          Text(l10n.fareDetails, style: AtaText.section),
          const SizedBox(height: AtaSpacing.xxs),
          Text(l10n.fareDetailsCopy(category.name), style: AtaText.small),
          if (quote.demand.code.isElevated) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            DemandChip(level: quote.demand),
          ],
          const SizedBox(height: AtaSpacing.md),
          _Row(l10n.fareBaseFare, TripText.price(l10n, b.baseFare)),
          _Row(
            l10n.fareDistance(TripText.distance(l10n, quote.distanceMeters)),
            TripText.price(l10n, b.distanceFare),
          ),
          _Row(
            l10n.fareTime(TripText.duration(l10n, quote.durationSeconds)),
            TripText.price(l10n, b.timeFare),
          ),
          if (b.minFareApplied) _Row(l10n.fareMinApplied, '', muted: true),
          if (b.hasTimeMultiplier)
            _Row(
              l10n.fareTimeMultiplier,
              l10n.multiplierValue(Money.compact(b.timeMultiplier)),
            ),
          if (b.hasDemandMultiplier)
            _Row(
              l10n.fareDemandMultiplier,
              l10n.multiplierValue(Money.compact(b.demandMultiplier)),
            ),
          _Row(l10n.fareBookingFee, TripText.price(l10n, b.bookingFee)),
          _Row(l10n.fareServiceFee, TripText.price(l10n, b.serviceFee)),
          if (b.hasDiscount && b.discounts.isEmpty)
            _Row(
              l10n.fareDiscount,
              '- ${TripText.price(l10n, b.discount)}',
              color: AtaColors.brand,
            ),
          for (final FareDiscount d in b.discounts)
            _Row(
              discountLabel(l10n, d),
              '- ${TripText.price(l10n, d.amount)}',
              color: AtaColors.brand,
            ),
          const Divider(color: AtaColors.line, height: AtaSpacing.lg),
          if (b.hasDiscount)
            _Row(
              l10n.fareTotalBeforeDiscount,
              TripText.price(l10n, category.totalBeforeDiscount),
              muted: true,
            ),
          _Row(
            l10n.fareTotal,
            TripText.price(l10n, category.total),
            style: AtaText.section,
          ),
          const SizedBox(height: AtaSpacing.md),
          AtaButton(
            label: l10n.done,
            variant: AtaButtonVariant.soft,
            height: AtaSizes.buttonCompact,
            onPressed: () => Navigator.of(context).pop(),
          ),
        ],
      ),
    );
  }
}

/// "خصم ATA10 · كود خصم": API label (or the source name) and its source.
String discountLabel(AppLocalizations l10n, FareDiscount d) {
  final String source = switch (d.source) {
    DiscountSource.promotion => l10n.discountSourcePromotion,
    DiscountSource.favoriteDriver => l10n.discountSourceFavoriteDriver,
    DiscountSource.other => l10n.fareDiscount,
  };
  if (d.label.isNotEmpty) return '${d.label} · $source';
  return d.reference.isEmpty ? source : '$source (${d.reference})';
}

class _Row extends StatelessWidget {
  const _Row(
    this.label,
    this.value, {
    this.muted = false,
    this.color,
    this.style,
  });

  final String label;
  final String value;
  final bool muted;
  final Color? color;
  final TextStyle? style;

  @override
  Widget build(BuildContext context) {
    final TextStyle base = style ?? (muted ? AtaText.small : AtaText.body);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          Expanded(child: Text(label, style: base)),
          Text(
            value,
            style: (style ?? AtaText.bodyStrong).copyWith(color: color),
            textDirection: TextDirection.ltr,
          ),
        ],
      ),
    );
  }
}
