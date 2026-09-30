import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/support/presentation/pages/help_article_page.dart';
import 'package:ata_app/features/support/presentation/pages/new_ticket_page.dart';
import 'package:ata_app/features/support/presentation/pages/support_page.dart';
import 'package:ata_app/features/support/presentation/pages/ticket_thread_page.dart';
import 'package:ata_app/features/support/presentation/pages/tickets_page.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// F18 support routes of one role: the help center, articles, "تذاكري",
/// the new ticket form (declared before `:ticketId`) and a ticket thread.
/// The rider copy (`/support`) sits inside the passenger shell, the driver
/// copy lives at `/driver/support`.
GoRoute _supportRoute(String path, TripActor actor) => GoRoute(
  path: path,
  builder: (_, _) => SupportPage(actor: actor),
  routes: <RouteBase>[
    GoRoute(
      path: 'articles/:${AppRoutes.slugParam}',
      builder: (BuildContext context, GoRouterState state) => HelpArticlePage(
        slug: state.pathParameters[AppRoutes.slugParam] ?? '',
        actor: actor,
      ),
    ),
    GoRoute(
      path: 'tickets',
      builder: (_, _) => TicketsPage(actor: actor),
      routes: <RouteBase>[
        GoRoute(
          path: 'new',
          builder: (BuildContext context, GoRouterState state) => NewTicketPage(
            actor: actor,
            type: state.uri.queryParameters[AppRoutes.ticketTypeParam],
            tripId: state.uri.queryParameters[AppRoutes.tripIdParam],
            dispute: state.uri.queryParameters[AppRoutes.disputeParam] == '1',
          ),
        ),
        GoRoute(
          path: ':${AppRoutes.ticketIdParam}',
          builder: (BuildContext context, GoRouterState state) =>
              TicketThreadPage(
                ticketId: state.pathParameters[AppRoutes.ticketIdParam] ?? '',
                actor: actor,
              ),
        ),
      ],
    ),
  ],
);

/// `/support/*` (inside the passenger shell).
GoRoute get riderSupportRoute =>
    _supportRoute(AppRoutes.support, TripActor.passenger);

/// `/driver/support/*`.
GoRoute get driverSupportRoute =>
    _supportRoute(AppRoutes.driverSupport, TripActor.driver);
