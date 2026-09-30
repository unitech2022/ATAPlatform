import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/safety/presentation/widgets/choice_wrap.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/presentation/cubit/fare_dispute_cubit.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "اعتراض على الأجرة" inside a payment-issue ticket: the toggle, the
/// reason picker and the optional requested refund.
class DisputeSection extends StatelessWidget {
  const DisputeSection({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<FareDisputeCubit, FareDisputeState>(
      builder: (BuildContext context, FareDisputeState state) {
        final FareDisputeCubit cubit = context.read<FareDisputeCubit>();
        return AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Row(
                children: <Widget>[
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(
                          l10n.disputeToggleTitle,
                          style: AtaText.bodyStrong,
                        ),
                        Text(l10n.disputeToggleCopy, style: AtaText.caption),
                      ],
                    ),
                  ),
                  AtaToggle(
                    key: const ValueKey<String>('dispute-toggle'),
                    value: state.enabled,
                    onChanged: (bool v) => cubit.toggle(enabled: v),
                  ),
                ],
              ),
              if (state.enabled) ...<Widget>[
                const SizedBox(height: AtaSpacing.md),
                Text(l10n.disputeReasonLabel, style: AtaText.label),
                const SizedBox(height: AtaSpacing.xs),
                ChoiceWrap<DisputeReason>(
                  values: DisputeReason.values,
                  selected: state.reason,
                  label: (DisputeReason r) =>
                      SupportText.disputeReason(l10n, r),
                  onSelected: cubit.selectReason,
                ),
                const SizedBox(height: AtaSpacing.md),
                TextField(
                  key: const ValueKey<String>('dispute-refund'),
                  onChanged: cubit.refundChanged,
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  textDirection: TextDirection.ltr,
                  decoration: InputDecoration(
                    labelText: l10n.disputeRefundLabel,
                    hintText: l10n.disputeRefundHint,
                    errorText: state.refundInvalid
                        ? l10n.disputeRefundInvalid
                        : null,
                  ),
                ),
                const SizedBox(height: AtaSpacing.xs),
                Text(l10n.disputeWindowNote, style: AtaText.caption),
              ],
            ],
          ),
        );
      },
    );
  }
}
