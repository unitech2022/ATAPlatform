import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "يبدأ بعد …": the live countdown to the pickup.
class ScheduledCountdownCard extends StatelessWidget {
  const ScheduledCountdownCard({super.key, required this.remaining});

  final Duration remaining;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Container(
      key: const ValueKey<String>('scheduled-countdown'),
      padding: const EdgeInsets.all(AtaSpacing.lg),
      decoration: const BoxDecoration(
        color: AtaColors.brandSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(l10n.scheduledCountdownLabel, style: AtaText.label),
          Text(
            ScheduledText.countdown(l10n, remaining),
            style: AtaText.headline.copyWith(color: AtaColors.brand),
          ),
        ],
      ),
    );
  }
}

/// The estimated fare with the fixed-price (no surge) label.
class ScheduledFareCard extends StatelessWidget {
  const ScheduledFareCard({super.key, required this.trip});

  final ScheduledTrip trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(l10n.scheduledFareLabel, style: AtaText.caption),
          Text(
            l10n.priceWithCurrency(Money.compact(trip.trip.fare)),
            style: AtaText.section,
          ),
          Text(trip.trip.category?.name ?? '', style: AtaText.caption),
          const SizedBox(height: AtaSpacing.xs),
          AtaBadge(label: l10n.scheduleFixedPrice),
        ],
      ),
    );
  }
}

/// The driver who reserved the booking, or when the search starts if none.
class ReservedDriverCard extends StatelessWidget {
  const ReservedDriverCard({super.key, required this.trip});

  final ScheduledTrip trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TripReservationInfo? reservation = trip.reservation;
    if (reservation == null || !reservation.status.isActive) {
      final DateTime? search = trip.scheduling?.searchStartsAt;
      return AtaCard(
        padding: const EdgeInsets.all(AtaSpacing.lg),
        child: Text(
          search == null
              ? l10n.scheduledNoDriverYet
              : l10n.scheduledSearchStartsAt(
                  DateText.dayAndTime(search, context.localeCode),
                ),
          style: AtaText.small,
        ),
      );
    }
    final TripVehicle? vehicle = reservation.vehicle;
    return AtaCard(
      key: const ValueKey<String>('reserved-driver'),
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.scheduledDriverTitle, style: AtaText.caption),
          const SizedBox(height: AtaSpacing.xs),
          Row(
            children: <Widget>[
              const IconBox.cloud(icon: AtaIcons.user, round: true),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      reservation.driverFirstName,
                      style: AtaText.bodyStrong,
                    ),
                    if (reservation.ratingAvg != null)
                      Text(
                        l10n.ratingValue(Money.compact(reservation.ratingAvg!)),
                        style: AtaText.caption,
                      ),
                    if (vehicle != null)
                      Text(
                        '${vehicle.title} · ${vehicle.color} · '
                        '${vehicle.plateNumber}',
                        style: AtaText.caption,
                      ),
                  ],
                ),
              ),
              AtaBadge(
                label: reservation.isConfirmed
                    ? l10n.scheduledDriverConfirmedBadge
                    : l10n.scheduledDriverReservedBadge,
                background: reservation.isConfirmed
                    ? AtaColors.brandSoft
                    : AtaColors.cloud,
                foreground: reservation.isConfirmed
                    ? AtaColors.brand
                    : AtaColors.muted,
              ),
            ],
          ),
        ],
      ),
    );
  }
}
