import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/account/presentation/widgets/back_to_settings.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Live chat, phone and email contact cards.
class AccountContactPage extends StatelessWidget {
  const AccountContactPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return PageWrap(
      children: <Widget>[
        const BackToSettings(),
        const SizedBox(height: AtaSpacing.xl),
        ScreenTitle(
          eyebrow: l10n.contactEyebrow,
          title: l10n.contactTitle,
          copy: l10n.contactCopy,
        ),
        const SizedBox(height: AtaSpacing.xxl),
        _ContactCard(
          icon: AtaIcons.user,
          title: l10n.liveChatTitle,
          value: l10n.liveChatValue,
          copy: l10n.liveChatCopy,
        ),
        const SizedBox(height: AtaSpacing.md),
        _ContactCard(
          icon: AtaIcons.phone,
          title: l10n.callTitle,
          value: l10n.callValue,
          copy: l10n.callCopy,
        ),
        const SizedBox(height: AtaSpacing.md),
        _ContactCard(
          icon: AtaIcons.document,
          title: l10n.emailTitle,
          value: l10n.emailValue,
          copy: l10n.emailCopy,
        ),
      ],
    );
  }
}

class _ContactCard extends StatelessWidget {
  const _ContactCard({
    required this.icon,
    required this.title,
    required this.value,
    required this.copy,
  });

  final AtaIcons icon;
  final String title;
  final String value;
  final String copy;

  @override
  Widget build(BuildContext context) {
    return AtaCard(
      onTap: () {},
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          IconBox(
            icon: icon,
            size: AtaSizes.iconBoxLarge,
            iconSize: AtaSizes.iconLarge,
            radius: AtaSpacing.md,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          Text(title, style: AtaText.section),
          const SizedBox(height: AtaSpacing.sm),
          Text(
            value,
            style: AtaText.bodyStrong.copyWith(color: AtaColors.brand),
            textDirection: TextDirection.ltr,
            textAlign: TextAlign.start,
          ),
          const SizedBox(height: AtaSpacing.xs),
          Text(copy, style: AtaText.small),
        ],
      ),
    );
  }
}
