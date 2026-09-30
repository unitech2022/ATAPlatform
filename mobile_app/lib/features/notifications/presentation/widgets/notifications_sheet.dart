import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Opens the inbox as a bottom sheet bound to the shell's
/// [NotificationsCubit].
Future<void> showNotificationsSheet(BuildContext context) {
  final NotificationsCubit cubit = context.read<NotificationsCubit>();
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => BlocProvider<NotificationsCubit>.value(
      value: cubit,
      child: const _NotificationsSheet(),
    ),
  );
}

class _NotificationsSheet extends StatelessWidget {
  const _NotificationsSheet();

  static const double _maxHeightFactor = 0.7;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return ConstrainedBox(
      constraints: BoxConstraints(
        maxHeight: MediaQuery.sizeOf(context).height * _maxHeightFactor,
      ),
      child: Padding(
        padding: const EdgeInsets.all(AtaSpacing.lg),
        child: BlocBuilder<NotificationsCubit, NotificationsState>(
          builder: (BuildContext context, NotificationsState state) {
            return Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                const SheetHandle(),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: <Widget>[
                    Text(l10n.notificationsTitle, style: AtaText.section),
                    TextButton(
                      onPressed: () {
                        Navigator.of(context).pop();
                        context.go(AppRoutes.accountNotifications);
                      },
                      child: Text(
                        l10n.manageNotifications,
                        style: AtaText.captionStrong.copyWith(
                          color: AtaColors.brand,
                        ),
                      ),
                    ),
                  ],
                ),
                if (state.items.isEmpty)
                  Padding(
                    padding: const EdgeInsets.symmetric(
                      vertical: AtaSpacing.xl,
                    ),
                    child: Text(
                      l10n.notificationsEmpty,
                      style: AtaText.bodyMuted,
                      textAlign: TextAlign.center,
                    ),
                  )
                else
                  Flexible(
                    child: ListView.builder(
                      shrinkWrap: true,
                      itemCount: state.items.length,
                      itemBuilder: (BuildContext context, int index) =>
                          _NotificationRow(
                            item: state.items[index],
                            last: index == state.items.length - 1,
                          ),
                    ),
                  ),
                if (state.unreadCount > 0)
                  TextButton(
                    onPressed: context.read<NotificationsCubit>().markAllRead,
                    child: Text(l10n.markAllRead, style: AtaText.labelMuted),
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _NotificationRow extends StatelessWidget {
  const _NotificationRow({required this.item, required this.last});

  final NotificationItem item;
  final bool last;

  static AtaIcons _iconFor(String category) => switch (category) {
    'trips' || 'offers' => AtaIcons.car,
    'wallet' => AtaIcons.wallet,
    'safety' => AtaIcons.shield,
    'support' => AtaIcons.document,
    _ => AtaIcons.bell,
  };

  @override
  Widget build(BuildContext context) {
    return SettingRow(
      leading: IconBox(
        icon: _iconFor(item.resolvedCategory),
        size: AtaSizes.iconBoxSmall,
      ),
      onTap: () => _open(context),
      title: item.title,
      subtitle: item.body,
      showChevron: false,
      verticalPadding: AtaSpacing.sm,
      last: last,
      titleStyle: AtaText.label,
      trailing: item.isUnread
          ? Container(
              width: AtaSizes.badgeDot,
              height: AtaSizes.badgeDot,
              decoration: const BoxDecoration(
                color: AtaColors.brand,
                shape: BoxShape.circle,
              ),
            )
          : null,
    );
  }

  /// Marks the row read and opens its deep link through [DeepLinkCubit].
  void _open(BuildContext context) {
    final DeepLinkCubit deepLinks = context.read<DeepLinkCubit>();
    context.read<NotificationsCubit>().markItemRead(item.id);
    Navigator.of(context).pop();
    deepLinks.open(item.deepLink, notificationId: item.id);
  }
}
