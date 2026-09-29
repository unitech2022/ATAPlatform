import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/driver_rewards/presentation/pages/driver_tier_page.dart';
import 'package:ata_app/features/driver_rewards/presentation/pages/incentive_detail_page.dart';
import 'package:ata_app/features/driver_rewards/presentation/pages/incentives_page.dart';
import 'package:ata_app/features/promotions/presentation/pages/promotions_page.dart';
import 'package:ata_app/features/rating/presentation/pages/driver_ratings_page.dart';
import 'package:ata_app/features/rating/presentation/pages/rate_trip_page.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// F15 rider routes (inside the passenger shell): `/rate/:tripId` and
/// `/promotions`.
List<RouteBase> get riderRewardsRoutes => <RouteBase>[
  GoRoute(
    path: '${AppRoutes.rate}/:${AppRoutes.tripIdParam}',
    builder: (BuildContext context, GoRouterState state) => RateTripPage(
      tripId: state.pathParameters[AppRoutes.tripIdParam] ?? '',
      rater: TripActor.passenger,
    ),
  ),
  GoRoute(
    path: AppRoutes.promotions,
    builder: (_, _) => const PromotionsPage(),
  ),
];

/// F15 driver routes: tier, incentives, ratings summary and rating.
List<RouteBase> get driverRewardsRoutes => <RouteBase>[
  GoRoute(
    path: AppRoutes.driverTier,
    builder: (_, _) => const DriverTierPage(),
  ),
  GoRoute(
    path: AppRoutes.driverIncentives,
    builder: (_, _) => const IncentivesPage(),
    routes: <RouteBase>[
      GoRoute(
        path: ':${AppRoutes.incentiveIdParam}',
        builder: (BuildContext context, GoRouterState state) =>
            IncentiveDetailPage(
              incentiveId:
                  state.pathParameters[AppRoutes.incentiveIdParam] ?? '',
            ),
      ),
    ],
  ),
  GoRoute(
    path: AppRoutes.driverRatings,
    builder: (_, _) => const DriverRatingsPage(),
  ),
  GoRoute(
    path: '${AppRoutes.driverRate}/:${AppRoutes.tripIdParam}',
    builder: (BuildContext context, GoRouterState state) => RateTripPage(
      tripId: state.pathParameters[AppRoutes.tripIdParam] ?? '',
      rater: TripActor.driver,
    ),
  ),
];
