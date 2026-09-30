import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_budget_bar.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Company, role and status of the employee with the monthly budget and
/// the per-trip limit (`/account/corporate`).
class CorporateDetailsCard extends StatelessWidget {
  const CorporateDetailsCard({super.key, required this.profile});

  final CorporateProfile profile;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CorporateMembership m = profile.membership;
    return AtaCard(
      key: const ValueKey<String>('corporate-details'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              const IconBox(icon: AtaIcons.building),
              const SizedBox(width: AtaSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(m.companyName, style: AtaText.section),
                    Text(
                      CorporateText.role(l10n, m.role),
                      style: AtaText.caption,
                    ),
                  ],
                ),
              ),
              AtaBadge(
                label: CorporateText.status(l10n, m.status),
                background: m.isActive
                    ? AtaColors.brandSoft
                    : AtaColors.warningSoft,
                foreground: m.isActive ? AtaColors.brand : AtaColors.warning,
              ),
            ],
          ),
          if (m.employeeNumber != null || m.department != null)
            Padding(
              padding: const EdgeInsets.only(top: AtaSpacing.sm),
              child: Text(
                <String>[
                  if (m.employeeNumber != null)
                    l10n.corpEmployeeNumber(m.employeeNumber!),
                  if (m.department != null) l10n.corpDepartment(m.department!),
                ].join(' · '),
                style: AtaText.caption,
              ),
            ),
          if (!m.isActive) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            Text(l10n.corpStatusDisabledCopy, style: AtaText.small),
          ] else ...<Widget>[
            const SizedBox(height: AtaSpacing.lg),
            Text(l10n.corpBudgetTitle, style: AtaText.label),
            const SizedBox(height: AtaSpacing.xs),
            CorporateBudgetBar(budget: profile.budget),
            if (profile.perTripLimit != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              Text(
                l10n.corpTripLimit(
                  CorporateText.money(l10n, profile.perTripLimit!),
                ),
                style: AtaText.captionStrong,
              ),
            ],
          ],
        ],
      ),
    );
  }
}
