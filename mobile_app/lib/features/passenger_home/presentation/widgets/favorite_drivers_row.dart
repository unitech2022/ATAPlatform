import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_state.dart';
import 'package:ata_app/features/passenger_home/domain/entities/favorite_selection.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/favorite_chip.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// "السائقون المفضلون" of the request sheet: favourites available now as
/// chips (pick one by name, tap again to deselect), the discount they give,
/// the fallback explanation and the link to the favourites page. Disabled
/// while offering your own price.
class FavoriteDriversRow extends StatelessWidget {
  const FavoriteDriversRow({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        borderRadius: AtaRadii.smallRadius,
        border: Border.all(color: AtaColors.line),
      ),
      child: BlocBuilder<HomeCubit, HomeState>(
        builder: (BuildContext context, HomeState home) =>
            BlocBuilder<AvailableFavoritesCubit, AvailableFavoritesState>(
              builder:
                  (BuildContext context, AvailableFavoritesState available) =>
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: <Widget>[
                          const _Header(),
                          const SizedBox(height: AtaSpacing.xs),
                          ..._body(context, l10n, home, available),
                        ],
                      ),
            ),
      ),
    );
  }

  List<Widget> _body(
    BuildContext context,
    AppLocalizations l10n,
    HomeState home,
    AvailableFavoritesState available,
  ) {
    if (!home.canUseFavorite) {
      return <Widget>[
        Text(l10n.favoriteRowNotWithOffer, style: AtaText.caption),
      ];
    }
    final FavoriteSelection? selected = home.favorite;
    if (available.items.isEmpty && selected == null) {
      return <Widget>[
        Text(
          available.isLoading
              ? l10n.favoriteRowLoading
              : available.status == AvailableFavoritesStatus.ready
              ? l10n.favoriteRowNone
              : l10n.favoriteRowHint,
          style: AtaText.caption,
        ),
      ];
    }
    final HomeCubit cubit = context.read<HomeCubit>();
    return <Widget>[
      SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: <Widget>[
            for (final AvailableFavorite f in available.items) ...<Widget>[
              FavoriteChip(
                key: ValueKey<String>('favorite-chip-${f.driverId}'),
                favorite: f,
                name: f.firstName,
                selected: selected?.driverId == f.driverId,
                onTap: () => cubit.toggleFavorite(f),
              ),
              const SizedBox(width: AtaSpacing.xs),
            ],
            if (selected != null && available.byId(selected.driverId) == null)
              FavoriteChip(
                key: ValueKey<String>('favorite-chip-${selected.driverId}'),
                name: selected.name,
                selected: true,
                onTap: cubit.clearFavorite,
              ),
          ],
        ),
      ),
      if (selected != null) ...<Widget>[
        const SizedBox(height: AtaSpacing.sm),
        _Selected(selection: selected, home: home),
      ],
    ];
  }
}

class _Header extends StatelessWidget {
  const _Header();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Row(
      children: <Widget>[
        const AtaIcon(AtaIcons.heart, color: AtaColors.danger),
        const SizedBox(width: AtaSpacing.sm),
        Expanded(child: Text(l10n.favoriteRowTitle, style: AtaText.label)),
        TextButton(
          key: const ValueKey<String>('favorites-manage'),
          onPressed: () => context.push(AppRoutes.accountFavoriteDrivers),
          child: Text(
            l10n.favoriteRowManage,
            style: AtaText.label.copyWith(color: AtaColors.brand),
          ),
        ),
      ],
    );
  }
}

/// What happens with the selected favourite: who gets the request first, the
/// fallback, the discount and how it combines with a promo code.
class _Selected extends StatelessWidget {
  const _Selected({required this.selection, required this.home});

  final FavoriteSelection selection;
  final HomeState home;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final FareDiscount? line = home.favoriteDiscountLine;
    final bool conditional = line != null
        ? home.quote?.favoriteDiscountConditional ?? false
        : selection.discount != null;
    final String? outcome = switch (home.favoritePromoOutcome) {
      FavoritePromoOutcome.promoNotApplied => l10n.favoritePromoNotStacked,
      FavoritePromoOutcome.favoriteNotApplied => l10n.favoriteLostToPromo,
      FavoritePromoOutcome.none => null,
    };
    return Column(
      key: const ValueKey<String>('favorite-selected'),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Row(
          children: <Widget>[
            Expanded(
              child: Text(
                l10n.favoriteSelectedLine(selection.name),
                style: AtaText.label,
              ),
            ),
            TextButton(
              key: const ValueKey<String>('favorite-deselect'),
              onPressed: context.read<HomeCubit>().clearFavorite,
              child: Text(
                l10n.promoRemove,
                style: AtaText.label.copyWith(color: AtaColors.danger),
              ),
            ),
          ],
        ),
        Text(l10n.favoriteFallbackNote, style: AtaText.caption),
        if (line != null)
          Text(
            l10n.favoriteDiscountSaved(
              l10n.priceWithCurrency(Money.compact(line.amount)),
            ),
            style: AtaText.captionStrong.copyWith(color: AtaColors.brand),
          ),
        if (conditional)
          Text(
            l10n.favoriteDiscountConditional(selection.name),
            style: AtaText.caption,
          ),
        if (outcome != null)
          Text(
            outcome,
            style: AtaText.caption.copyWith(color: AtaColors.warning),
          ),
      ],
    );
  }
}
