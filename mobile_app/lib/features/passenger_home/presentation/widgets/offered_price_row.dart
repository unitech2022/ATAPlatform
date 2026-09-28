import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Optional "suggest a price" row: a toggle, then a slider bounded by the
/// quote's `offerMin..offerMax` with -/+ 1 SAR buttons.
class OfferedPriceRow extends StatelessWidget {
  const OfferedPriceRow({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final HomeCubit cubit = context.read<HomeCubit>();
    return BlocBuilder<HomeCubit, HomeState>(
      buildWhen: (HomeState p, HomeState c) =>
          p.offeredPrice != c.offeredPrice || p.offerBounds != c.offerBounds,
      builder: (BuildContext context, HomeState state) {
        final double? price = state.offeredPrice;
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
              if (active)
                _OfferSlider(
                  price: price,
                  bounds: state.offerBounds,
                  onChanged: cubit.setOfferedPrice,
                  onStep: cubit.adjustOfferedPrice,
                ),
            ],
          ),
        );
      },
    );
  }
}

class _OfferSlider extends StatelessWidget {
  const _OfferSlider({
    required this.price,
    required this.bounds,
    required this.onChanged,
    required this.onStep,
  });

  final double price;
  final OfferBounds bounds;
  final ValueChanged<double> onChanged;
  final ValueChanged<double> onStep;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final double value = bounds.clamp(price);
    final int divisions = (bounds.max - bounds.min).round();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const SizedBox(height: AtaSpacing.xs),
        Row(
          children: <Widget>[
            _StepButton(
              icon: AtaIcons.arrow,
              mirrored: true,
              tooltip: l10n.offeredPriceDecrease,
              onTap: value > bounds.min
                  ? () => onStep(-HomeCubit.offeredPriceStep)
                  : null,
            ),
            Expanded(
              child: bounds.isValid
                  ? Slider(
                      value: value,
                      min: bounds.min,
                      max: bounds.max,
                      divisions: divisions > 0 ? divisions : null,
                      activeColor: AtaColors.brand,
                      inactiveColor: AtaColors.line,
                      label: Money.compact(value),
                      onChanged: onChanged,
                    )
                  : Text(
                      l10n.priceWithCurrency(Money.compact(value)),
                      style: AtaText.section,
                      textAlign: TextAlign.center,
                      textDirection: TextDirection.ltr,
                    ),
            ),
            _StepButton(
              icon: AtaIcons.plus,
              tooltip: l10n.offeredPriceIncrease,
              onTap: value < bounds.max
                  ? () => onStep(HomeCubit.offeredPriceStep)
                  : null,
            ),
          ],
        ),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: <Widget>[
            Text(
              l10n.offeredPriceMin(
                l10n.priceWithCurrency(Money.compact(bounds.min)),
              ),
              style: AtaText.caption,
            ),
            Text(
              l10n.offeredPriceMax(
                l10n.priceWithCurrency(Money.compact(bounds.max)),
              ),
              style: AtaText.caption,
            ),
          ],
        ),
      ],
    );
  }
}

class _StepButton extends StatelessWidget {
  const _StepButton({
    required this.icon,
    required this.onTap,
    required this.tooltip,
    this.mirrored = false,
  });

  final AtaIcons icon;
  final VoidCallback? onTap;
  final String tooltip;
  final bool mirrored;

  @override
  Widget build(BuildContext context) {
    final bool enabled = onTap != null;
    return Tooltip(
      message: tooltip,
      child: Material(
        color: AtaColors.white,
        shape: const CircleBorder(side: BorderSide(color: AtaColors.line)),
        child: InkWell(
          onTap: onTap,
          customBorder: const CircleBorder(),
          child: SizedBox(
            width: AtaSizes.iconBox,
            height: AtaSizes.iconBox,
            child: Center(
              child: AtaIcon(
                icon,
                color: enabled ? AtaColors.ink : AtaColors.muted,
                mirrored: mirrored,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
