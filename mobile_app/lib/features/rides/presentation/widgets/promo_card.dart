import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Dark "book a ride now" promo card.
class PromoCard extends StatelessWidget {
  const PromoCard({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return DarkCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          IconBox(
            icon: AtaIcons.clock,
            size: AtaSizes.iconBox + 4,
            iconSize: AtaSizes.iconMedium,
            background: AtaColors.white10,
            radius: AtaSpacing.md,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          Text(
            l10n.promoTitle,
            style: AtaText.headline.copyWith(color: AtaColors.white),
          ),
          const SizedBox(height: AtaSpacing.sm),
          Text(
            l10n.promoCopy,
            style: AtaText.small.copyWith(color: AtaColors.white70),
          ),
          const SizedBox(height: AtaSpacing.xxl),
          AtaButton(
            label: l10n.promoCta,
            variant: AtaButtonVariant.brand,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.go(AppRoutes.home),
          ),
        ],
      ),
    );
  }
}
