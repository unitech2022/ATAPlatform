import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/decorative_background.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_documents_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_overview_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_tabs_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/documents_tab.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/driver_header.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/driver_hero.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/driver_tabs.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/overview_tab.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/settings_tab.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_cubit.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Approved-driver dashboard: header, greeting + online toggle, tabs. Going
/// online starts the offer feed and the location stream; going offline
/// stops them.
class DriverDashboardPage extends StatelessWidget {
  const DriverDashboardPage({super.key, this.initialTab = DriverTab.overview});

  final DriverTab initialTab;

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<DriverTabsCubit>(
          create: (_) => DriverTabsCubit(initial: initialTab),
        ),
        BlocProvider<PayoutSummaryCubit>(
          create: (_) => PayoutSummaryCubit(getSummary: getIt())..load(),
        ),
        BlocProvider<OnlineStatusCubit>(
          create: (_) =>
              OnlineStatusCubit(getStatus: getIt(), setOnline: getIt())..load(),
        ),
        BlocProvider<DriverOverviewCubit>(
          create: (_) =>
              DriverOverviewCubit(getEarnings: getIt(), getTrips: getIt())
                ..load(),
        ),
        BlocProvider<ReliabilityCubit>(
          create: (_) =>
              ReliabilityCubit(getReliability: getIt(), role: TripActor.driver)
                ..load(),
        ),
        BlocProvider<DriverDocumentsCubit>(
          create: (_) => DriverDocumentsCubit(getApplication: getIt())..load(),
        ),
      ],
      child: BlocListener<OnlineStatusCubit, OnlineStatusState>(
        listenWhen: (OnlineStatusState p, OnlineStatusState c) =>
            p.isOnline != c.isOnline && !c.updating,
        listener: (BuildContext context, OnlineStatusState state) {
          final DriverOfferCubit offers = context.read<DriverOfferCubit>();
          final LocationStreamCubit location = context
              .read<LocationStreamCubit>();
          if (state.isOnline) {
            offers.start();
            location.start();
          } else {
            offers.stop();
            location.stop();
          }
        },
        child: Scaffold(
          body: PageBackground(
            child: SafeArea(
              child: Column(
                children: <Widget>[
                  const DriverHeader(),
                  Expanded(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.fromLTRB(
                        AtaSpacing.gutter,
                        AtaSpacing.xxl,
                        AtaSpacing.gutter,
                        AtaSpacing.xxxl,
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: <Widget>[
                          const DriverHero(),
                          const SizedBox(height: AtaSpacing.xl),
                          const DriverTabs(),
                          const SizedBox(height: AtaSpacing.xl),
                          BlocBuilder<DriverTabsCubit, DriverTab>(
                            builder: (BuildContext context, DriverTab tab) =>
                                switch (tab) {
                                  DriverTab.overview => const OverviewTab(),
                                  DriverTab.documents => const DocumentsTab(),
                                  DriverTab.settings => const SettingsTab(),
                                },
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  static String greetingName(BuildContext context, String? fullName) =>
      fullName?.trim().split(' ').first ?? context.l10n.driverGuestName;
}
