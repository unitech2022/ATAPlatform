import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/app/router/rewards_routes.dart';
import 'package:ata_app/app/router/scheduled_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_tabs_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/pages/driver_dashboard_page.dart';
import 'package:ata_app/features/driver_onboarding/presentation/pages/driver_pending_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/driver_earnings_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/driver_payouts_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/payout_request_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/safety/presentation/pages/driver_lost_items_page.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/pages/driver_offer_page.dart';
import 'package:ata_app/features/trip/presentation/pages/driver_trip_page.dart';
import 'package:ata_app/features/trip/presentation/widgets/reliability_details.dart';
import 'package:ata_app/features/trip_chat/presentation/pages/trip_chat_page.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/presentation/pages/top_up_page.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Driver routes (outside the passenger shell).
List<RouteBase> get driverRoutes => <RouteBase>[
  GoRoute(
    path: AppRoutes.driverPending,
    builder: (_, _) => const DriverPendingPage(),
  ),
  GoRoute(
    path: AppRoutes.driver,
    builder: (BuildContext context, GoRouterState state) => DriverDashboardPage(
      initialTab: DriverTab.parse(
        state.uri.queryParameters[AppRoutes.tabParam],
      ),
    ),
  ),
  GoRoute(
    path: AppRoutes.driverOffer,
    builder: (_, _) => const DriverOfferPage(),
  ),
  GoRoute(
    path: AppRoutes.driverTrip,
    builder: (_, _) => const DriverTripPage(),
    routes: <RouteBase>[
      GoRoute(
        path: 'chat',
        builder: (_, _) => const Scaffold(
          body: SafeArea(child: TripChatPage(actor: TripActor.driver)),
        ),
      ),
    ],
  ),
  GoRoute(
    path: AppRoutes.driverReliability,
    builder: (BuildContext context, GoRouterState state) =>
        DriverSubpage.content(
          eyebrow: context.l10n.driverAccountEyebrow,
          title: context.l10n.reliabilityTitle,
          copy: context.l10n.reliabilityCopy,
          children: const <Widget>[ReliabilityDetails(role: TripActor.driver)],
        ),
  ),
  GoRoute(
    path: AppRoutes.driverLostItems,
    builder: (_, _) => const DriverLostItemsPage(),
    routes: <RouteBase>[
      GoRoute(
        path: ':${AppRoutes.reportIdParam}',
        builder: (BuildContext context, GoRouterState state) =>
            DriverLostItemsPage(
              reportId: state.pathParameters[AppRoutes.reportIdParam],
            ),
      ),
    ],
  ),
  GoRoute(
    path: AppRoutes.driverEarnings,
    builder: (_, _) => const DriverEarningsPage(),
  ),
  GoRoute(
    path: AppRoutes.driverPayouts,
    builder: (_, _) => const DriverPayoutsPage(),
  ),
  GoRoute(
    path: AppRoutes.driverPayoutRequest,
    builder: (_, _) => const PayoutRequestPage(),
  ),
  GoRoute(
    path: AppRoutes.driverTopUp,
    builder: (BuildContext context, GoRouterState state) => DriverSubpage(
      child: TopUpPage(
        kind: WalletKind.driver,
        initialAmount: double.tryParse(
          state.uri.queryParameters[AppRoutes.amountParam] ?? '',
        ),
      ),
    ),
  ),
  ...driverRewardsRoutes,
  ...driverScheduledRoutes,
];
