import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/confirm_prompt_card.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/release_dialog.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/features/trip/presentation/widgets/route_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Body of a reservation: the confirmation prompt, the countdown to the
/// pickup, the route, earnings and the release action.
class ReservationDetails extends StatelessWidget {
  const ReservationDetails({
    super.key,
    required this.reservation,
    required this.state,
  });

  final Reservation reservation;
  final ReservationsState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final ReservationsCubit cubit = context.read<ReservationsCubit>();
    final Reservation r = reservation;
    final Duration left = r.scheduledAt.difference(state.now);
    final DateTime? freeUntil = r.freeReleaseUntil;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        ConfirmPromptCard(
          reservation: r,
          now: state.now,
          busy: state.busyTripId == r.tripId,
          onConfirm: () => cubit.confirm(r.tripId),
        ),
        if (r.pendingAt(state.now) != null)
          const SizedBox(height: AtaSpacing.md),
        if (r.isActive && !left.isNegative) ...<Widget>[
          Container(
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: const BoxDecoration(
              color: AtaColors.brandSoft,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(l10n.scheduledCountdownLabel, style: AtaText.label),
                Text(
                  ScheduledText.countdown(l10n, left),
                  key: const ValueKey<String>('reservation-countdown'),
                  style: AtaText.headline.copyWith(color: AtaColors.brand),
                ),
              ],
            ),
          ),
          const SizedBox(height: AtaSpacing.md),
        ],
        if (r.source == 'favorite') ...<Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: AtaBadge(
              key: const ValueKey<String>('reservation-favorite'),
              label: l10n.offerFavoriteRequest,
              background: AtaColors.dangerSoft,
              foreground: AtaColors.danger,
            ),
          ),
          const SizedBox(height: AtaSpacing.sm),
        ],
        if (r.pickup != null && r.dropoff != null)
          RouteSummary(pickup: r.pickup!, dropoff: r.dropoff!),
        const SizedBox(height: AtaSpacing.md),
        AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              _Line(
                label: l10n.reservationPassenger,
                value: r.passengerFirstName.isEmpty
                    ? l10n.offerPassenger
                    : r.passengerFirstName,
              ),
              _Line(
                label: l10n.reservationFare,
                value: l10n.priceWithCurrency(Money.compact(r.estimatedFare)),
              ),
              _Line(
                label: l10n.marketNetEarnings,
                value: l10n.priceWithCurrency(
                  Money.compact(r.driverNetEarnings),
                ),
                strong: true,
              ),
              if (freeUntil != null && r.canRelease)
                _Line(
                  label: l10n.reservationFreeReleaseLabel,
                  value: DateText.fullDayAndTime(freeUntil, context.localeCode),
                ),
            ],
          ),
        ),
        if (r.canRelease) ...<Widget>[
          const SizedBox(height: AtaSpacing.lg),
          AtaButton(
            key: const ValueKey<String>('release-reservation'),
            label: l10n.reservationReleaseAction,
            variant: AtaButtonVariant.dangerOutline,
            height: AtaSizes.buttonCompact,
            loading: state.busyTripId == r.tripId,
            onPressed: state.isBusy
                ? null
                : () async {
                    final bool ok = await ReleaseDialog.show(
                      context,
                      reservation: r,
                      now: state.now,
                    );
                    if (ok) cubit.release(r.tripId);
                  },
          ),
        ],
      ],
    );
  }
}

class _Line extends StatelessWidget {
  const _Line({required this.label, required this.value, this.strong = false});

  final String label;
  final String value;
  final bool strong;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Expanded(child: Text(label, style: AtaText.small)),
          const SizedBox(width: AtaSpacing.sm),
          Flexible(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: strong
                  ? AtaText.bodyStrong.copyWith(color: AtaColors.brand)
                  : AtaText.bodyStrong,
            ),
          ),
        ],
      ),
    );
  }
}
