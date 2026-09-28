import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_request_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_request_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Available balance, IBAN and the amount field of a payout request.
class PayoutRequestForm extends StatelessWidget {
  const PayoutRequestForm({
    super.key,
    required this.state,
    required this.summary,
  });

  final PayoutRequestState state;
  final PayoutSummary summary;

  static const int _maxDigits = 7;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final PayoutRequestCubit cubit = context.read<PayoutRequestCubit>();
    final String minText = l10n.priceWithCurrency(
      Money.compact(summary.minPayoutAmount),
    );
    final String? error = switch (state.amountError) {
      PayoutAmountError.invalid => l10n.payoutAmountInvalid,
      PayoutAmountError.belowMinimum => l10n.payoutBelowMinimumError(
        Money.compact(summary.minPayoutAmount),
      ),
      PayoutAmountError.aboveAvailable => l10n.insufficientBalanceError,
      null => null,
    };
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        _Line(
          label: l10n.payoutAvailable,
          value: l10n.priceWithCurrency(
            Money.fixed(summary.availableForPayout),
          ),
        ),
        _Line(
          label: l10n.payoutIban,
          value: summary.ibanMasked ?? l10n.payoutIbanMissing,
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(l10n.payoutAmountLabel, style: AtaText.labelMuted),
        const SizedBox(height: AtaSpacing.xxs),
        TextFormField(
          key: ValueKey<bool>(summary.canRequest),
          initialValue: state.amountText,
          onChanged: cubit.amountChanged,
          enabled: summary.canRequest && !state.submitting,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          textDirection: TextDirection.ltr,
          style: AtaText.bodyStrong,
          inputFormatters: <TextInputFormatter>[
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
            LengthLimitingTextInputFormatter(_maxDigits),
          ],
          decoration: InputDecoration(
            errorText: error,
            errorStyle: AtaText.caption.copyWith(color: AtaColors.danger),
          ),
        ),
        const SizedBox(height: AtaSpacing.xs),
        Text(l10n.payoutMinimumHint(minText), style: AtaText.caption),
      ],
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
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        children: <Widget>[
          Expanded(child: Text(label, style: AtaText.small)),
          Flexible(
            flex: 2,
            child: Text(
              value,
              textDirection: TextDirection.ltr,
              textAlign: TextAlign.end,
              style: AtaText.bodyStrong,
            ),
          ),
        ],
      ),
    );
  }
}
