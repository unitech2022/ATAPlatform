import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/account/presentation/cubit/notification_prefs_cubit.dart';
import 'package:ata_app/features/account/presentation/cubit/notification_prefs_state.dart';
import 'package:ata_app/features/account/presentation/widgets/back_to_settings.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Toggles for trips / wallet / safety / offers notifications.
class NotificationPrefsPage extends StatelessWidget {
  const NotificationPrefsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<NotificationPrefsCubit>(
      create: (_) => NotificationPrefsCubit(
        getPreferences: getIt(),
        updatePreferences: getIt(),
      )..load(),
      child: PageWrap(
        children: <Widget>[
          const BackToSettings(),
          const SizedBox(height: AtaSpacing.xl),
          ScreenTitle(
            eyebrow: l10n.notifPrefsEyebrow,
            title: l10n.notifPrefsTitle,
            copy: l10n.notifPrefsCopy,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          const _PrefsCard(),
        ],
      ),
    );
  }
}

class _PrefsCard extends StatelessWidget {
  const _PrefsCard();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<(NotificationChannel, AtaIcons, String, String)> options =
        <(NotificationChannel, AtaIcons, String, String)>[
          (
            NotificationChannel.trips,
            AtaIcons.car,
            l10n.prefTripsTitle,
            l10n.prefTripsCopy,
          ),
          (
            NotificationChannel.wallet,
            AtaIcons.wallet,
            l10n.prefWalletTitle,
            l10n.prefWalletCopy,
          ),
          (
            NotificationChannel.safety,
            AtaIcons.shield,
            l10n.prefSafetyTitle,
            l10n.prefSafetyCopy,
          ),
          (
            NotificationChannel.offers,
            AtaIcons.clock,
            l10n.prefOffersTitle,
            l10n.prefOffersCopy,
          ),
        ];
    return BlocBuilder<NotificationPrefsCubit, NotificationPrefsState>(
      builder: (BuildContext context, NotificationPrefsState state) {
        final NotificationPrefsCubit cubit = context
            .read<NotificationPrefsCubit>();
        return AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              if (state.loading) const CenteredLoader(),
              if (state.failure != null) ...<Widget>[
                InlineError(message: failureText(state.failure!, l10n)),
                const SizedBox(height: AtaSpacing.sm),
              ],
              for (final (
                    NotificationChannel channel,
                    AtaIcons icon,
                    String title,
                    String copy,
                  )
                  in options)
                SettingRow(
                  leading: IconBox.cloud(
                    icon: icon,
                    size: AtaSizes.iconBox + 4,
                  ),
                  title: title,
                  subtitle: copy,
                  showChevron: false,
                  last: channel == options.last.$1,
                  verticalPadding: AtaSpacing.lg,
                  onTap: state.saving ? null : () => cubit.toggle(channel),
                  trailing: AtaToggle(
                    value: cubit.isEnabled(channel),
                    onChanged: state.saving
                        ? null
                        : (_) => cubit.toggle(channel),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}
