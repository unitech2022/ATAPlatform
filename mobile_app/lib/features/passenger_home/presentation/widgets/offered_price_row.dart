import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Optional "suggest a price" row: a toggle, then -/+ steppers.
class OfferedPriceRow extends StatelessWidget {
  const OfferedPriceRow({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final HomeCubit cubit = context.read<HomeCubit>();
    return BlocSelector<HomeCubit, HomeState, double?>(
      selector: (HomeState state) => state.offeredPrice,
      builder: (BuildContext context, double? price) {
        final bool active = price != null;
        return Container(
          padding: const EdgeInsets.all(AtaSpacing.md),
          decoration: BoxDecoration(
            color: active ? AtaColors.brandSoft : AtaColors.white,
            borderRadius: AtaRadii.itemRadius,
            border: Border.all(
              color: active ? AtaColors.brand : AtaColors.line,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Row(
                children: <Widget>[
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(l10n.offeredPriceLabel, style: AtaText.label),
                        Text(
                          active
                              ? l10n.offeredPriceActive(
                                  l10n.priceWithCurrency(Money.compact(price)),
                                )
                              : l10n.offeredPriceHint,
                          style: AtaText.caption,
                        ),
                      ],
                    ),
                  ),
                  AtaToggle(
                    value: active,
                    onChanged: (_) => cubit.toggleOfferedPrice(),
                  ),
                ],
              ),
              if (active) ...<Widget>[
                const SizedBox(height: AtaSpacing.sm),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: <Widget>[
                    _StepButton(
                      icon: AtaIcons.arrow,
                      mirrored: true,
                      onTap: () =>
                          cubit.adjustOfferedPrice(-HomeCubit.offeredPriceStep),
                    ),
                    Text(
                      l10n.priceWithCurrency(Money.compact(price)),
                      style: AtaText.section,
                      textDirection: TextDirection.ltr,
                    ),
                    _StepButton(
                      icon: AtaIcons.plus,
                      onTap: () =>
                          cubit.adjustOfferedPrice(HomeCubit.offeredPriceStep),
                    ),
                    PillButton(
                      label: l10n.offeredPriceClear,
                      elevated: false,
                      bordered: true,
                      onTap: cubit.toggleOfferedPrice,
                    ),
                  ],
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}

class _StepButton extends StatelessWidget {
  const _StepButton({
    required this.icon,
    required this.onTap,
    this.mirrored = false,
  });

  final AtaIcons icon;
  final VoidCallback onTap;
  final bool mirrored;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AtaColors.white,
      shape: const CircleBorder(side: BorderSide(color: AtaColors.line)),
      child: InkWell(
        onTap: onTap,
        customBorder: const CircleBorder(),
        child: SizedBox(
          width: AtaSizes.iconBox,
          height: AtaSizes.iconBox,
          child: Center(
            child: AtaIcon(icon, color: AtaColors.ink, mirrored: mirrored),
          ),
        ),
      ),
    );
  }
}
