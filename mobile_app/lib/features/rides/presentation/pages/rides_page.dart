import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/add_favorite_button.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_sheet.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/presentation/cubit/rides_cubit.dart';
import 'package:ata_app/features/rides/presentation/cubit/rides_state.dart';
import 'package:ata_app/features/rides/presentation/widgets/promo_card.dart';
import 'package:ata_app/features/rides/presentation/widgets/trip_tile.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Trip history with the promo card.
class RidesPage extends StatelessWidget {
  const RidesPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<RidesCubit>(
      create: (_) => RidesCubit(getTrips: getIt())..load(),
      child: PageWrap(
        children: <Widget>[
          ScreenTitle(
            eyebrow: l10n.ridesEyebrow,
            title: l10n.ridesTitle,
            copy: l10n.ridesCopy,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          const _ScheduledLink(),
          const SizedBox(height: AtaSpacing.md),
          const _TripsCard(),
          const SizedBox(height: AtaSpacing.xl),
          const PromoCard(),
        ],
      ),
    );
  }
}

/// Link to "رحلاتي المجدولة" (F17).
class _ScheduledLink extends StatelessWidget {
  const _ScheduledLink();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.md,
        vertical: AtaSpacing.xs,
      ),
      child: SettingRow(
        key: const ValueKey<String>('scheduled-link'),
        leading: const IconBox.cloud(icon: AtaIcons.clock),
        title: l10n.scheduledTripsTitle,
        subtitle: l10n.scheduledLinkCopy,
        last: true,
        onTap: () => context.push(AppRoutes.scheduled),
      ),
    );
  }
}

class _TripsCard extends StatelessWidget {
  const _TripsCard();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Text(l10n.recentTrips, style: AtaText.section),
              PillButton(
                label: l10n.allTrips,
                background: AtaColors.brandSoft,
                foreground: AtaColors.brand,
                elevated: false,
                onTap: context.read<RidesCubit>().load,
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.lg),
          BlocBuilder<RidesCubit, RidesState>(
            builder: (BuildContext context, RidesState state) {
              if (state.loading) return const CenteredLoader();
              if (state.failure != null) {
                return FailureView(
                  failure: state.failure!,
                  onRetry: context.read<RidesCubit>().load,
                );
              }
              if (state.isEmpty) {
                return Column(
                  children: <Widget>[
                    Text(l10n.ridesEmptyTitle, style: AtaText.bodyStrong),
                    const SizedBox(height: AtaSpacing.xxs),
                    Text(
                      l10n.ridesEmptyCopy,
                      style: AtaText.small,
                      textAlign: TextAlign.center,
                    ),
                  ],
                );
              }
              final PendingRatingState ratings = context
                  .watch<PendingRatingCubit>()
                  .state;
              final DateTime now = DateTime.now();
              return Column(
                children: <Widget>[
                  for (final TripSummary trip in state.trips) ...<Widget>[
                    TripTile(
                      trip: trip,
                      favoriteAction:
                          trip.status == TripStatus.completed &&
                              trip.driverName.isNotEmpty
                          ? AddFavoriteButton(tripId: trip.id, compact: true)
                          : null,
                      onTap: trip.status == TripStatus.completed
                          ? () => context.push(AppRoutes.rideReceipt(trip.id))
                          : null,
                      onRate: trip.canRateAt(now) && !ratings.isRated(trip.id)
                          ? () => RatingSheet.show(
                              context,
                              subject: RatingSubject(
                                tripId: trip.id,
                                rater: TripActor.passenger,
                                counterpartName: trip.driverName,
                              ),
                            )
                          : null,
                    ),
                    const SizedBox(height: AtaSpacing.sm),
                  ],
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}
