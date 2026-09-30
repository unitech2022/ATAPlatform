import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/airport/presentation/widgets/trip_airport_info.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_detail_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_detail_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_cancel_section.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_trip_sections.dart';
import 'package:ata_app/features/trip/presentation/widgets/route_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/scheduled/:tripId`: the booking with its date, the countdown, the
/// reserved driver, the free-cancel deadline and the cancel action.
class ScheduledTripPage extends StatelessWidget {
  const ScheduledTripPage({super.key, required this.tripId});

  final String tripId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<ScheduledDetailCubit>(
      create: (_) =>
          ScheduledDetailCubit(tripId: tripId, getTrip: getIt())..load(),
      child: PageWrap(
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: l10n.back,
              onTap: () => context.canPop()
                  ? context.pop()
                  : context.go(AppRoutes.scheduled),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          BlocBuilder<ScheduledDetailCubit, ScheduledDetailState>(
            builder: _body,
          ),
        ],
      ),
    );
  }

  Widget _body(BuildContext context, ScheduledDetailState state) {
    final AppLocalizations l10n = context.l10n;
    final ScheduledTrip? trip = state.trip;
    if (trip == null) {
      return state.failure == null
          ? const CenteredLoader()
          : FailureView(
              failure: state.failure!,
              onRetry: context.read<ScheduledDetailCubit>().load,
            );
    }
    final DateTime? at = trip.scheduledAt;
    final ScheduledPhase phase = trip.phase;
    final Duration? remaining = state.remaining;
    final bool upcoming = trip.isWaiting && remaining != null;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        ScreenTitle(
          eyebrow: l10n.scheduledDetailEyebrow,
          title: at == null
              ? trip.trip.tripNumber
              : DateText.fullDayAndTime(at, context.localeCode),
          copy: ScheduledText.phase(l10n, phase),
        ),
        const SizedBox(height: AtaSpacing.lg),
        if (upcoming) ...<Widget>[
          ScheduledCountdownCard(remaining: remaining),
          const SizedBox(height: AtaSpacing.md),
        ],
        RouteSummary(
          pickup: trip.trip.pickup,
          dropoff: trip.trip.dropoff,
          stops: trip.trip.stops,
        ),
        if (trip.trip.airport != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          TripAirportInfo(airport: trip.trip.airport!),
        ],
        const SizedBox(height: AtaSpacing.md),
        ScheduledFareCard(trip: trip),
        if (phase != ScheduledPhase.ended) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          ReservedDriverCard(trip: trip),
        ],
        const SizedBox(height: AtaSpacing.lg),
        ScheduledCancelSection(trip: trip, now: state.now),
      ],
    );
  }
}
