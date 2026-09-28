import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/safety/presentation/widgets/emergency_bar.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Safety features and the emergency bar.
class SafetyPage extends StatelessWidget {
  const SafetyPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return PageWrap(
      children: <Widget>[
        ScreenTitle(
          eyebrow: l10n.safetyEyebrow,
          title: l10n.safetyTitle,
          copy: l10n.safetyCopy,
        ),
        const SizedBox(height: AtaSpacing.xxl),
        SafetyCard(
          icon: AtaIcons.pin,
          title: l10n.shareTripTitle,
          copy: l10n.shareTripCopy,
        ),
        const SizedBox(height: AtaSpacing.md),
        SafetyCard(
          icon: AtaIcons.shield,
          title: l10n.helpCenterTitle,
          copy: l10n.helpCenterCopy,
        ),
        const SizedBox(height: AtaSpacing.md),
        SafetyCard(
          icon: AtaIcons.user,
          title: l10n.trustedContactsTitle,
          copy: l10n.trustedContactsCopy,
        ),
        const SizedBox(height: AtaSpacing.xl),
        const EmergencyBar(),
      ],
    );
  }
}
