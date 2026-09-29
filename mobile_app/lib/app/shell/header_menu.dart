import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Opens the header popover with the profile summary and settings links.
Future<void> showHeaderMenu(BuildContext context) {
  return showDialog<void>(
    context: context,
    barrierColor: Colors.transparent,
    builder: (BuildContext dialogContext) => _HeaderMenu(
      onNavigate: (String route) {
        Navigator.of(dialogContext).pop();
        context.go(route);
      },
    ),
  );
}

class _HeaderMenu extends StatelessWidget {
  const _HeaderMenu({required this.onNavigate});

  final ValueChanged<String> onNavigate;

  static const double _width = 288;
  static const double _top = AtaSizes.header - AtaSpacing.md;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String name =
        context.read<SessionCubit>().state.session?.user.fullName ??
        l10n.guestName;
    final List<(String, AtaIcons, String)> items = <(String, AtaIcons, String)>[
      (l10n.menuAccountSettings, AtaIcons.user, AppRoutes.account),
      (l10n.menuLanguage, AtaIcons.document, AppRoutes.accountLanguage),
      (l10n.menuNotifications, AtaIcons.bell, AppRoutes.accountNotifications),
      (l10n.menuSafety, AtaIcons.shield, AppRoutes.safety),
      (l10n.promotionsTitle, AtaIcons.gift, AppRoutes.promotions),
      (l10n.menuContact, AtaIcons.phone, AppRoutes.accountContact),
    ];
    return SafeArea(
      child: Align(
        alignment: AlignmentDirectional.topEnd,
        child: Padding(
          padding: const EdgeInsetsDirectional.only(
            top: _top,
            end: AtaSpacing.gutter,
          ),
          child: Material(
            color: AtaColors.white,
            borderRadius: AtaRadii.cardRadius,
            elevation: 0,
            child: Container(
              width: _width,
              padding: const EdgeInsets.all(AtaSpacing.sm),
              decoration: BoxDecoration(
                borderRadius: AtaRadii.cardRadius,
                border: Border.all(color: AtaColors.line),
                boxShadow: AtaShadows.float,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  InkWell(
                    borderRadius: AtaRadii.itemRadius,
                    onTap: () => onNavigate(AppRoutes.account),
                    child: Container(
                      padding: const EdgeInsets.all(AtaSpacing.sm),
                      decoration: const BoxDecoration(
                        color: AtaColors.cloud,
                        borderRadius: AtaRadii.itemRadius,
                      ),
                      child: Row(
                        children: <Widget>[
                          const IconBox(
                            icon: AtaIcons.user,
                            background: AtaColors.ink,
                            foreground: AtaColors.white,
                            round: true,
                          ),
                          const SizedBox(width: AtaSpacing.sm),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: <Widget>[
                                Text(name, style: AtaText.bodyStrong),
                                Text(
                                  l10n.menuViewProfile,
                                  style: AtaText.caption,
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(
                      AtaSpacing.sm,
                      AtaSpacing.sm,
                      AtaSpacing.sm,
                      AtaSpacing.xs,
                    ),
                    child: Text(l10n.menuSettings, style: AtaText.caption),
                  ),
                  for (final (String label, AtaIcons icon, String route)
                      in items)
                    InkWell(
                      borderRadius: AtaRadii.smallRadius,
                      onTap: () => onNavigate(route),
                      child: Padding(
                        padding: const EdgeInsets.all(AtaSpacing.sm),
                        child: Row(
                          children: <Widget>[
                            AtaIcon(icon, color: AtaColors.muted),
                            const SizedBox(width: AtaSpacing.sm),
                            Expanded(child: Text(label, style: AtaText.label)),
                            const AtaIcon(
                              AtaIcons.chevron,
                              size: AtaSizes.iconSmall,
                              color: AtaColors.muted,
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
