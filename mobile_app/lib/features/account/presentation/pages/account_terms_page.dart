import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/account/presentation/widgets/back_to_settings.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Terms and conditions text.
class AccountTermsPage extends StatelessWidget {
  const AccountTermsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<(String, String)> sections = <(String, String)>[
      (l10n.terms1Title, l10n.terms1Copy),
      (l10n.terms2Title, l10n.terms2Copy),
      (l10n.terms3Title, l10n.terms3Copy),
      (l10n.terms4Title, l10n.terms4Copy),
    ];
    return PageWrap(
      children: <Widget>[
        const BackToSettings(),
        const SizedBox(height: AtaSpacing.xl),
        ScreenTitle(
          eyebrow: l10n.termsPanelEyebrow,
          title: l10n.termsPanelTitle,
          copy: l10n.termsPanelCopy,
        ),
        const SizedBox(height: AtaSpacing.xxl),
        AtaCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              for (int i = 0; i < sections.length; i++)
                Container(
                  padding: EdgeInsets.only(
                    top: i == 0 ? 0 : AtaSpacing.lg,
                    bottom: i == sections.length - 1 ? 0 : AtaSpacing.lg,
                  ),
                  decoration: BoxDecoration(
                    border: i == sections.length - 1
                        ? null
                        : const Border(
                            bottom: BorderSide(color: AtaColors.line),
                          ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Text(sections[i].$1, style: AtaText.bodyStrong),
                      const SizedBox(height: AtaSpacing.sm),
                      Text(sections[i].$2, style: AtaText.small),
                    ],
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
