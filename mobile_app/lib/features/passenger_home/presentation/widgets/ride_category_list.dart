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
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Selectable ride categories with ETA and estimated price.
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
          p.loadingCategories != c.loadingCategories,
      builder: (BuildContext context, HomeState state) {
        if (state.loadingCategories) return const CenteredLoader();
        if (state.categories.isEmpty) {
          return Text(l10n.categoriesError, style: AtaText.caption);
        }
        final HomeCubit cubit = context.read<HomeCubit>();
        return Column(
          children: <Widget>[
            for (final RideCategory category in state.categories) ...<Widget>[
              _CategoryTile(
                category: category,
                selected: category.id == state.selectedCategory?.id,
                onTap: () => cubit.selectCategory(category.id),
              ),
              const SizedBox(height: AtaSpacing.xs),
            ],
          ],
        );
      },
    );
  }
}

class _CategoryTile extends StatelessWidget {
  const _CategoryTile({
    required this.category,
    required this.selected,
    required this.onTap,
  });

  final RideCategory category;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final RideEstimate? estimate = category.estimate;
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
                    if (estimate != null) ...<Widget>[
                      const SizedBox(width: AtaSpacing.xs),
                      Text(
                        l10n.minutesLabel(estimate.etaMinutes),
                        style: AtaText.captionStrong.copyWith(
                          color: AtaColors.brand,
                        ),
                      ),
                    ],
                  ],
                ),
                Text(category.description, style: AtaText.caption),
              ],
            ),
          ),
          if (estimate != null)
            Text(
              l10n.priceWithCurrency(Money.compact(estimate.price)),
              style: AtaText.bodyStrong,
            ),
        ],
      ),
    );
  }
}
