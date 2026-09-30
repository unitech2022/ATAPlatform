import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_trips_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_trips_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_trip_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/scheduled` ("رحلاتي المجدولة"): the rider's upcoming bookings with
/// their status (waiting for a driver, driver confirmed, …).
class ScheduledTripsPage extends StatelessWidget {
  const ScheduledTripsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<ScheduledTripsCubit>(
      create: (_) => ScheduledTripsCubit(getScheduled: getIt())..load(),
      child: PageWrap(
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: l10n.back,
              onTap: () => context.canPop()
                  ? context.pop()
                  : context.go(AppRoutes.rides),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          ScreenTitle(
            eyebrow: l10n.scheduledTripsEyebrow,
            title: l10n.scheduledTripsTitle,
            copy: l10n.scheduledTripsCopy,
          ),
          const SizedBox(height: AtaSpacing.xl),
          BlocBuilder<ScheduledTripsCubit, ScheduledTripsState>(builder: _body),
        ],
      ),
    );
  }

  Widget _body(BuildContext context, ScheduledTripsState state) {
    final AppLocalizations l10n = context.l10n;
    final ScheduledTripsCubit cubit = context.read<ScheduledTripsCubit>();
    if (state.loading && !state.loaded) return const CenteredLoader();
    if (state.failure != null && state.trips.isEmpty) {
      return FailureView(failure: state.failure!, onRetry: cubit.load);
    }
    if (state.isEmpty) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            l10n.scheduledEmptyTitle,
            style: AtaText.bodyStrong,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            l10n.scheduledEmptyCopy,
            style: AtaText.small,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AtaSpacing.lg),
          AtaButton(
            label: l10n.scheduledBookNew,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.go(AppRoutes.home),
          ),
        ],
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        for (final ScheduledTrip trip in state.trips) ...<Widget>[
          ScheduledTripTile(
            trip: trip,
            onTap: () => context.push(AppRoutes.scheduledTrip(trip.id)),
          ),
          const SizedBox(height: AtaSpacing.sm),
        ],
      ],
    );
  }
}
