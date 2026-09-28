import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
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

/// Driver settings rows and logout.
class SettingsTab extends StatelessWidget {
  const SettingsTab({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final LocaleCubit locale = context.watch<LocaleCubit>();
    final List<(AtaIcons, String, String, VoidCallback?)> rows =
        <(AtaIcons, String, String, VoidCallback?)>[
          (AtaIcons.user, l10n.personalInfo, l10n.driverPersonalCopy, null),
          (AtaIcons.wallet, l10n.driverBank, l10n.driverBankCopy, null),
          (
            AtaIcons.car,
            l10n.driverTripSettings,
            l10n.driverTripSettingsCopy,
            null,
          ),
          (AtaIcons.clock, l10n.notificationsRow, l10n.driverNotifCopy, null),
          (
            AtaIcons.check,
            l10n.reliabilityTitle,
            l10n.reliabilityCopy,
            () => context.push(AppRoutes.driverReliability),
          ),
          (
            AtaIcons.search,
            l10n.driverLostItemsTitle,
            l10n.driverLostItemsCopy,
            () => context.push(AppRoutes.driverLostItems),
          ),
          (
            AtaIcons.document,
            l10n.languageRow,
            locale.isArabic ? l10n.languageArabic : l10n.languageEnglish,
            () => locale.change(
              locale.isArabic
                  ? LocaleCubit.english.languageCode
                  : LocaleCubit.arabic.languageCode,
              syncRemote: true,
            ),
          ),
          (AtaIcons.shield, l10n.driverSupport, l10n.driverSupportCopy, null),
        ];
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          for (int i = 0; i < rows.length; i++)
            SettingRow(
              leading: IconBox.cloud(icon: rows[i].$1),
              title: rows[i].$2,
              subtitle: rows[i].$3,
              onTap: rows[i].$4,
              last: i == rows.length - 1,
            ),
          const SizedBox(height: AtaSpacing.xl),
          AtaButton(
            label: l10n.logout,
            variant: AtaButtonVariant.dangerOutline,
            height: AtaSizes.buttonCompact,
            onPressed: context.read<SessionCubit>().signOut,
          ),
        ],
      ),
    );
  }
}
