import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/money_text.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Net amount on a dark card, then the period breakdown and daily rows.
class StatementTotalsCard extends StatelessWidget {
  const StatementTotalsCard({super.key, required this.statement});

  final EarningsStatement statement;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final StatementTotals t = statement.totals;
    final List<(String, double)> rows = <(String, double)>[
      (l10n.statementGross, t.grossFares),
      (l10n.statementCommission, -t.commission),
      (l10n.statementEarnings, t.earnings),
      (l10n.statementIncentives, t.incentives),
      (l10n.statementCompensation, t.cancellationCompensation),
      (l10n.statementAdjustments, t.adjustments),
      (l10n.statementCashCollected, -t.cashCollected),
      (l10n.statementPayouts, -t.payouts),
    ];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        DarkCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Text(
                l10n.statementNet,
                style: AtaText.small.copyWith(color: AtaColors.white60),
              ),
              const SizedBox(height: AtaSpacing.xs),
              MoneyText(
                amount: Money.fixed(t.net),
                currency: l10n.currency,
                style: AtaText.display.copyWith(color: AtaColors.white),
              ),
              const SizedBox(height: AtaSpacing.xs),
              Text(
                l10n.statementTrips(t.trips),
                style: AtaText.caption.copyWith(color: AtaColors.white60),
              ),
            ],
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        AtaCard(
          child: Column(
            children: <Widget>[
              for (final (String label, double value) in rows)
                _Row(label: label, value: value),
            ],
          ),
        ),
        if (statement.days.isNotEmpty) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          AtaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(l10n.statementDaily, style: AtaText.section),
                const SizedBox(height: AtaSpacing.sm),
                for (final StatementDay day in statement.days)
                  _Row(
                    label:
                        '${DateText.longDate(day.date, context.localeCode)}'
                        ' · ${l10n.statementTrips(day.trips)}',
                    value: day.earnings,
                  ),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.label, required this.value});

  final String label;
  final double value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          Expanded(child: Text(label, style: AtaText.small)),
          Text(
            context.l10n.priceWithCurrency(Money.fixed(value)),
            textDirection: TextDirection.ltr,
            style: AtaText.bodyStrong.copyWith(
              color: value < 0 ? AtaColors.danger : AtaColors.ink,
            ),
          ),
        ],
      ),
    );
  }
}
