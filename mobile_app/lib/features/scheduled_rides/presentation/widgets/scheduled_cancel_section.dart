import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_detail_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancel_reason_sheet.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancellation_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Bottom of the booking page: the free-cancel deadline and the cancel
/// action (the existing F14 cancel flow, with the fee preview), or where the
/// booking went (search started, trip in progress, cancelled).
class ScheduledCancelSection extends StatelessWidget {
  const ScheduledCancelSection({
    super.key,
    required this.trip,
    required this.now,
  });

  final ScheduledTrip trip;
  final DateTime now;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TripStage status = trip.trip.status;
    if (status.isTerminal) {
      final String? fee = CancellationText.passengerCopy(l10n, trip.trip);
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            l10n.scheduledCancelledTitle,
            key: const ValueKey<String>('scheduled-cancelled'),
            style: AtaText.section,
          ),
          if (fee != null) Text(fee, style: AtaText.small),
        ],
      );
    }
    if (status != TripStage.scheduled) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.scheduledSearchStartedCopy, style: AtaText.small),
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: l10n.scheduledTrackTrip,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.go(AppRoutes.trip),
          ),
        ],
      );
    }
    final DateTime? until = trip.freeCancelUntil;
    final bool free = trip.isFreeCancelAt(now);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        if (until != null)
          Text(
            free
                ? l10n.cancelFreeUntil(
                    DateText.fullDayAndTime(until, context.localeCode),
                  )
                : l10n.scheduledLateCancelNote,
            key: const ValueKey<String>('scheduled-free-cancel'),
            style: AtaText.small.copyWith(
              color: free ? AtaColors.brand : AtaColors.warning,
            ),
          ),
        const SizedBox(height: AtaSpacing.sm),
        AtaButton(
          key: const ValueKey<String>('scheduled-cancel'),
          label: l10n.scheduledCancelButton,
          variant: AtaButtonVariant.dangerOutline,
          height: AtaSizes.buttonCompact,
          onPressed: () => _cancel(context, trip.trip),
        ),
      ],
    );
  }

  Future<void> _cancel(BuildContext context, Trip trip) async {
    final ScheduledDetailCubit cubit = context.read<ScheduledDetailCubit>();
    final Trip? cancelled = await CancelReasonSheet.show(
      context,
      trip: trip,
      actor: TripActor.passenger,
    );
    if (cancelled != null) cubit.adopt(cancelled);
  }
}
