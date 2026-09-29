import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_state.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/rewards_text.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/tier_card.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/tier` (`ata://driver/tier`): tier, next-tier criteria, benefits
/// and the next weekly recalculation.
class DriverTierPage extends StatelessWidget {
  const DriverTierPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<DriverTierCubit>(
      create: (_) => DriverTierCubit(getTier: getIt())..load(),
      child: DriverSubpage.content(
        eyebrow: l10n.driverAccountEyebrow,
        title: l10n.tierTitle,
        copy: l10n.tierCopy,
        children: <Widget>[
          BlocBuilder<DriverTierCubit, DriverTierState>(
            builder: (BuildContext context, DriverTierState state) {
              final DriverTierInfo? info = state.info;
              if (info != null) return _Details(info: info);
              if (state.failure != null) {
                return FailureView(
                  failure: state.failure!,
                  onRetry: context.read<DriverTierCubit>().load,
                );
              }
              return const CenteredLoader();
            },
          ),
        ],
      ),
    );
  }
}

class _Details extends StatelessWidget {
  const _Details({required this.info});

  final DriverTierInfo info;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? recalc = info.recalculatesAt;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        TierSummary(info: info),
        if (!info.isTopTier) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          AtaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(
                  l10n.tierRequirementsTitle(
                    RewardsText.tier(l10n, info.nextTier!),
                    info.periodDays,
                  ),
                  style: AtaText.section,
                ),
                const SizedBox(height: AtaSpacing.sm),
                for (final TierCheck check in info.checks) _CheckRow(check),
              ],
            ),
          ),
        ],
        const SizedBox(height: AtaSpacing.md),
        Text(
          recalc == null
              ? l10n.tierRecalcWeekly
              : l10n.tierRecalcAt(
                  DateText.longDate(recalc, context.localeCode),
                ),
          style: AtaText.caption,
          textAlign: TextAlign.center,
        ),
      ],
    );
  }
}

class _CheckRow extends StatelessWidget {
  const _CheckRow(this.check);

  final TierCheck check;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              AtaIcon(
                check.met ? AtaIcons.check : AtaIcons.clock,
                size: AtaSizes.iconSmall,
                color: check.met ? AtaColors.brand : AtaColors.muted,
              ),
              const SizedBox(width: AtaSpacing.xs),
              Expanded(
                child: Text(
                  RewardsText.criterion(context.l10n, check.criterion),
                  style: AtaText.label,
                ),
              ),
              Text(
                RewardsText.checkValue(check),
                style: AtaText.captionStrong,
                textDirection: TextDirection.ltr,
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.xxs),
          ProgressBar(value: check.progress, track: AtaColors.cloud),
        ],
      ),
    );
  }
}
