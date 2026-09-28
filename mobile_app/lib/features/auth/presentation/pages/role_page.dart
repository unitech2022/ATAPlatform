import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_scaffold.dart';
import 'package:ata_app/features/auth/presentation/widgets/choice_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Rider or driver account.
class RolePage extends StatelessWidget {
  const RolePage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AuthScaffold(
      onBack: () => context.go(AppRoutes.language),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          ScreenTitle(
            eyebrow: l10n.roleEyebrow,
            title: l10n.roleTitle,
            copy: l10n.roleCopy,
            centered: true,
          ),
          const SizedBox(height: AtaSpacing.xxxl),
          ChoiceCard(
            leading: const IconBox.brand(
              icon: AtaIcons.user,
              size: AtaSizes.iconBoxHero,
              iconSize: AtaSizes.iconHero,
            ),
            tag: l10n.riderTag,
            title: l10n.riderTitle,
            copy: l10n.riderCopy,
            cta: l10n.riderCta,
            onTap: () => context.go(AppRoutes.phoneFor(UserRole.passenger)),
          ),
          const SizedBox(height: AtaSpacing.lg),
          ChoiceCard(
            leading: const IconBox.brand(
              icon: AtaIcons.car,
              size: AtaSizes.iconBoxHero,
              iconSize: AtaSizes.iconHero,
            ),
            tag: l10n.driverTag,
            title: l10n.driverTitle,
            copy: l10n.driverCopy,
            cta: l10n.driverCta,
            dark: true,
            onTap: () => context.go(AppRoutes.phoneFor(UserRole.driver)),
          ),
          const SizedBox(height: AtaSpacing.xxl),
          Text(
            l10n.roleTerms,
            style: AtaText.caption,
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
