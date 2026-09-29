import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_state.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/incentive_tile.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/reduced_reward_notice.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/rewards_text.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/driver/incentives`: active / upcoming / completed quests.
class IncentivesPage extends StatelessWidget {
  const IncentivesPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<IncentivesCubit>(
      create: (_) =>
          IncentivesCubit(getIncentives: getIt(), getReliability: getIt())
            ..load(),
      child: DriverSubpage.content(
        eyebrow: l10n.driverAccountEyebrow,
        title: l10n.incentivesTitle,
        copy: l10n.incentivesCopy,
        children: <Widget>[
          BlocBuilder<IncentivesCubit, IncentivesState>(
            builder: (BuildContext context, IncentivesState state) => Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                _Tabs(state: state),
                const SizedBox(height: AtaSpacing.lg),
                if (state.isReduced) ...<Widget>[
                  ReducedRewardNotice(multiplier: state.multiplier),
                  const SizedBox(height: AtaSpacing.md),
                ],
                ..._list(context, state),
              ],
            ),
          ),
        ],
      ),
    );
  }

  List<Widget> _list(BuildContext context, IncentivesState state) {
    final IncentivesCubit cubit = context.read<IncentivesCubit>();
    if (state.loading && !state.isLoaded) {
      return const <Widget>[CenteredLoader()];
    }
    if (state.failure != null) {
      return <Widget>[
        FailureView(failure: state.failure!, onRetry: cubit.refresh),
      ];
    }
    if (state.isEmpty) {
      return <Widget>[
        Text(
          context.l10n.incentivesEmpty,
          style: AtaText.small,
          textAlign: TextAlign.center,
        ),
      ];
    }
    return <Widget>[
      for (final Incentive i in state.current) ...<Widget>[
        IncentiveTile(
          incentive: i,
          multiplier: state.multiplier,
          onTap: () => context.push(AppRoutes.driverIncentive(i.id)),
        ),
        const SizedBox(height: AtaSpacing.sm),
      ],
    ];
  }
}

class _Tabs extends StatelessWidget {
  const _Tabs({required this.state});

  final IncentivesState state;

  @override
  Widget build(BuildContext context) {
    final IncentivesCubit cubit = context.read<IncentivesCubit>();
    return Row(
      children: <Widget>[
        for (final IncentiveTab tab in IncentiveTab.values) ...<Widget>[
          Expanded(
            child: SelectableTile(
              selected: state.tab == tab,
              onTap: () => cubit.selectTab(tab),
              padding: const EdgeInsets.symmetric(vertical: AtaSpacing.sm),
              child: Text(
                RewardsText.tab(context.l10n, tab),
                textAlign: TextAlign.center,
                style: AtaText.label.copyWith(
                  color: state.tab == tab ? AtaColors.brand : AtaColors.ink,
                ),
              ),
            ),
          ),
          if (tab != IncentiveTab.values.last)
            const SizedBox(width: AtaSpacing.sm),
        ],
      ],
    );
  }
}
