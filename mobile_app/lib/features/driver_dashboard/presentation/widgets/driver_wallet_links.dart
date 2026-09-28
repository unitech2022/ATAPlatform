import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/cash_debt_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Cash-debt warning (when money is owed) and links to the earnings
/// statement and the payouts.
class DriverWalletLinks extends StatelessWidget {
  const DriverWalletLinks({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<PayoutSummaryCubit, PayoutSummaryState>(
      builder: (BuildContext context, PayoutSummaryState state) {
        final PayoutSummary? summary = state.summary;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            if (summary != null && summary.hasCashDebt) ...<Widget>[
              CashDebtCard(summary: summary),
              const SizedBox(height: AtaSpacing.xl),
            ],
            AtaCard(
              padding: const EdgeInsets.all(AtaSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  Text(l10n.driverWalletEyebrow, style: AtaText.section),
                  SettingRow(
                    leading: const IconBox.cloud(icon: AtaIcons.document),
                    title: l10n.earningsStatementTitle,
                    subtitle: l10n.earningsStatementCopy,
                    onTap: () => context.push(AppRoutes.driverEarnings),
                  ),
                  SettingRow(
                    leading: const IconBox.cloud(icon: AtaIcons.wallet),
                    title: l10n.payoutsTitle,
                    subtitle: summary == null
                        ? l10n.payoutsCopy
                        : l10n.payoutAvailableLine(
                            l10n.priceWithCurrency(
                              Money.fixed(summary.availableForPayout),
                            ),
                          ),
                    last: true,
                    onTap: () => context.push(AppRoutes.driverPayouts),
                  ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }
}
