import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_scaffold.dart';
import 'package:ata_app/features/auth/presentation/widgets/choice_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// First screen: pick Arabic or English.
class LanguagePage extends StatelessWidget {
  const LanguagePage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AuthScaffold(
      maxWidth: AuthScaffold.defaultMaxWidth,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          ScreenTitle(
            eyebrow: l10n.appName,
            title: l10n.languageTitle,
            copy: l10n.languageCopy,
            centered: true,
          ),
          const SizedBox(height: AtaSpacing.xxxl),
          Directionality(
            textDirection: TextDirection.rtl,
            child: ChoiceCard(
              leading: const _LanguageTag(label: 'AR'),
              title: l10n.arabicName,
              copy: l10n.arabicRegion,
              cta: l10n.arabicContinue,
              onTap: () => _choose(context, LocaleCubit.arabic),
            ),
          ),
          const SizedBox(height: AtaSpacing.lg),
          Directionality(
            textDirection: TextDirection.ltr,
            child: ChoiceCard(
              leading: const _LanguageTag(label: 'EN', dark: true),
              title: l10n.englishName,
              copy: l10n.englishRegion,
              cta: l10n.englishContinue,
              dark: true,
              mirrorArrow: true,
              onTap: () => _choose(context, LocaleCubit.english),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _choose(BuildContext context, Locale locale) async {
    await context.read<LocaleCubit>().change(locale.languageCode);
    if (context.mounted) context.go(AppRoutes.role);
  }
}

class _LanguageTag extends StatelessWidget {
  const _LanguageTag({required this.label, this.dark = false});

  final String label;
  final bool dark;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: AtaSizes.iconBoxHero,
      height: AtaSizes.iconBoxHero,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: dark ? AtaColors.white10 : AtaColors.brandSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Text(
        label,
        style: AtaText.section.copyWith(color: AtaColors.brand),
      ),
    );
  }
}
