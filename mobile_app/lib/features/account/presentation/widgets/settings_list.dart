import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// The settings rows of the account page plus logout / delete actions.
class SettingsList extends StatelessWidget {
  const SettingsList({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool isArabic = context.watch<LocaleCubit>().isArabic;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.settings, style: AtaText.section),
          const SizedBox(height: AtaSpacing.xs),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.user),
            title: l10n.personalInfo,
            subtitle: l10n.personalInfoCopy,
            onTap: () {},
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.home),
            title: l10n.savedPlaces,
            subtitle: l10n.savedPlacesCopy,
            onTap: () {},
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.shield),
            title: l10n.privacySecurity,
            subtitle: l10n.privacySecurityCopy,
            onTap: () => context.go(AppRoutes.safety),
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.clock),
            title: l10n.notificationsRow,
            subtitle: l10n.notificationsRowCopy,
            trailing: Container(
              width: AtaSizes.badgeDot,
              height: AtaSizes.badgeDot,
              decoration: const BoxDecoration(
                color: AtaColors.brand,
                shape: BoxShape.circle,
              ),
            ),
            onTap: () => context.go(AppRoutes.accountNotifications),
          ),
          SettingRow(
            leading: Container(
              width: AtaSizes.iconBox,
              height: AtaSizes.iconBox,
              alignment: Alignment.center,
              decoration: const BoxDecoration(
                color: AtaColors.brandSoft,
                borderRadius: AtaRadii.smallRadius,
              ),
              child: Text(
                isArabic ? 'AR' : 'EN',
                style: AtaText.label.copyWith(color: AtaColors.brand),
              ),
            ),
            title: l10n.languageRow,
            subtitle: isArabic ? l10n.languageArabic : l10n.languageEnglish,
            onTap: () => context.go(AppRoutes.accountLanguage),
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.phone),
            title: l10n.supportTitle,
            subtitle: l10n.supportRowCopy,
            onTap: () => context.go(AppRoutes.support),
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.user),
            title: l10n.contactRow,
            subtitle: l10n.contactRowCopy,
            onTap: () => context.go(AppRoutes.accountContact),
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.document),
            title: l10n.termsRow,
            subtitle: l10n.termsRowCopy,
            last: true,
            onTap: () => context.go(AppRoutes.accountTerms),
          ),
          const SizedBox(height: AtaSpacing.lg),
          Row(
            children: <Widget>[
              Expanded(
                child: AtaButton(
                  label: l10n.logout,
                  variant: AtaButtonVariant.outline,
                  height: AtaSizes.buttonCompact,
                  onPressed: context.read<SessionCubit>().signOut,
                ),
              ),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: AtaButton(
                  label: l10n.deleteApp,
                  variant: AtaButtonVariant.dangerSoft,
                  height: AtaSizes.buttonCompact,
                  onPressed: () => context.go(AppRoutes.accountDelete),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
