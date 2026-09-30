import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:flutter/material.dart';

/// A search / category result: title and the excerpt.
class HelpArticleTile extends StatelessWidget {
  const HelpArticleTile({
    super.key,
    required this.article,
    required this.onTap,
  });

  final HelpArticleSummary article;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
      child: AtaCard(
        onTap: onTap,
        padding: const EdgeInsets.all(AtaSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Text(article.title, style: AtaText.bodyStrong),
            if (article.excerpt.isNotEmpty) ...<Widget>[
              const SizedBox(height: AtaSpacing.xxs),
              Text(
                article.excerpt,
                style: AtaText.small,
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
