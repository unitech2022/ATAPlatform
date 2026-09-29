import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/design/widgets/star_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Average + distribution, frequent tags and anonymous recent comments.
class RatingSummaryView extends StatelessWidget {
  const RatingSummaryView({super.key, required this.summary});

  final RatingSummary summary;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        AtaCard(
          child: Column(
            children: <Widget>[
              Text(
                Money.compact(summary.ratingAvg),
                style: AtaText.display,
                textDirection: TextDirection.ltr,
              ),
              StarRating(value: summary.ratingAvg.round()),
              const SizedBox(height: AtaSpacing.xs),
              Text(
                l10n.ratingCountLine(summary.ratingCount),
                style: AtaText.caption,
              ),
              const SizedBox(height: AtaSpacing.md),
              for (int n = StarRating.max; n >= 1; n--)
                _DistributionRow(stars: n, summary: summary),
            ],
          ),
        ),
        if (summary.topTags.isNotEmpty) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          AtaCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(l10n.ratingTopTags, style: AtaText.section),
                const SizedBox(height: AtaSpacing.sm),
                Wrap(
                  spacing: AtaSpacing.xs,
                  runSpacing: AtaSpacing.xs,
                  children: <Widget>[
                    for (final RatingTagCount t in summary.topTags)
                      AtaBadge(
                        label:
                            '${t.name.isEmpty ? RatingText.tagCode(l10n, t.code) : t.name} · ${t.count}',
                        background: t.positive
                            ? AtaColors.brandSoft
                            : AtaColors.dangerSoft,
                        foreground: t.positive
                            ? AtaColors.brand
                            : AtaColors.danger,
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
        const SizedBox(height: AtaSpacing.md),
        AtaCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(l10n.ratingRecentComments, style: AtaText.section),
              const SizedBox(height: AtaSpacing.sm),
              if (summary.recentComments.isEmpty)
                Text(l10n.ratingNoComments, style: AtaText.small),
              for (final RatingComment c in summary.recentComments)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: <Widget>[
                      Row(
                        children: <Widget>[
                          StarRating(value: c.stars, size: AtaSizes.iconSmall),
                          const Spacer(),
                          Text(
                            c.week,
                            style: AtaText.caption,
                            textDirection: TextDirection.ltr,
                          ),
                        ],
                      ),
                      Text(c.comment, style: AtaText.small),
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

class _DistributionRow extends StatelessWidget {
  const _DistributionRow({required this.stars, required this.summary});

  final int stars;
  final RatingSummary summary;

  static const double _labelWidth = 24;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          SizedBox(
            width: _labelWidth,
            child: Text('$stars', style: AtaText.label),
          ),
          Expanded(
            child: ProgressBar(
              value: summary.shareOf(stars),
              track: AtaColors.cloud,
              fill: AtaColors.warning,
            ),
          ),
          const SizedBox(width: AtaSpacing.sm),
          Text('${summary.countFor(stars)}', style: AtaText.caption),
        ],
      ),
    );
  }
}
