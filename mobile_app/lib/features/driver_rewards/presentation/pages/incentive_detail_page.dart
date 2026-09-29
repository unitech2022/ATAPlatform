import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentive_detail_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentive_detail_state.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/incentive_tile.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/reduced_reward_notice.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/rewards_text.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/incentives/:id` (`ata://driver/incentives/{id}`): progress,
/// window, zones, categories and opt-in.
class IncentiveDetailPage extends StatelessWidget {
  const IncentiveDetailPage({super.key, required this.incentiveId});

  final String incentiveId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<IncentiveDetailCubit>(
      create: (_) => IncentiveDetailCubit(
        incentiveId: incentiveId,
        getIncentive: getIt(),
        optIn: getIt(),
        getReliability: getIt(),
      )..load(),
      child: DriverSubpage.content(
        eyebrow: l10n.incentivesTitle,
        title: l10n.incentiveDetailTitle,
        children: <Widget>[
          BlocBuilder<IncentiveDetailCubit, IncentiveDetailState>(
            builder: (BuildContext context, IncentiveDetailState state) {
              final Incentive? incentive = state.incentive;
              if (incentive != null) {
                return _Body(incentive: incentive, state: state);
              }
              if (state.failure != null) {
                return FailureView(
                  failure: state.failure!,
                  onRetry: context.read<IncentiveDetailCubit>().load,
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

class _Body extends StatelessWidget {
  const _Body({required this.incentive, required this.state});

  final Incentive incentive;
  final IncentiveDetailState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<(String, String)> rows = <(String, String)>[
      (l10n.incentiveTarget, l10n.incentiveTripsCount(incentive.targetTrips)),
      if (RewardsText.window(l10n, incentive.window) != null)
        (l10n.incentiveWindow, RewardsText.window(l10n, incentive.window)!),
      (
        l10n.incentiveZones,
        RewardsText.zones(incentive) ?? l10n.incentiveAllZones,
      ),
      (
        l10n.incentiveCategories,
        incentive.rideCategoryCodes?.join('، ') ?? l10n.incentiveAllCategories,
      ),
      if (RewardsText.period(l10n, incentive, context.localeCode) != null)
        (
          l10n.incentivePeriod,
          RewardsText.period(l10n, incentive, context.localeCode)!,
        ),
    ];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        if (state.isReduced) ...<Widget>[
          ReducedRewardNotice(multiplier: state.multiplier),
          const SizedBox(height: AtaSpacing.md),
        ],
        IncentiveTile(incentive: incentive, multiplier: state.multiplier),
        if (incentive.description.isNotEmpty) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          Text(incentive.description, style: AtaText.body),
        ],
        const SizedBox(height: AtaSpacing.md),
        AtaCard(
          child: Column(
            children: <Widget>[
              for (final (String label, String value) in rows)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Expanded(child: Text(label, style: AtaText.small)),
                      Flexible(child: Text(value, style: AtaText.label)),
                    ],
                  ),
                ),
            ],
          ),
        ),
        if (state.actionFailure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          InlineError(message: failureText(state.actionFailure!, l10n)),
        ],
        if (incentive.needsOptIn) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          AtaButton(
            label: l10n.incentiveOptIn,
            variant: AtaButtonVariant.brand,
            loading: state.joining,
            onPressed: context.read<IncentiveDetailCubit>().optIn,
          ),
        ] else if (incentive.requiresOptIn) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          Text(
            l10n.incentiveJoined,
            style: AtaText.captionStrong,
            textAlign: TextAlign.center,
          ),
        ],
      ],
    );
  }
}
