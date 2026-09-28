import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/money_text.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:ata_app/features/wallet/presentation/widgets/top_up_source_picker.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Amount presets, payment source and the confirm button.
class TopUpChooseView extends StatelessWidget {
  const TopUpChooseView({
    super.key,
    required this.state,
    required this.isDriver,
  });

  final TopUpState state;
  final bool isDriver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TopUpCubit cubit = context.read<TopUpCubit>();
    final String amountText = l10n.priceWithCurrency(
      Money.compact(state.amount),
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: PillButton.back(
            label: l10n.back,
            background: AtaColors.cloud,
            elevated: false,
            onTap: () => Navigator.of(context).pop(),
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        Text(
          isDriver ? l10n.settleDebtTitle : l10n.topUp,
          style: AtaText.title,
        ),
        const SizedBox(height: AtaSpacing.sm),
        Text(
          isDriver ? l10n.settleDebtCopy : l10n.topUpCopy,
          style: AtaText.bodyMuted,
        ),
        const SizedBox(height: AtaSpacing.xl),
        DarkCard(
          child: Column(
            children: <Widget>[
              Text(
                l10n.topUpAmount,
                style: AtaText.small.copyWith(color: AtaColors.white60),
              ),
              const SizedBox(height: AtaSpacing.xs),
              MoneyText(
                amount: Money.fixed(state.amount),
                currency: l10n.currency,
                style: AtaText.display.copyWith(color: AtaColors.white),
                alignment: MainAxisAlignment.center,
              ),
            ],
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        Row(
          children: <Widget>[
            for (final double preset in TopUpState.presets) ...<Widget>[
              Expanded(
                child: SelectableTile(
                  selected: state.amount == preset,
                  onTap: () => cubit.selectAmount(preset),
                  padding: const EdgeInsets.symmetric(vertical: AtaSpacing.sm),
                  child: Text(
                    l10n.priceWithCurrency(Money.compact(preset)),
                    textAlign: TextAlign.center,
                    style: AtaText.label.copyWith(
                      color: state.amount == preset
                          ? AtaColors.brand
                          : AtaColors.ink,
                    ),
                  ),
                ),
              ),
              if (preset != TopUpState.presets.last)
                const SizedBox(width: AtaSpacing.sm),
            ],
          ],
        ),
        const SizedBox(height: AtaSpacing.xl),
        TopUpSourcePicker(state: state, allowAddCard: !isDriver),
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
        const SizedBox(height: AtaSpacing.xl),
        AtaButton(
          label: l10n.confirmTopUp(amountText),
          onPressed: state.canConfirm ? cubit.confirm : null,
          loading: state.submitting,
        ),
      ],
    );
  }
}
