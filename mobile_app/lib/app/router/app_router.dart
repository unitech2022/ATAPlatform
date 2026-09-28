import 'package:ata_app/app/router/app_redirect.dart';
import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/app/router/router_refresh.dart';
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
import 'package:ata_app/features/driver_dashboard/presentation/pages/driver_dashboard_page.dart';
import 'package:ata_app/features/driver_onboarding/presentation/pages/driver_pending_page.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/rides/presentation/pages/rides_page.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_page.dart';
import 'package:ata_app/features/wallet/presentation/pages/top_up_page.dart';
import 'package:ata_app/features/wallet/presentation/pages/wallet_page.dart';
import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// Builds the app router bound to the [SessionCubit].
GoRouter createAppRouter(SessionCubit session) {
  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: StreamRefreshListenable(session.stream),
    redirect: (BuildContext context, GoRouterState state) =>
        resolveRedirect(session.state, state.matchedLocation),
    routes: <RouteBase>[
      GoRoute(path: AppRoutes.splash, builder: (_, _) => const SplashPage()),
      ..._authRoutes,
      GoRoute(
        path: AppRoutes.driverPending,
        builder: (_, _) => const DriverPendingPage(),
      ),
      GoRoute(
        path: AppRoutes.driver,
        builder: (_, _) => const DriverDashboardPage(),
      ),
      ShellRoute(
        builder: (BuildContext context, GoRouterState state, Widget child) =>
            PassengerShell(location: state.matchedLocation, child: child),
        routes: <RouteBase>[
          GoRoute(path: AppRoutes.home, builder: (_, _) => const HomePage()),
          GoRoute(path: AppRoutes.rides, builder: (_, _) => const RidesPage()),
          GoRoute(
            path: AppRoutes.wallet,
            builder: (_, _) => const WalletPage(),
            routes: <RouteBase>[
              GoRoute(path: 'top-up', builder: (_, _) => const TopUpPage()),
            ],
          ),
          GoRoute(
            path: AppRoutes.safety,
            builder: (_, _) => const SafetyPage(),
          ),
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
            ],
          ),
        ],
      ),
    ],
  );
}

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
