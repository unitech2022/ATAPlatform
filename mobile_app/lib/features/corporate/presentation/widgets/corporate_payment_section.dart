import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/cost_center.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_state.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_violations_list.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The company-account block of the request sheet, shown while the corporate
/// payment is selected: company, remaining monthly budget and per-trip limit,
/// the required "غرض الرحلة" and "مركز التكلفة" fields and the policy
/// violations (with the allowed options) of the current category / time.
class CorporatePaymentSection extends StatelessWidget {
  const CorporatePaymentSection({super.key, this.categories = const []});

  /// The catalog, to show allowed categories by name.
  final List<RideCategory> categories;

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<CorporatePaymentCubit, CorporatePaymentState>(
      builder: (BuildContext context, CorporatePaymentState state) {
        if (!state.showForm) return const SizedBox.shrink();
        final AppLocalizations l10n = context.l10n;
        final CorporateProfile profile = state.profile!;
        final double? remaining = state.eligibility.remainingBudget;
        return Container(
          key: const ValueKey<String>('corporate-section'),
          margin: const EdgeInsets.only(top: AtaSpacing.sm),
          padding: const EdgeInsets.all(AtaSpacing.md),
          decoration: BoxDecoration(
            color: AtaColors.brandSoft,
            borderRadius: AtaRadii.smallRadius,
            border: Border.all(color: AtaColors.brand),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Row(
                children: <Widget>[
                  const AtaIcon(AtaIcons.building, color: AtaColors.brand),
                  const SizedBox(width: AtaSpacing.sm),
                  Expanded(
                    child: Text(
                      profile.membership.companyName,
                      style: AtaText.bodyStrong,
                    ),
                  ),
                ],
              ),
              if (remaining != null)
                Text(
                  l10n.corpRemainingBudget(
                    CorporateText.money(l10n, remaining),
                  ),
                  style: AtaText.caption,
                ),
              if (profile.perTripLimit != null)
                Text(
                  l10n.corpOptionTripLimit(
                    CorporateText.money(l10n, profile.perTripLimit!),
                  ),
                  style: AtaText.caption,
                ),
              const SizedBox(height: AtaSpacing.sm),
              _PurposeField(state: state),
              if (profile.costCenters.isNotEmpty) ...<Widget>[
                const SizedBox(height: AtaSpacing.sm),
                _CostCenters(state: state, centers: profile.costCenters),
              ],
              if (!state.eligibility.eligible) ...<Widget>[
                const SizedBox(height: AtaSpacing.sm),
                CorporateViolationsList(
                  violations: state.eligibility.violations,
                  categories: categories,
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}

class _PurposeField extends StatelessWidget {
  const _PurposeField({required this.state});

  final CorporatePaymentState state;

  static const int _maxLength = 200;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        TextFormField(
          key: const ValueKey<String>('corporate-purpose'),
          initialValue: state.purpose,
          onChanged: context.read<CorporatePaymentCubit>().purposeChanged,
          maxLength: _maxLength,
          style: AtaText.body,
          decoration: InputDecoration(
            labelText: state.purposeRequired
                ? '${l10n.corpPurposeLabel} *'
                : l10n.corpPurposeLabel,
            hintText: l10n.corpPurposeHint,
            counterText: '',
          ),
        ),
        if (state.purposeMissing)
          Padding(
            padding: const EdgeInsets.only(top: AtaSpacing.xxs),
            child: Text(
              l10n.corpPurposeRequired,
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          ),
      ],
    );
  }
}

class _CostCenters extends StatelessWidget {
  const _CostCenters({required this.state, required this.centers});

  final CorporatePaymentState state;
  final List<CostCenter> centers;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CorporatePaymentCubit cubit = context.read<CorporatePaymentCubit>();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Text(
          state.costCenterRequired
              ? '${l10n.corpCostCenterLabel} *'
              : l10n.corpCostCenterLabel,
          style: AtaText.label,
        ),
        const SizedBox(height: AtaSpacing.xs),
        Wrap(
          spacing: AtaSpacing.xs,
          runSpacing: AtaSpacing.xs,
          children: <Widget>[
            for (final CostCenter c in centers)
              _Chip(
                key: ValueKey<String>('cost-center-${c.id}'),
                label: l10n.corpCostCenterItem(c.code, c.name),
                selected: c.id == state.costCenterId,
                onTap: () => cubit.selectCostCenter(c.id),
              ),
          ],
        ),
        if (state.costCenterMissing)
          Padding(
            padding: const EdgeInsets.only(top: AtaSpacing.xxs),
            child: Text(
              l10n.corpCostCenterRequired,
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          ),
      ],
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? AtaColors.brand : AtaColors.white,
      borderRadius: AtaRadii.pillRadius,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.pillRadius,
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AtaSpacing.sm,
            vertical: AtaSpacing.xs,
          ),
          decoration: BoxDecoration(
            borderRadius: AtaRadii.pillRadius,
            border: Border.all(
              color: selected ? AtaColors.brand : AtaColors.line,
            ),
          ),
          child: Text(
            label,
            style: AtaText.label.copyWith(
              color: selected ? AtaColors.white : AtaColors.ink,
            ),
          ),
        ),
      ),
    );
  }
}
