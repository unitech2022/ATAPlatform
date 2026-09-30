import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "سياسة الشركة": the categories, days and hours the employee may ride,
/// the purpose / cost center requirements and whether scheduling is allowed.
class CorporatePolicyCard extends StatelessWidget {
  const CorporatePolicyCard({
    super.key,
    required this.policy,
    this.categories = const <RideCategory>[],
  });

  final CorporatePolicySummary policy;
  final List<RideCategory> categories;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String separator = l10n.corpListSeparator;
    final List<String>? codes = policy.allowedRideCategoryCodes;
    final List<int>? days = policy.allowedDays;
    final List<TimeWindow>? windows = policy.timeWindows;
    final List<String> lines = <String>[
      codes == null
          ? l10n.corpPolicyAllCategories
          : l10n.corpPolicyCategories(
              codes
                  .map(CorporateText.categoryNames(categories))
                  .join(separator),
            ),
      if (days != null)
        l10n.corpPolicyDays(
          CorporateText.days(
            l10n,
            days.map((int d) => '$d').toList(growable: false),
          ),
        ),
      if (windows != null)
        l10n.corpPolicyHours(
          windows
              .map(
                (TimeWindow w) =>
                    CorporateText.window(l10n, '${w.from}-${w.to}'),
              )
              .join(separator),
        ),
      if (policy.requirePurpose) l10n.corpPolicyPurposeRequired,
      if (policy.requireCostCenter) l10n.corpPolicyCostCenterRequired,
      policy.allowScheduled
          ? l10n.corpPolicyScheduledAllowed
          : l10n.corpPolicyScheduledBlocked,
    ];
    return AtaCard(
      key: const ValueKey<String>('corporate-policy'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            policy.name.isEmpty ? l10n.corpPolicyTitle : policy.name,
            style: AtaText.section,
          ),
          const SizedBox(height: AtaSpacing.xs),
          for (final String line in lines)
            Padding(
              padding: const EdgeInsets.only(top: AtaSpacing.xxs),
              child: Text(line, style: AtaText.small),
            ),
        ],
      ),
    );
  }
}
