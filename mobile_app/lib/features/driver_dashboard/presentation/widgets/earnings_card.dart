import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/money_text.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Dark weekly-earnings card with progress toward the target and the
/// "transfer earnings" button (payout request, F11).
class EarningsCard extends StatelessWidget {
  const EarningsCard({super.key, required this.earnings});

  final EarningsSummary earnings;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return DarkCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              const IconBox.brand(
                icon: AtaIcons.wallet,
                size: AtaSizes.iconBox + 4,
                iconSize: AtaSizes.iconMedium,
              ),
              AtaBadge(label: l10n.thisWeek, background: AtaColors.white10),
            ],
          ),
          const SizedBox(height: AtaSpacing.xl),
          Text(
            l10n.totalEarnings,
            style: AtaText.small.copyWith(color: AtaColors.white60),
          ),
          const SizedBox(height: AtaSpacing.xs),
          MoneyText(
            amount: Money.compact(earnings.weekEarnings),
            currency: l10n.currency,
            style: AtaText.display.copyWith(color: AtaColors.white),
          ),
          const SizedBox(height: AtaSpacing.xl),
          ProgressBar(value: earnings.weekProgress),
          const SizedBox(height: AtaSpacing.sm),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Text(
                l10n.weeklyTarget,
                style: AtaText.caption.copyWith(color: AtaColors.white60),
              ),
              Text(
                l10n.priceWithCurrency(Money.compact(earnings.weekTarget)),
                style: AtaText.caption.copyWith(color: AtaColors.white60),
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.xl),
          AtaButton(
            label: l10n.transferEarnings,
            variant: AtaButtonVariant.white,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.push(AppRoutes.driverPayoutRequest),
          ),
        ],
      ),
    );
  }
}
