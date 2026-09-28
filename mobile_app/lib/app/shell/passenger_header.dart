import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/app/shell/header_menu.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_header.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_logo.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_state.dart';
import 'package:ata_app/features/notifications/presentation/widgets/notifications_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Bell with unread badge, logo, wallet pill, back pill and menu button.
class PassengerHeader extends StatelessWidget {
  const PassengerHeader({super.key, required this.isHome});

  final bool isHome;

  static const double _badgeSize = 16;

  @override
  Widget build(BuildContext context) {
    return AtaHeader(
      leading: <Widget>[
        BlocBuilder<NotificationsCubit, NotificationsState>(
          buildWhen: (NotificationsState p, NotificationsState c) =>
              p.unreadCount != c.unreadCount,
          builder: (BuildContext context, NotificationsState state) {
            return HeaderIconButton(
              onTap: () => showNotificationsSheet(context),
              badge: state.unreadCount > 0
                  ? _UnreadBadge(count: state.unreadCount)
                  : null,
              child: const AtaIcon(AtaIcons.bell, color: AtaColors.ink),
            );
          },
        ),
        const AtaLogo(),
      ],
      trailing: <Widget>[
        PillButton(
          label: context.l10n.navWallet,
          icon: AtaIcons.wallet,
          background: AtaColors.cloud,
          elevated: false,
          onTap: () => context.go(AppRoutes.wallet),
        ),
        if (!isHome)
          HeaderIconButton(
            background: AtaColors.white,
            onTap: () => context.go(AppRoutes.home),
            child: const AtaIcon(
              AtaIcons.arrow,
              size: AtaSizes.iconSmall,
              color: AtaColors.ink,
            ),
          ),
        HeaderIconButton(
          background: AtaColors.ink,
          onTap: () => showHeaderMenu(context),
          child: const AtaIcon(AtaIcons.menu, color: AtaColors.white),
        ),
      ],
    );
  }
}

class _UnreadBadge extends StatelessWidget {
  const _UnreadBadge({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: PassengerHeader._badgeSize,
      height: PassengerHeader._badgeSize,
      alignment: Alignment.center,
      decoration: const BoxDecoration(
        color: AtaColors.brand,
        shape: BoxShape.circle,
      ),
      child: Text(
        '$count',
        style: AtaText.captionStrong.copyWith(
          color: AtaColors.white,
          fontSize: 10,
        ),
      ),
    );
  }
}
