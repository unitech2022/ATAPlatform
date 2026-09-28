import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/money_text.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Choose an amount, confirm, then see the success screen. Pops with the
/// new balance.
class TopUpPage extends StatelessWidget {
  const TopUpPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<TopUpCubit>(
      create: (_) => TopUpCubit(topUpWallet: getIt()),
      child: PageWrap(
        center: true,
        children: <Widget>[
          AtaCard(
            shadow: AtaShadows.panel,
            child: BlocBuilder<TopUpCubit, TopUpState>(
              builder: (BuildContext context, TopUpState state) =>
                  state.step == TopUpStep.success
                  ? _SuccessView(state: state)
                  : _ChooseView(state: state),
            ),
          ),
        ],
      ),
    );
  }
}

class _ChooseView extends StatelessWidget {
  const _ChooseView({required this.state});

  final TopUpState state;

  static const String _cardDigits = '2841';

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
        Text(l10n.topUp, style: AtaText.title),
        const SizedBox(height: AtaSpacing.sm),
        Text(l10n.topUpCopy, style: AtaText.bodyMuted),
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
        SettingRow(
          leading: const IconBox.cloud(icon: AtaIcons.wallet),
          title: l10n.madaCard,
          subtitle: l10n.cardEnding(_cardDigits),
          last: true,
          verticalPadding: 0,
        ),
        const SizedBox(height: AtaSpacing.xs),
        Text(l10n.sandboxCopy, style: AtaText.caption),
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

class _SuccessView extends StatelessWidget {
  const _SuccessView({required this.state});

  final TopUpState state;

  static const double _checkSize = 80;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String added = l10n.priceWithCurrency(Money.compact(state.amount));
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const SizedBox(height: AtaSpacing.xl),
        const Center(
          child: IconBox(
            icon: AtaIcons.check,
            size: _checkSize,
            iconSize: AtaSizes.iconHero + AtaSizes.iconSmall / 2,
            round: true,
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        Text(
          l10n.topUpSuccessTitle,
          style: AtaText.title,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.sm),
        Text(
          l10n.topUpSuccessCopy(added),
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xl),
        Text(
          l10n.newBalance,
          style: AtaText.labelMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xs),
        MoneyText(
          amount: Money.fixed(state.newBalance ?? 0),
          currency: l10n.currency,
          style: AtaText.display.copyWith(color: AtaColors.brand),
          alignment: MainAxisAlignment.center,
        ),
        const SizedBox(height: AtaSpacing.xxl),
        AtaButton(
          label: l10n.backToWallet,
          onPressed: () => Navigator.of(context).pop(state.newBalance),
        ),
      ],
    );
  }
}
