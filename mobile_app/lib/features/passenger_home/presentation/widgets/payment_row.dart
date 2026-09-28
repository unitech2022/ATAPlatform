import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Label for a [PaymentOption].
String paymentLabel(AppLocalizations l10n, PaymentOption option) =>
    switch (option) {
      PaymentOption.cash => l10n.paymentCash,
      PaymentOption.wallet => l10n.paymentWallet,
      PaymentOption.card => l10n.paymentCard,
    };

/// Payment-method row that opens a chooser sheet.
class PaymentRow extends StatelessWidget {
  const PaymentRow({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocSelector<HomeCubit, HomeState, PaymentOption>(
      selector: (HomeState state) => state.payment,
      builder: (BuildContext context, PaymentOption payment) {
        return Material(
          type: MaterialType.transparency,
          child: InkWell(
            borderRadius: AtaRadii.smallRadius,
            onTap: () => _choose(context),
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: AtaSpacing.md,
                vertical: AtaSpacing.sm,
              ),
              decoration: BoxDecoration(
                borderRadius: AtaRadii.smallRadius,
                border: Border.all(color: AtaColors.line),
              ),
              child: Row(
                children: <Widget>[
                  const AtaIcon(AtaIcons.wallet, color: AtaColors.brand),
                  const SizedBox(width: AtaSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(l10n.paymentMethod, style: AtaText.label),
                        Text(
                          paymentLabel(l10n, payment),
                          style: AtaText.caption,
                        ),
                      ],
                    ),
                  ),
                  const AtaIcon(
                    AtaIcons.chevron,
                    size: AtaSizes.iconSmall,
                    color: AtaColors.muted,
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Future<void> _choose(BuildContext context) {
    final HomeCubit cubit = context.read<HomeCubit>();
    return showModalBottomSheet<void>(
      context: context,
      builder: (BuildContext sheetContext) => Padding(
        padding: const EdgeInsets.all(AtaSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            const SheetHandle(),
            Text(context.l10n.paymentMethod, style: AtaText.section),
            const SizedBox(height: AtaSpacing.md),
            for (final PaymentOption option
                in PaymentOption.values) ...<Widget>[
              SelectableTile(
                selected: option == cubit.state.payment,
                onTap: () {
                  cubit.selectPayment(option);
                  Navigator.of(sheetContext).pop();
                },
                child: Row(
                  children: <Widget>[
                    Expanded(
                      child: Text(
                        paymentLabel(context.l10n, option),
                        style: AtaText.bodyStrong,
                      ),
                    ),
                    RadioDot(selected: option == cubit.state.payment),
                  ],
                ),
              ),
              const SizedBox(height: AtaSpacing.xs),
            ],
          ],
        ),
      ),
    );
  }
}
