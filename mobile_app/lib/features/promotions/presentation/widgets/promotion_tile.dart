import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/presentation/widgets/promo_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One promotion: value, code, conditions; copy / use while available.
class PromotionTile extends StatelessWidget {
  const PromotionTile({
    super.key,
    required this.promotion,
    this.onCopy,
    this.onUse,
  });

  final Promotion promotion;
  final VoidCallback? onCopy;
  final VoidCallback? onUse;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final Promotion p = promotion;
    final bool available = p.status == PromotionStatus.available;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              IconBox(
                icon: AtaIcons.gift,
                background: available ? AtaColors.brandSoft : AtaColors.cloud,
                foreground: available ? AtaColors.brand : AtaColors.muted,
              ),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      p.name.isEmpty ? p.code : p.name,
                      style: AtaText.bodyStrong,
                    ),
                    Text(
                      PromoText.value(l10n, p),
                      style: AtaText.captionStrong.copyWith(
                        color: available ? AtaColors.brand : AtaColors.muted,
                      ),
                    ),
                  ],
                ),
              ),
              AtaBadge(
                label: p.code,
                background: available ? AtaColors.ink : AtaColors.cloud,
                foreground: available ? AtaColors.white : AtaColors.muted,
              ),
            ],
          ),
          if (p.description.isNotEmpty) ...<Widget>[
            const SizedBox(height: AtaSpacing.xs),
            Text(p.description, style: AtaText.small),
          ],
          for (final String line in PromoText.conditions(
            l10n,
            p,
            context.localeCode,
          ))
            Text('• $line', style: AtaText.caption),
          if (available && (onCopy != null || onUse != null)) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: <Widget>[
                if (onCopy != null)
                  PillButton(
                    label: l10n.promoCopy,
                    onTap: onCopy,
                    bordered: true,
                    elevated: false,
                  ),
                const SizedBox(width: AtaSpacing.xs),
                if (onUse != null)
                  PillButton(
                    label: l10n.promoUse,
                    onTap: onUse,
                    background: AtaColors.brand,
                    foreground: AtaColors.white,
                    elevated: false,
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
