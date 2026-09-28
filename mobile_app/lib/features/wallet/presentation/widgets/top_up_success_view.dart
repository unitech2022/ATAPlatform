import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/money_text.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Success step of the top-up; pops with the new balance.
class TopUpSuccessView extends StatelessWidget {
  const TopUpSuccessView({super.key, required this.state});

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
