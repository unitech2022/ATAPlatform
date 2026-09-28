import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/app/shell/passenger_header.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/bottom_nav.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_state.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_cubit.dart';
import 'package:ata_app/features/notifications/presentation/widgets/notifications_sheet.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Rider chrome: header on top, page in the middle, floating bottom
/// navigation (hidden on the map pages: home and the active trip).
class PassengerShell extends StatelessWidget {
  const PassengerShell({
    super.key,
    required this.location,
    required this.child,
  });

  final String location;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool isHome = AppRoutes.isMapPage(location);
    final bool hideNav = AppRoutes.hidesBottomNav(location);
    final int? tabIndex = AppRoutes.tabIndexFor(location);
    return BlocProvider<NotificationsCubit>(
      create: (_) =>
          NotificationsCubit(
              getNotifications: getIt(),
              markRead: getIt(),
              watchIncoming: getIt(),
            )
            ..load()
            ..watchPushes(),
      child: BlocListener<DeepLinkCubit, DeepLinkState>(
        // `ata://notifications` opens the inbox on top of the page.
        listenWhen: (DeepLinkState p, DeepLinkState c) =>
            !p.inboxRequested && c.inboxRequested,
        listener: (BuildContext context, _) {
          context.read<DeepLinkCubit>().inboxShown();
          showNotificationsSheet(context);
        },
        child: _scaffold(context, l10n, isHome, hideNav, tabIndex),
      ),
    );
  }

  Widget _scaffold(
    BuildContext context,
    AppLocalizations l10n,
    bool isHome,
    bool hideNav,
    int? tabIndex,
  ) {
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(
          children: <Widget>[
            PassengerHeader(isHome: isHome),
            Expanded(
              child: Stack(
                children: <Widget>[
                  Positioned.fill(child: child),
                  if (!hideNav)
                    Positioned(
                      left: 0,
                      right: 0,
                      bottom: MediaQuery.paddingOf(context).bottom,
                      child: BottomNav(
                        currentIndex: tabIndex ?? -1,
                        onSelected: (int index) =>
                            context.go(AppRoutes.passengerTabs[index]),
                        items: <BottomNavItem>[
                          BottomNavItem(
                            icon: AtaIcons.home,
                            label: l10n.navHome,
                          ),
                          BottomNavItem(
                            icon: AtaIcons.car,
                            label: l10n.navRides,
                          ),
                          BottomNavItem(
                            icon: AtaIcons.wallet,
                            label: l10n.navWallet,
                          ),
                          BottomNavItem(
                            icon: AtaIcons.user,
                            label: l10n.navAccount,
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
