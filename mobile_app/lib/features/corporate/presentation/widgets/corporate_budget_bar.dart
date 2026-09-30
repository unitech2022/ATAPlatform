import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_budget.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "استُخدم 420.5 من 1,500 ر.س" with a progress bar and the amount left;
/// "بدون حد شهري" when the employee has no monthly budget.
class CorporateBudgetBar extends StatelessWidget {
  const CorporateBudgetBar({super.key, required this.budget});

  final CorporateBudget? budget;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CorporateBudget? b = budget;
    if (b == null) return Text(l10n.corpBudgetNone, style: AtaText.small);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(
          l10n.corpBudgetUsed(
            CorporateText.money(l10n, b.spent),
            CorporateText.money(l10n, b.monthly),
          ),
          style: AtaText.small,
        ),
        const SizedBox(height: AtaSpacing.xs),
        ProgressBar(
          value: b.usedFraction,
          track: AtaColors.line,
          fill: b.isExhausted ? AtaColors.danger : AtaColors.brand,
        ),
        const SizedBox(height: AtaSpacing.xs),
        Text(
          l10n.corpBudgetLeft(CorporateText.money(l10n, b.remaining)),
          style: AtaText.captionStrong,
        ),
      ],
    );
  }
}
