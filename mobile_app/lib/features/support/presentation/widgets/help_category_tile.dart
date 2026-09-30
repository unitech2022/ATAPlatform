import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:flutter/material.dart';

/// A help topic: icon, name and the number of articles.
class HelpCategoryTile extends StatelessWidget {
  const HelpCategoryTile({
    super.key,
    required this.category,
    required this.onTap,
  });

  final HelpCategory category;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
      child: AtaCard(
        onTap: onTap,
        padding: const EdgeInsets.all(AtaSpacing.md),
        child: Row(
          children: <Widget>[
            IconBox.cloud(
              icon: SupportText.categoryIcon(
                category.icon.isEmpty ? category.code : category.icon,
              ),
            ),
            const SizedBox(width: AtaSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(category.name, style: AtaText.bodyStrong),
                  if (category.articlesCount > 0)
                    Text(
                      context.l10n.helpArticlesCount(category.articlesCount),
                      style: AtaText.caption,
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
