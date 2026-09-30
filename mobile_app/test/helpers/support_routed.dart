import 'package:ata_app/app/router/support_routes.dart';
import 'package:ata_app/design/theme/ata_theme.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Hosts the real F18 routes (rider and driver copies) in a [GoRouter]
/// starting at [location]. `/` and the listed [placeholders] only print the
/// location they were opened at.
Widget wrapSupport(
  String location, {
  Locale locale = const Locale('ar'),
  List<String> placeholders = const <String>['/', '/rides/:tripId'],
}) {
  final GoRouter router = GoRouter(
    initialLocation: location,
    routes: <RouteBase>[
      // The rider pages sit inside the passenger shell's Scaffold.
      ShellRoute(
        builder: (_, _, Widget child) => Scaffold(body: child),
        routes: <RouteBase>[riderSupportRoute],
      ),
      driverSupportRoute,
      for (final String path in placeholders)
        GoRoute(
          path: path,
          builder: (_, GoRouterState state) =>
              Scaffold(body: Center(child: Text('at:${state.uri}'))),
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
