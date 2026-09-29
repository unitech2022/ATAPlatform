import 'package:ata_app/app/router/app_redirect.dart';
import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/app/router/driver_routes.dart';
import 'package:ata_app/app/router/rewards_routes.dart';
import 'package:ata_app/app/router/router_refresh.dart';
import 'package:ata_app/app/router/safety_routes.dart';
import 'package:ata_app/app/shell/passenger_shell.dart';
import 'package:ata_app/app/splash_page.dart';
import 'package:ata_app/features/account/presentation/pages/account_contact_page.dart';
import 'package:ata_app/features/account/presentation/pages/account_language_page.dart';
import 'package:ata_app/features/account/presentation/pages/account_page.dart';
import 'package:ata_app/features/account/presentation/pages/account_terms_page.dart';
import 'package:ata_app/features/account/presentation/pages/delete_account_page.dart';
import 'package:ata_app/features/account/presentation/pages/notification_prefs_page.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/pages/language_page.dart';
import 'package:ata_app/features/auth/presentation/pages/otp_page.dart';
import 'package:ata_app/features/auth/presentation/pages/otp_page_args.dart';
import 'package:ata_app/features/auth/presentation/pages/phone_page.dart';
import 'package:ata_app/features/auth/presentation/pages/role_page.dart';
import 'package:ata_app/features/auth/presentation/pages/terms_page.dart';
import 'package:ata_app/features/favorite_drivers/presentation/pages/favorite_drivers_page.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/payments/presentation/pages/add_card_page.dart';
import 'package:ata_app/features/payments/presentation/pages/payment_methods_page.dart';
import 'package:ata_app/features/payments/presentation/pages/receipt_page.dart';
import 'package:ata_app/features/rides/presentation/pages/rides_page.dart';
import 'package:ata_app/features/safety/presentation/pages/lost_item_page.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:ata_app/features/trip/presentation/pages/active_trip_page.dart';
import 'package:ata_app/features/trip/presentation/pages/reliability_page.dart';
import 'package:ata_app/features/trip_chat/presentation/pages/trip_chat_page.dart';
import 'package:ata_app/features/wallet/presentation/pages/top_up_page.dart';
import 'package:ata_app/features/wallet/presentation/pages/wallet_page.dart';
import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// Builds the app router bound to the [SessionCubit] and the trip cubits
/// (active trip / offer redirects).
GoRouter createAppRouter(SessionCubit session, TripCubits trips) {
  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: StreamRefreshListenable.merge(<Stream<Object?>>[
      session.stream,
      trips.changes,
    ]),
    redirect: (BuildContext context, GoRouterState state) => resolveRedirect(
      session.state,
      state.matchedLocation,
      presence: trips.presence,
    ),
    // Unknown locations (deep links to features not shipped yet) fall back
    // to the rider home; the redirect sends drivers on to /driver.
    onException: (_, _, GoRouter router) => router.go(AppRoutes.home),
    routes: <RouteBase>[
      GoRoute(path: AppRoutes.splash, builder: (_, _) => const SplashPage()),
      ..._authRoutes,
      ...driverRoutes,
      ShellRoute(
        builder: (BuildContext context, GoRouterState state, Widget child) =>
            PassengerShell(location: state.matchedLocation, child: child),
        routes: <RouteBase>[
          GoRoute(
            path: AppRoutes.home,
            builder: (BuildContext context, GoRouterState state) => HomePage(
              promoCode: state.uri.queryParameters[AppRoutes.promoParam],
            ),
          ),
          GoRoute(
            path: AppRoutes.trip,
            builder: (_, _) => const ActiveTripPage(),
            routes: <RouteBase>[
              GoRoute(
                path: 'chat',
                builder: (_, _) =>
                    const TripChatPage(actor: TripActor.passenger),
              ),
            ],
          ),
          GoRoute(
            path: AppRoutes.rides,
            builder: (_, _) => const RidesPage(),
            routes: <RouteBase>[
              GoRoute(
                path: ':${AppRoutes.tripIdParam}',
                builder: _receipt,
                routes: <RouteBase>[
                  GoRoute(path: 'receipt', builder: _receipt),
                  GoRoute(
                    path: 'lost-item',
                    builder: (BuildContext context, GoRouterState state) =>
                        LostItemPage(
                          tripId:
                              state.pathParameters[AppRoutes.tripIdParam] ?? '',
                        ),
                  ),
                ],
              ),
            ],
          ),
          GoRoute(
            path: AppRoutes.wallet,
            builder: (_, _) => const WalletPage(),
            routes: <RouteBase>[
              GoRoute(path: 'top-up', builder: (_, _) => const TopUpPage()),
              GoRoute(
                path: 'payment-methods',
                builder: (_, _) => const PaymentMethodsPage(),
                routes: <RouteBase>[
                  GoRoute(path: 'add', builder: (_, _) => const AddCardPage()),
                ],
              ),
            ],
          ),
          safetyRoute,
          ...riderRewardsRoutes,
          GoRoute(
            path: AppRoutes.account,
            builder: (_, _) => const AccountPage(),
            routes: <RouteBase>[
              GoRoute(
                path: 'notifications',
                builder: (_, _) => const NotificationPrefsPage(),
              ),
              GoRoute(
                path: 'language',
                builder: (_, _) => const AccountLanguagePage(),
              ),
              GoRoute(
                path: 'contact',
                builder: (_, _) => const AccountContactPage(),
              ),
              GoRoute(
                path: 'terms',
                builder: (_, _) => const AccountTermsPage(),
              ),
              GoRoute(
                path: 'delete',
                builder: (_, _) => const DeleteAccountPage(),
              ),
              GoRoute(
                path: 'reliability',
                builder: (_, _) => const ReliabilityPage(),
              ),
              GoRoute(
                path: 'favorite-drivers',
                builder: (_, _) => const FavoriteDriversPage(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
}

/// `/rides/:tripId` and `/rides/:tripId/receipt` both show the receipt.
Widget _receipt(BuildContext context, GoRouterState state) =>
    ReceiptPage(tripId: state.pathParameters[AppRoutes.tripIdParam] ?? '');

List<RouteBase> get _authRoutes => <RouteBase>[
  GoRoute(path: AppRoutes.language, builder: (_, _) => const LanguagePage()),
  GoRoute(path: AppRoutes.role, builder: (_, _) => const RolePage()),
  GoRoute(
    path: AppRoutes.phone,
    builder: (BuildContext context, GoRouterState state) => PhonePage(
      role:
          UserRole.tryParse(state.uri.queryParameters[AppRoutes.roleParam]) ??
          UserRole.passenger,
    ),
  ),
  GoRoute(
    path: AppRoutes.otp,
    redirect: (BuildContext context, GoRouterState state) =>
        state.extra is OtpPageArgs ? null : AppRoutes.role,
    builder: (BuildContext context, GoRouterState state) =>
        OtpPage(args: state.extra! as OtpPageArgs),
  ),
  GoRoute(path: AppRoutes.terms, builder: (_, _) => const TermsPage()),
];
