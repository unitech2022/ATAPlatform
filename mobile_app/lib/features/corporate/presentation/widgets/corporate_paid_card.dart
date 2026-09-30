import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/corporate/domain/entities/trip_corporate.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "مدفوعة من حساب الشركة" with the company, the trip purpose and the cost
/// center, on the trip end view and the receipt. Says nothing about wallet
/// or card charges: the company is billed.
class CorporatePaidCard extends StatelessWidget {
  const CorporatePaidCard({super.key, this.corporate});

  /// `null` when the API sent no `corporate` object: only the headline.
  final TripCorporate? corporate;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TripCorporate? c = corporate;
    return Container(
      key: const ValueKey<String>('corporate-paid-card'),
      width: double.infinity,
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.brandSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            children: <Widget>[
              const AtaIcon(AtaIcons.building, color: AtaColors.brand),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Text(
                  l10n.corpPaidByCompany,
                  style: AtaText.bodyStrong.copyWith(color: AtaColors.brand),
                ),
              ),
            ],
          ),
          if (c != null && c.companyName.isNotEmpty) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(c.companyName, style: AtaText.small),
          ],
          if (c != null && c.hasPurpose) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            _Line(label: l10n.corpPurposeLabel, value: c.purpose!),
          ],
          if (c != null && c.hasCostCenter)
            _Line(label: l10n.corpCostCenterLabel, value: c.costCenter!),
        ],
      ),
    );
  }
}

class _Line extends StatelessWidget {
  const _Line({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: AtaSpacing.xxs),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: <Widget>[
          Text(label, style: AtaText.caption),
          Flexible(
            child: Text(
              value,
              style: AtaText.captionStrong,
              textAlign: TextAlign.end,
            ),
          ),
        ],
      ),
    );
  }
}
