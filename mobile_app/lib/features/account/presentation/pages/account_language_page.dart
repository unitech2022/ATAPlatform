import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/account/presentation/widgets/back_to_settings.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Switch the app language; persisted locally and synced to the account.
class AccountLanguagePage extends StatelessWidget {
  const AccountLanguagePage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final Locale current = context.watch<LocaleCubit>().state;
    return PageWrap(
      children: <Widget>[
        const BackToSettings(),
        const SizedBox(height: AtaSpacing.xl),
        ScreenTitle(
          eyebrow: l10n.languagePanelEyebrow,
          title: l10n.languagePanelTitle,
          copy: l10n.languagePanelCopy,
        ),
        const SizedBox(height: AtaSpacing.xxl),
        AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              _LanguageOption(
                locale: LocaleCubit.arabic,
                tag: 'AR',
                title: l10n.languageArabic,
                copy: l10n.languageArabicCopy,
                selected:
                    current.languageCode == LocaleCubit.arabic.languageCode,
              ),
              const SizedBox(height: AtaSpacing.sm),
              _LanguageOption(
                locale: LocaleCubit.english,
                tag: 'EN',
                title: l10n.languageEnglish,
                copy: l10n.languageEnglishCopy,
                selected:
                    current.languageCode == LocaleCubit.english.languageCode,
              ),
              const SizedBox(height: AtaSpacing.lg),
              Container(
                padding: const EdgeInsets.all(AtaSpacing.md),
                decoration: const BoxDecoration(
                  color: AtaColors.cloud,
                  borderRadius: AtaRadii.itemRadius,
                ),
                child: Text(l10n.languageSavedNote, style: AtaText.small),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _LanguageOption extends StatelessWidget {
  const _LanguageOption({
    required this.locale,
    required this.tag,
    required this.title,
    required this.copy,
    required this.selected,
  });

  final Locale locale;
  final String tag;
  final String title;
  final String copy;
  final bool selected;

  static const double _checkSize = 24;

  @override
  Widget build(BuildContext context) {
    return SelectableTile(
      selected: selected,
      onTap: () => context.read<LocaleCubit>().change(
        locale.languageCode,
        syncRemote: true,
      ),
      child: Row(
        children: <Widget>[
          Container(
            width: AtaSizes.iconBox + 4,
            height: AtaSizes.iconBox + 4,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: selected ? AtaColors.brand : AtaColors.cloud,
              borderRadius: AtaRadii.smallRadius,
            ),
            child: Text(
              tag,
              style: AtaText.label.copyWith(
                color: selected ? AtaColors.white : AtaColors.ink,
              ),
            ),
          ),
          const SizedBox(width: AtaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(title, style: AtaText.bodyStrong),
                Text(copy, style: AtaText.small),
              ],
            ),
          ),
          Container(
            width: _checkSize,
            height: _checkSize,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: selected ? AtaColors.brand : AtaColors.white,
              shape: BoxShape.circle,
              border: Border.all(
                color: selected ? AtaColors.brand : AtaColors.line,
                width: AtaSizes.borderThick,
              ),
            ),
            child: selected
                ? const AtaIcon(
                    AtaIcons.check,
                    size: AtaSizes.iconSmall,
                    color: AtaColors.white,
                  )
                : null,
          ),
        ],
      ),
    );
  }
}
