import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_state.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/rewards_text.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/tier_badge.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Overview card: tier badge, progress to the next tier and benefits.
class TierCard extends StatelessWidget {
  const TierCard({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<DriverTierCubit, DriverTierState>(
      builder: (BuildContext context, DriverTierState state) {
        final DriverTierInfo? info = state.info;
        if (info == null) return const SizedBox.shrink();
        return InkWell(
          key: const ValueKey<String>('tier-card'),
          onTap: () => context.push(AppRoutes.driverTier),
          child: TierSummary(info: info),
        );
      },
    );
  }
}

/// Badge, progress bar, remaining trips and benefits of [info].
class TierSummary extends StatelessWidget {
  const TierSummary({super.key, required this.info});

  final DriverTierInfo info;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverTier? next = info.nextTier;
    final double discount = info.benefits.commissionDiscountPercent;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(child: Text(l10n.tierTitle, style: AtaText.section)),
              TierBadge(tier: info.tier),
            ],
          ),
          const SizedBox(height: AtaSpacing.md),
          if (info.isTopTier || next == null)
            Text(l10n.tierTopReached, style: AtaText.small)
          else ...<Widget>[
            Text(
              info.tripsToNext > 0
                  ? l10n.tierTripsToNext(
                      info.tripsToNext,
                      RewardsText.tier(l10n, next),
                    )
                  : l10n.tierProgressTo(RewardsText.tier(l10n, next)),
              style: AtaText.small,
            ),
            const SizedBox(height: AtaSpacing.xs),
            ProgressBar(value: info.progress, track: AtaColors.cloud),
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              l10n.tierChecksMet(info.metCount, info.checks.length),
              style: AtaText.caption,
            ),
          ],
          if (discount > 0 || info.benefits.text.isNotEmpty) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            Text(
              <String>[
                if (discount > 0)
                  l10n.tierCommissionDiscount(Money.compact(discount)),
                if (info.benefits.text.isNotEmpty) info.benefits.text,
              ].join(' · '),
              style: AtaText.captionStrong.copyWith(color: AtaColors.brand),
            ),
          ],
        ],
      ),
    );
  }
}
