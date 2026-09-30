import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/airport/presentation/pages/airport_queue_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/driver_scheduled_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/reservation_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/scheduled_trip_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/scheduled_trips_page.dart';
import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// F17 rider routes (inside the passenger shell): `/scheduled` and
/// `/scheduled/:tripId`.
List<RouteBase> get riderScheduledRoutes => <RouteBase>[
  GoRoute(
    path: AppRoutes.scheduled,
    builder: (_, _) => const ScheduledTripsPage(),
    routes: <RouteBase>[
      GoRoute(
        path: ':${AppRoutes.tripIdParam}',
        builder: (BuildContext context, GoRouterState state) =>
            ScheduledTripPage(
              tripId: state.pathParameters[AppRoutes.tripIdParam] ?? '',
            ),
      ),
    ],
  ),
];

/// F17 driver routes: marketplace / reservations, a reservation and the
/// airport queue.
List<RouteBase> get driverScheduledRoutes => <RouteBase>[
  GoRoute(
    path: AppRoutes.driverScheduled,
    builder: (_, _) => const DriverScheduledPage(),
    routes: <RouteBase>[
      GoRoute(
        path: ':${AppRoutes.tripIdParam}',
        builder: (BuildContext context, GoRouterState state) => ReservationPage(
          tripId: state.pathParameters[AppRoutes.tripIdParam] ?? '',
        ),
      ),
    ],
  ),
  GoRoute(
    path: AppRoutes.driverAirportQueue,
    builder: (_, _) => const AirportQueuePage(),
  ),
];
