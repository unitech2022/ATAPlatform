import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/entities/card_validation.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Card number / expiry / CVC / holder fields bound to [AddCardCubit].
/// Uncontrolled fields: the cubit owns every value.
class CardForm extends StatelessWidget {
  const CardForm({super.key, required this.state});

  final AddCardState state;

  static const int _expiryLength = 5;
  static const int _cvcLength = 4;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final AddCardCubit cubit = context.read<AddCardCubit>();
    final bool enabled = !state.submitting;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        _field(
          label: l10n.cardNumberLabel,
          hint: l10n.cardNumberHint,
          error: state.hasError(CardField.number) ? l10n.cardNumberError : null,
          onChanged: cubit.numberChanged,
          enabled: enabled,
          maxLength: CardValidation.maxNumberLength,
          digitsOnly: true,
        ),
        const SizedBox(height: AtaSpacing.md),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Expanded(
              child: _field(
                label: l10n.cardExpiryLabel,
                hint: l10n.cardExpiryHint,
                error: state.hasError(CardField.expiry)
                    ? l10n.cardExpiryError
                    : null,
                onChanged: cubit.expiryChanged,
                enabled: enabled,
                maxLength: _expiryLength,
                formatter: FilteringTextInputFormatter.allow(RegExp(r'[0-9/]')),
              ),
            ),
            const SizedBox(width: AtaSpacing.md),
            Expanded(
              child: _field(
                label: l10n.cardCvcLabel,
                hint: l10n.cardCvcHint,
                error: state.hasError(CardField.cvc) ? l10n.cardCvcError : null,
                onChanged: cubit.cvcChanged,
                enabled: enabled,
                maxLength: _cvcLength,
                digitsOnly: true,
                obscure: true,
              ),
            ),
          ],
        ),
        const SizedBox(height: AtaSpacing.md),
        _field(
          label: l10n.cardHolderLabel,
          hint: l10n.cardHolderHint,
          onChanged: cubit.holderChanged,
          enabled: enabled,
          number: false,
        ),
        const SizedBox(height: AtaSpacing.md),
        Row(
          children: <Widget>[
            Expanded(child: Text(l10n.cardSetDefault, style: AtaText.label)),
            AtaToggle(
              value: state.setDefault,
              onChanged: enabled ? (_) => cubit.toggleDefault() : null,
            ),
          ],
        ),
      ],
    );
  }

  Widget _field({
    required String label,
    required String hint,
    required ValueChanged<String> onChanged,
    required bool enabled,
    String? error,
    int? maxLength,
    bool digitsOnly = false,
    bool obscure = false,
    bool number = true,
    TextInputFormatter? formatter,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Text(label, style: AtaText.labelMuted),
        const SizedBox(height: AtaSpacing.xxs),
        TextField(
          onChanged: onChanged,
          enabled: enabled,
          obscureText: obscure,
          keyboardType: number ? TextInputType.number : TextInputType.name,
          textDirection: TextDirection.ltr,
          style: AtaText.bodyStrong,
          inputFormatters: <TextInputFormatter>[
            if (digitsOnly) FilteringTextInputFormatter.digitsOnly,
            ?formatter,
            if (maxLength != null) LengthLimitingTextInputFormatter(maxLength),
          ],
          decoration: InputDecoration(
            hintText: hint,
            errorText: error,
            errorStyle: AtaText.caption.copyWith(color: AtaColors.danger),
          ),
        ),
      ],
    );
  }
}
