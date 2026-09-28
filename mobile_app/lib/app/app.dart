import 'package:ata_app/design/theme/ata_theme.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Root widget: provides the app-wide cubits and the localized
/// [MaterialApp.router]. Text direction follows the locale automatically.
/// Deep links (tapped pushes, inbox rows) are navigated from here.
class AtaApp extends StatelessWidget {
  const AtaApp({
    super.key,
    required this.sessionCubit,
    required this.localeCubit,
    required this.tripCubits,
    required this.deepLinkCubit,
    required this.router,
  });

  final SessionCubit sessionCubit;
  final LocaleCubit localeCubit;
  final TripCubits tripCubits;
  final DeepLinkCubit deepLinkCubit;
  final GoRouter router;

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<SessionCubit>.value(value: sessionCubit),
        BlocProvider<LocaleCubit>.value(value: localeCubit),
        BlocProvider<DeepLinkCubit>.value(value: deepLinkCubit),
      ],
      child: tripCubits.provide(
        child: BlocListener<DeepLinkCubit, DeepLinkState>(
          listenWhen: (DeepLinkState p, DeepLinkState c) =>
              c.target != null && p.target != c.target,
          listener: (BuildContext context, DeepLinkState state) {
            router.go(state.target!.route);
            deepLinkCubit.consumed();
          },
          child: _localizedApp(),
        ),
      ),
    );
  }

  Widget _localizedApp() => BlocBuilder<LocaleCubit, Locale>(
    builder: (BuildContext context, Locale locale) {
      return MaterialApp.router(
        title: 'ATA',
        debugShowCheckedModeBanner: false,
        theme: AtaTheme.light(),
        locale: locale,
        supportedLocales: LocaleCubit.supported,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        routerConfig: router,
      );
    },
  );
}
