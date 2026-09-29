import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_state.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/incentive_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Overview: the active quest closest to its target, plus the link to all
/// incentives and to the ratings summary.
class NearestIncentiveCard extends StatelessWidget {
  const NearestIncentiveCard({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<IncentivesCubit, IncentivesState>(
      builder: (BuildContext context, IncentivesState state) {
        final Incentive? nearest = state.nearest;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            if (nearest != null) ...<Widget>[
              Text(l10n.incentiveNearest, style: AtaText.section),
              const SizedBox(height: AtaSpacing.sm),
              IncentiveTile(
                incentive: nearest,
                multiplier: state.multiplier,
                onTap: () =>
                    context.push(AppRoutes.driverIncentive(nearest.id)),
              ),
              const SizedBox(height: AtaSpacing.sm),
            ],
            SettingRow(
              leading: const IconBox.cloud(icon: AtaIcons.gift),
              title: l10n.incentivesTitle,
              subtitle: l10n.incentivesLinkCopy,
              onTap: () => context.push(AppRoutes.driverIncentives),
            ),
            SettingRow(
              leading: const IconBox.cloud(icon: AtaIcons.star),
              title: l10n.driverRatingsTitle,
              subtitle: l10n.driverRatingsCopy,
              last: true,
              onTap: () => context.push(AppRoutes.driverRatings),
            ),
          ],
        );
      },
    );
  }
}
