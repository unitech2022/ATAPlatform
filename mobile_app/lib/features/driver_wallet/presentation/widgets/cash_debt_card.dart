import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Cash fares owed to the platform against the limit, with "settle".
class CashDebtCard extends StatelessWidget {
  const CashDebtCard({super.key, required this.summary});

  final PayoutSummary summary;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool blocked = summary.debtLimitReached;
    final Color accent = blocked ? AtaColors.danger : AtaColors.warning;
    return AtaCard(
      color: blocked ? AtaColors.dangerSoft : AtaColors.warningSoft,
      borderColor: accent.withValues(alpha: 0.25),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(
                  l10n.cashDebtTitle,
                  style: AtaText.section.copyWith(color: accent),
                ),
              ),
              Text(
                l10n.priceWithCurrency(Money.fixed(summary.cashDebt)),
                textDirection: TextDirection.ltr,
                style: AtaText.section.copyWith(color: accent),
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.sm),
          ProgressBar(value: summary.debtRatio),
          const SizedBox(height: AtaSpacing.xs),
          Text(
            l10n.cashDebtLimit(
              l10n.priceWithCurrency(Money.compact(summary.cashDebtLimit)),
            ),
            style: AtaText.caption,
          ),
          const SizedBox(height: AtaSpacing.xs),
          Text(
            blocked ? l10n.cashDebtLimitError : l10n.cashDebtCopy,
            style: AtaText.small,
          ),
          const SizedBox(height: AtaSpacing.md),
          AtaButton(
            label: l10n.settleDebt,
            icon: AtaIcons.wallet,
            variant: blocked ? AtaButtonVariant.danger : AtaButtonVariant.brand,
            height: AtaSizes.buttonCompact,
            onPressed: () => _settle(context),
          ),
        ],
      ),
    );
  }

  /// Opens the driver top-up, then reloads the debt.
  Future<void> _settle(BuildContext context) async {
    final PayoutSummaryCubit cubit = context.read<PayoutSummaryCubit>();
    await context.push<double>(AppRoutes.driverTopUpFor(summary.cashDebt));
    await cubit.load();
  }
}
