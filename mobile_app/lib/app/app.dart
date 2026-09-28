import 'package:ata_app/design/theme/ata_theme.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Root widget: provides the app-wide cubits and the localized
/// [MaterialApp.router]. Text direction follows the locale automatically.
class AtaApp extends StatelessWidget {
  const AtaApp({
    super.key,
    required this.sessionCubit,
    required this.localeCubit,
    required this.router,
  });

  final SessionCubit sessionCubit;
  final LocaleCubit localeCubit;
  final GoRouter router;

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<SessionCubit>.value(value: sessionCubit),
        BlocProvider<LocaleCubit>.value(value: localeCubit),
      ],
      child: BlocBuilder<LocaleCubit, Locale>(
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
      ),
    );
  }
}
