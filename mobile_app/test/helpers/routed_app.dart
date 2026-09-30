import 'package:ata_app/design/theme/ata_theme.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Hosts [child] at `/` in a real [GoRouter] (for pages that call
/// `context.push` / `go`). Every path in [placeholders] renders its own name
/// so a test can tell where the page navigated to.
Widget wrapRouted(
  Widget child, {
  List<String> placeholders = const <String>[
    '/home',
    '/trip',
    '/driver',
    '/driver/scheduled',
    '/driver/trip',
    '/scheduled',
  ],
  Locale locale = const Locale('ar'),
}) {
  final GoRouter router = GoRouter(
    routes: <RouteBase>[
      GoRoute(
        path: '/',
        builder: (_, _) => Scaffold(body: child),
      ),
      for (final String path in placeholders)
        GoRoute(
          path: path,
          builder: (_, GoRouterState state) =>
              Scaffold(body: Center(child: Text('at:${state.uri.path}'))),
        ),
      GoRoute(
        path: '/scheduled/:tripId',
        builder: (_, GoRouterState state) =>
            Scaffold(body: Center(child: Text('at:${state.uri.path}'))),
      ),
      GoRoute(
        path: '/driver/scheduled/:tripId',
        builder: (_, GoRouterState state) =>
            Scaffold(body: Center(child: Text('at:${state.uri.path}'))),
      ),
    ],
  );
  return MaterialApp.router(
    theme: AtaTheme.light(),
    locale: locale,
    supportedLocales: LocaleCubit.supported,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    routerConfig: router,
  );
}
