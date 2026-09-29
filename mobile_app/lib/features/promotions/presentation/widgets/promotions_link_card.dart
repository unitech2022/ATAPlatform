import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// "العروض" entry (wallet page) opening `/promotions`.
class PromotionsLinkCard extends StatelessWidget {
  const PromotionsLinkCard({super.key});

  @override
  Widget build(BuildContext context) {
    return AtaCard(
      padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.lg),
      child: SettingRow(
        leading: const IconBox(icon: AtaIcons.gift),
        title: context.l10n.promotionsTitle,
        subtitle: context.l10n.promotionsLinkCopy,
        last: true,
        onTap: () => context.push(AppRoutes.promotions),
      ),
    );
  }
}
