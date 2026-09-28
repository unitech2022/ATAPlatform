import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Selectable ride categories with ETA and price. Prices come from the fare
/// quote (F10) and fall back to the catalog estimate while it loads.
class RideCategoryList extends StatelessWidget {
  const RideCategoryList({super.key});

  static AtaIcons iconFor(String icon) => switch (icon) {
    'shield' => AtaIcons.shield,
    'pin' => AtaIcons.pin,
    _ => AtaIcons.car,
  };

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<HomeCubit, HomeState>(
      buildWhen: (HomeState p, HomeState c) =>
          p.categories != c.categories ||
          p.selectedCategoryId != c.selectedCategoryId ||
          p.loadingCategories != c.loadingCategories ||
          p.quote != c.quote,
      builder: (BuildContext context, HomeState state) {
        if (state.loadingCategories) return const CenteredLoader();
        if (state.categories.isEmpty) {
          return Text(l10n.categoriesError, style: AtaText.caption);
        }
        final HomeCubit cubit = context.read<HomeCubit>();
        return BlocSelector<QuoteCubit, QuoteState, bool>(
          selector: (QuoteState quote) => quote.isLoading && !quote.hasQuote,
          builder: (BuildContext context, bool pricing) => Column(
            children: <Widget>[
              for (final RideCategory category in state.categories) ...<Widget>[
                _CategoryTile(
                  category: category,
                  quoted: state.quote?.forCategory(category.id),
                  pricing: pricing,
                  selected: category.id == state.selectedCategory?.id,
                  onTap: () => cubit.selectCategory(category.id),
                ),
                const SizedBox(height: AtaSpacing.xs),
              ],
            ],
          ),
        );
      },
    );
  }
}

class _CategoryTile extends StatelessWidget {
  const _CategoryTile({
    required this.category,
    required this.quoted,
    required this.pricing,
    required this.selected,
    required this.onTap,
  });

  final RideCategory category;
  final QuoteCategory? quoted;
  final bool pricing;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final QuoteCategory? quoted = this.quoted;
    final int? eta = quoted != null
        ? quoted.etaMinutes
        : category.estimate?.etaMinutes;
    final bool noDrivers = quoted?.hasNoNearbyDrivers ?? false;
    final double? price = quoted?.total ?? category.estimate?.price;
    final AtaIcons icon = RideCategoryList.iconFor(category.icon);
    return SelectableTile(
      selected: selected,
      onTap: onTap,
      padding: const EdgeInsets.all(AtaSpacing.sm),
      unselectedBackground: AtaColors.cloud,
      unselectedBorder: Colors.transparent,
      child: Row(
        children: <Widget>[
          selected
              ? IconBox.brand(
                  icon: icon,
                  size: AtaSizes.iconBox + 4,
                  iconSize: AtaSizes.iconLarge,
                )
              : IconBox(
                  icon: icon,
                  size: AtaSizes.iconBox + 4,
                  iconSize: AtaSizes.iconLarge,
                  background: AtaColors.white,
                  foreground: AtaColors.ink,
                ),
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Row(
                  children: <Widget>[
                    Text(category.name, style: AtaText.bodyStrong),
                    if (eta != null || noDrivers) ...<Widget>[
                      const SizedBox(width: AtaSpacing.xs),
                      Flexible(
                        child: Text(
                          eta == null
                              ? l10n.noDriversNearby
                              : l10n.minutesLabel(eta),
                          overflow: TextOverflow.ellipsis,
                          style: AtaText.captionStrong.copyWith(
                            color: eta == null
                                ? AtaColors.muted
                                : AtaColors.brand,
                          ),
                        ),
                      ),
                    ],
                  ],
                ),
                Text(category.description, style: AtaText.caption),
              ],
            ),
          ),
          if (pricing && quoted == null)
            Text(l10n.quoteLoading, style: AtaText.caption)
          else if (price != null)
            Text(
              l10n.priceWithCurrency(Money.compact(price)),
              style: AtaText.bodyStrong,
            ),
        ],
      ),
    );
  }
}
