import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/safety/presentation/pages/lost_items_page.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_cases_page.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_check_page.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_page.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_report_page.dart';
import 'package:ata_app/features/safety/presentation/pages/trusted_contacts_page.dart';
import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// Rider `/safety` and its F12 sub-pages (inside the passenger shell).
GoRoute get safetyRoute => GoRoute(
  path: AppRoutes.safety,
  builder: (_, _) => const SafetyPage(),
  routes: <RouteBase>[
    GoRoute(path: 'contacts', builder: (_, _) => const TrustedContactsPage()),
    GoRoute(
      path: 'report',
      builder: (BuildContext context, GoRouterState state) => SafetyReportPage(
        tripId: state.uri.queryParameters[AppRoutes.tripIdParam] ?? '',
      ),
    ),
    GoRoute(
      path: 'cases',
      builder: (_, _) => const SafetyCasesPage(),
      routes: <RouteBase>[
        GoRoute(
          path: ':${AppRoutes.caseIdParam}',
          builder: (BuildContext context, GoRouterState state) =>
              SafetyCasesPage(
                caseId: state.pathParameters[AppRoutes.caseIdParam],
              ),
        ),
      ],
    ),
    GoRoute(
      path: 'check/:${AppRoutes.alertIdParam}',
      builder: (BuildContext context, GoRouterState state) => SafetyCheckPage(
        alertId: state.pathParameters[AppRoutes.alertIdParam] ?? '',
      ),
    ),
    GoRoute(path: 'lost-items', builder: (_, _) => const LostItemsPage()),
  ],
);
