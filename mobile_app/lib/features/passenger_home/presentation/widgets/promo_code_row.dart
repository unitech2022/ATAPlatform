import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_state.dart';
import 'package:ata_app/features/promotions/presentation/widgets/promo_code_sheet.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "كود خصم" row of the request sheet: add a code, see the applied
/// discount, remove it. Hidden behind a note while offering a price.
class PromoCodeRow extends StatelessWidget {
  const PromoCodeRow({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<HomeCubit, HomeState>(
      buildWhen: (HomeState p, HomeState c) =>
          p.canUsePromo != c.canUsePromo || p.quoteCategory != c.quoteCategory,
      builder: (BuildContext context, HomeState home) =>
          BlocBuilder<PromoCodeCubit, PromoCodeState>(
            builder: (BuildContext context, PromoCodeState promo) {
              final String? code = promo.appliedCode;
              final QuoteCategory? quoted = home.quoteCategory;
              final double? discount = quoted != null && quoted.hasDiscount
                  ? quoted.breakdown.discount
                  : promo.applied?.discountAmount;
              final String subtitle = !home.canUsePromo
                  ? l10n.promoNotWithOffer
                  : code == null
                  ? l10n.promoAddHint
                  : discount == null || discount <= 0
                  ? l10n.promoAppliedLine(code)
                  : l10n.promoAppliedDiscount(
                      code,
                      l10n.priceWithCurrency(Money.compact(discount)),
                    );
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  _Row(
                    title: l10n.promoCodeLabel,
                    subtitle: subtitle,
                    active: code != null && home.canUsePromo,
                    onTap: home.canUsePromo ? () => _open(context) : null,
                    onRemove: code == null
                        ? null
                        : context.read<PromoCodeCubit>().remove,
                  ),
                  if (promo.failure != null &&
                      promo.status == PromoCodeStatus.error &&
                      home.canUsePromo)
                    Padding(
                      padding: const EdgeInsets.only(top: AtaSpacing.xxs),
                      child: Text(
                        failureText(promo.failure!, l10n),
                        style: AtaText.caption.copyWith(
                          color: AtaColors.danger,
                        ),
                      ),
                    ),
                ],
              );
            },
          ),
    );
  }

  void _open(BuildContext context) {
    final HomeCubit home = context.read<HomeCubit>();
    PromoCodeSheet.show(
      context,
      cubit: context.read<PromoCodeCubit>(),
      draft: () => buildPromoContext(home.state),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.title,
    required this.subtitle,
    required this.active,
    required this.onTap,
    required this.onRemove,
  });

  final String title;
  final String subtitle;
  final bool active;
  final VoidCallback? onTap;
  final VoidCallback? onRemove;

  @override
  Widget build(BuildContext context) {
    return Material(
      type: MaterialType.transparency,
      child: InkWell(
        key: const ValueKey<String>('promo-row'),
        borderRadius: AtaRadii.smallRadius,
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AtaSpacing.md,
            vertical: AtaSpacing.sm,
          ),
          decoration: BoxDecoration(
            color: active ? AtaColors.brandSoft : null,
            borderRadius: AtaRadii.smallRadius,
            border: Border.all(
              color: active ? AtaColors.brand : AtaColors.line,
            ),
          ),
          child: Row(
            children: <Widget>[
              AtaIcon(
                AtaIcons.gift,
                color: onTap == null ? AtaColors.muted : AtaColors.brand,
              ),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(title, style: AtaText.label),
                    Text(subtitle, style: AtaText.caption),
                  ],
                ),
              ),
              if (active && onRemove != null)
                TextButton(
                  onPressed: onRemove,
                  child: Text(
                    context.l10n.promoRemove,
                    style: AtaText.label.copyWith(color: AtaColors.danger),
                  ),
                )
              else
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
  }
}
