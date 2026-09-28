import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/stat_card.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_overview_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_overview_state.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/driver_wallet_links.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/earnings_card.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/recent_trips_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Stat cards, recent trips and the weekly earnings card.
class OverviewTab extends StatelessWidget {
  const OverviewTab({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<DriverOverviewCubit, DriverOverviewState>(
      builder: (BuildContext context, DriverOverviewState state) {
        final EarningsSummary e = state.earnings;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            if (state.failure != null) ...<Widget>[
              FailureView(
                failure: state.failure!,
                onRetry: context.read<DriverOverviewCubit>().load,
              ),
              const SizedBox(height: AtaSpacing.md),
            ],
            Row(
              children: <Widget>[
                Expanded(
                  child: StatCard(
                    icon: AtaIcons.wallet,
                    title: l10n.statEarningsToday,
                    value: l10n.priceWithCurrency(
                      Money.compact(e.todayEarnings),
                    ),
                  ),
                ),
                const SizedBox(width: AtaSpacing.md),
                Expanded(
                  child: StatCard(
                    icon: AtaIcons.car,
                    title: l10n.statTrips,
                    value: '${e.todayTrips}',
                    meta: l10n.statTripsMeta,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AtaSpacing.md),
            Row(
              children: <Widget>[
                Expanded(
                  child: StatCard(
                    icon: AtaIcons.clock,
                    title: l10n.statHours,
                    value: Money.compact(e.todayOnlineHours),
                    meta: l10n.statHoursMeta,
                  ),
                ),
                const SizedBox(width: AtaSpacing.md),
                Expanded(
                  child: StatCard(
                    icon: AtaIcons.shield,
                    title: l10n.statRating,
                    value: Money.compact(e.ratingAvg),
                    meta: l10n.statRatingMeta,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AtaSpacing.xl),
            RecentTripsCard(trips: state.trips, loading: state.loading),
            const SizedBox(height: AtaSpacing.xl),
            EarningsCard(earnings: e),
            const SizedBox(height: AtaSpacing.xl),
            const DriverWalletLinks(),
          ],
        );
      },
    );
  }
}
