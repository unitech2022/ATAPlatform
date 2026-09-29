import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/rewards_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One quest: progress bar, reward (reduced by [multiplier] when < 1),
/// window / zones and period end.
class IncentiveTile extends StatelessWidget {
  const IncentiveTile({
    super.key,
    required this.incentive,
    this.multiplier = 1,
    this.onTap,
  });

  final Incentive incentive;
  final double multiplier;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final Incentive i = incentive;
    final bool reduced = multiplier < 1;
    final double? paid = i.progress?.rewardAmount;
    final List<String> details = <String>[
      ?RewardsText.window(l10n, i.window),
      ?RewardsText.zones(i),
      ?RewardsText.period(l10n, i, context.localeCode),
    ];
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AtaSpacing.xl),
      child: AtaCard(
        padding: const EdgeInsets.all(AtaSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Row(
              children: <Widget>[
                Expanded(child: Text(i.name, style: AtaText.bodyStrong)),
                if (i.type.isNotEmpty)
                  AtaBadge(label: RewardsText.type(l10n, i.type)),
              ],
            ),
            const SizedBox(height: AtaSpacing.sm),
            ProgressBar(
              value: i.progressValue,
              track: AtaColors.cloud,
              fill: i.isAchieved ? AtaColors.brand : AtaColors.warning,
            ),
            const SizedBox(height: AtaSpacing.xs),
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    i.progress == null && i.needsOptIn
                        ? l10n.incentiveJoinFirst
                        : '${l10n.incentiveTripsProgress(i.completedTrips, i.targetTrips)}'
                              ' · ${RewardsText.status(l10n, i.status)}',
                    style: AtaText.caption,
                  ),
                ),
                if (reduced && paid == null)
                  Padding(
                    padding: const EdgeInsetsDirectional.only(
                      end: AtaSpacing.xs,
                    ),
                    child: Text(
                      l10n.priceWithCurrency(Money.compact(i.rewardAmount)),
                      style: AtaText.caption.copyWith(
                        decoration: TextDecoration.lineThrough,
                      ),
                    ),
                  ),
                Text(
                  l10n.priceWithCurrency(
                    Money.compact(paid ?? i.rewardWith(multiplier)),
                  ),
                  style: AtaText.bodyStrong.copyWith(color: AtaColors.brand),
                ),
              ],
            ),
            for (final String line in details)
              Text(line, style: AtaText.caption),
          ],
        ),
      ),
    );
  }
}
