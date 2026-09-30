import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/marketplace_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/marketplace_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_tab_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/marketplace_view.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/reservation_notices.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/reservations_view.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/scheduled` ("رحلاتي المجدولة"): the marketplace of upcoming
/// requests and the driver's own reservations.
class DriverScheduledPage extends StatelessWidget {
  const DriverScheduledPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final GeoPoint? position = context
        .read<LocationStreamCubit>()
        .state
        .position
        ?.point;
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<ScheduledTabCubit>(create: (_) => ScheduledTabCubit()),
        BlocProvider<MarketplaceCubit>(
          create: (_) =>
              MarketplaceCubit(getMarketplace: getIt(), reserve: getIt())
                ..load(position: position),
        ),
        BlocProvider<ReservationsCubit>(
          create: (_) => ReservationsCubit(
            getReservations: getIt(),
            confirm: getIt(),
            release: getIt(),
            watchIncoming: getIt(),
          )..load(),
        ),
      ],
      child: MultiBlocListener(
        listeners: <BlocListener<dynamic, dynamic>>[
          BlocListener<MarketplaceCubit, MarketplaceState>(
            listenWhen: (MarketplaceState p, MarketplaceState c) =>
                c.reserved != null && p.reserved != c.reserved,
            listener: (BuildContext context, MarketplaceState state) {
              ScaffoldMessenger.of(
                context,
              ).showSnackBar(SnackBar(content: Text(l10n.marketReserved)));
              context.read<MarketplaceCubit>().clearNotice();
              context.read<ScheduledTabCubit>().select(ScheduledTab.mine);
              context.read<ReservationsCubit>().load(silent: true);
            },
          ),
          BlocListener<ReservationsCubit, ReservationsState>(
            listenWhen: (ReservationsState p, ReservationsState c) =>
                c.event != null && p.event != c.event,
            listener: announceReservationEvent,
          ),
        ],
        child: DriverSubpage.content(
          eyebrow: l10n.driverScheduledEyebrow,
          title: l10n.driverScheduledTitle,
          copy: l10n.driverScheduledCopy,
          children: const <Widget>[_Tabs(), _Body()],
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ScheduledTabCubit, ScheduledTab>(
      builder: (BuildContext context, ScheduledTab tab) =>
          tab == ScheduledTab.market
          ? const MarketplaceView()
          : const ReservationsView(),
    );
  }
}

class _Tabs extends StatelessWidget {
  const _Tabs();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<ScheduledTabCubit, ScheduledTab>(
      builder: (BuildContext context, ScheduledTab selected) => Padding(
        padding: const EdgeInsets.only(bottom: AtaSpacing.lg),
        child: Row(
          children: <Widget>[
            for (final ScheduledTab tab in ScheduledTab.values)
              Expanded(
                child: Padding(
                  padding: const EdgeInsetsDirectional.only(end: AtaSpacing.sm),
                  child: SelectableTile(
                    key: ValueKey<String>('scheduled-tab-${tab.name}'),
                    selected: selected == tab,
                    onTap: () => context.read<ScheduledTabCubit>().select(tab),
                    padding: const EdgeInsets.symmetric(
                      vertical: AtaSpacing.sm,
                    ),
                    child: Text(
                      tab == ScheduledTab.market
                          ? l10n.driverScheduledTabMarket
                          : l10n.driverScheduledTabMine,
                      textAlign: TextAlign.center,
                      style: AtaText.label.copyWith(
                        color: selected == tab
                            ? AtaColors.brand
                            : AtaColors.ink,
                      ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
