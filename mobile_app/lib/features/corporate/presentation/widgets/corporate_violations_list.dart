import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// The rules a trip breaks, each with the options the company allows
/// ("الفئات المسموحة: اقتصادي"), and a hint on how to fix it.
class CorporateViolationsList extends StatelessWidget {
  const CorporateViolationsList({
    super.key,
    required this.violations,
    this.categories = const <RideCategory>[],
    this.showTitle = true,
  });

  final List<PolicyViolation> violations;

  /// Turns the allowed category codes into names.
  final List<RideCategory> categories;
  final bool showTitle;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Container(
      key: const ValueKey<String>('corporate-violations'),
      width: double.infinity,
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.warningSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          if (showTitle) ...<Widget>[
            Text(
              l10n.corpViolationsTitle,
              style: AtaText.label.copyWith(color: AtaColors.warning),
            ),
            const SizedBox(height: AtaSpacing.xxs),
          ],
          for (final PolicyViolation v in violations) ...<Widget>[
            Text(CorporateText.violation(l10n, v), style: AtaText.small),
            ?_allowed(l10n, v),
          ],
          const SizedBox(height: AtaSpacing.xxs),
          Text(l10n.corpViolationsHint, style: AtaText.caption),
        ],
      ),
    );
  }

  Widget? _allowed(AppLocalizations l10n, PolicyViolation v) {
    final String? text = CorporateText.allowedOptions(
      l10n,
      v,
      categoryName: CorporateText.categoryNames(categories),
    );
    return text == null ? null : Text(text, style: AtaText.caption);
  }
}
