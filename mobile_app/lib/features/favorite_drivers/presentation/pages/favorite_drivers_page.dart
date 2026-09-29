import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_state.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/favorite_drivers_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/favorite_drivers_state.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/favorite_driver_tile.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/remove_favorite_dialog.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/account/favorite-drivers`: my favourite drivers with who is available
/// now (and their ETA) and removal with confirmation.
class FavoriteDriversPage extends StatelessWidget {
  const FavoriteDriversPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<FavoriteDriversCubit>(
          create: (_) => FavoriteDriversCubit(
            getFavorites: getIt(),
            removeFavorite: getIt(),
          )..load(),
        ),
        BlocProvider<AvailableFavoritesCubit>(
          lazy: false,
          create: (_) =>
              AvailableFavoritesCubit(getAvailable: getIt())
                ..watch(TripPlaces.currentLocation),
        ),
      ],
      child: BlocListener<FavoriteDriversCubit, FavoriteDriversState>(
        listenWhen: (FavoriteDriversState p, FavoriteDriversState c) =>
            c.lastRemoved != null && p.lastRemoved != c.lastRemoved,
        listener: (BuildContext context, FavoriteDriversState state) =>
            ScaffoldMessenger.maybeOf(context)?.showSnackBar(
              SnackBar(
                content: Text(
                  l10n.favoriteRemovedSnack(state.lastRemoved!.firstName),
                ),
              ),
            ),
        child: PageWrap(
          children: <Widget>[
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: PillButton.back(
                label: l10n.back,
                onTap: () => context.canPop()
                    ? context.pop()
                    : context.go(AppRoutes.account),
              ),
            ),
            const SizedBox(height: AtaSpacing.xl),
            ScreenTitle(
              eyebrow: l10n.accountEyebrow,
              title: l10n.favoriteDriversTitle,
              copy: l10n.favoriteDriversCopy,
            ),
            const SizedBox(height: AtaSpacing.xl),
            const _Body(),
          ],
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final FavoriteDriversCubit cubit = context.read<FavoriteDriversCubit>();
    return BlocBuilder<FavoriteDriversCubit, FavoriteDriversState>(
      builder: (BuildContext context, FavoriteDriversState state) {
        if (state.loading && !state.loaded) return const CenteredLoader();
        if (state.failure != null && !state.loaded) {
          return FailureView(failure: state.failure!, onRetry: cubit.load);
        }
        if (state.isEmpty) return const _Empty();
        final AvailableFavoritesState available = context
            .watch<AvailableFavoritesCubit>()
            .state;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            if (state.failure != null) ...<Widget>[
              InlineError(message: failureText(state.failure!, l10n)),
              const SizedBox(height: AtaSpacing.sm),
            ],
            for (final FavoriteDriver driver in state.items) ...<Widget>[
              FavoriteDriverTile(
                driver: driver,
                available: available.byId(driver.driverId),
                removing: state.removingId == driver.driverId,
                onRemove: state.removingId == null
                    ? () => _confirmRemove(context, driver)
                    : null,
              ),
              const SizedBox(height: AtaSpacing.sm),
            ],
          ],
        );
      },
    );
  }

  Future<void> _confirmRemove(
    BuildContext context,
    FavoriteDriver driver,
  ) async {
    final FavoriteDriversCubit cubit = context.read<FavoriteDriversCubit>();
    final bool ok = await RemoveFavoriteDialog.show(
      context,
      name: driver.firstName,
    );
    if (ok) await cubit.remove(driver);
  }
}

class _Empty extends StatelessWidget {
  const _Empty();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      child: Column(
        children: <Widget>[
          Text(l10n.favoriteEmptyTitle, style: AtaText.bodyStrong),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            l10n.favoriteEmptyCopy,
            style: AtaText.small,
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
