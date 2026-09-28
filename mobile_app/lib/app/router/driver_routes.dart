import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_tabs_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/pages/driver_dashboard_page.dart';
import 'package:ata_app/features/driver_onboarding/presentation/pages/driver_pending_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/driver_earnings_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/driver_payouts_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/payout_request_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/trip/presentation/pages/driver_offer_page.dart';
import 'package:ata_app/features/trip/presentation/pages/driver_trip_page.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/presentation/pages/top_up_page.dart';
import 'package:flutter/widgets.dart';
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
];
