import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One marketplace request: time, badges, pickup and dropoff areas (the
/// pickup is approximate until reserved), distances, net earnings and the
/// reserve action.
class MarketplaceTile extends StatelessWidget {
  const MarketplaceTile({
    super.key,
    required this.trip,
    required this.reserving,
    required this.onReserve,
  });

  final MarketplaceTrip trip;

  /// This trip is being reserved right now.
  final bool reserving;

  /// `null` while another reservation is in progress.
  final VoidCallback? onReserve;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String locale = context.localeCode;
    final DateTime? exclusive = trip.exclusiveUntil;
    return AtaCard(
      key: ValueKey<String>('market-${trip.tripId}'),
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(
                  DateText.fullDayAndTime(trip.scheduledAt, locale),
                  style: AtaText.bodyStrong,
                ),
              ),
              Text(
                l10n.priceWithCurrency(Money.compact(trip.driverNetEarnings)),
                style: AtaText.section.copyWith(color: AtaColors.brand),
              ),
            ],
          ),
          Text(l10n.marketNetEarnings, style: AtaText.caption),
          const SizedBox(height: AtaSpacing.xs),
          Wrap(
            spacing: AtaSpacing.xs,
            runSpacing: AtaSpacing.xs,
            children: <Widget>[
              if (trip.categoryName.isNotEmpty)
                AtaBadge(
                  label: trip.categoryName,
                  background: AtaColors.cloud,
                  foreground: AtaColors.ink,
                ),
              if (trip.isAirport) AtaBadge(label: l10n.marketAirportBadge),
              if (trip.isFavoriteRequest)
                AtaBadge(
                  label: l10n.offerFavoriteRequest,
                  background: AtaColors.dangerSoft,
                  foreground: AtaColors.danger,
                ),
            ],
          ),
          const SizedBox(height: AtaSpacing.sm),
          Text(
            '${trip.pickupArea}  ←  ${trip.dropoffArea}',
            style: AtaText.body,
          ),
          Text(l10n.marketApproxPickup, style: AtaText.caption),
          const SizedBox(height: AtaSpacing.xs),
          Text(
            '${l10n.marketDistanceToPickup(Money.compact(trip.distanceToPickupKm))}'
            ' · ${TripText.distance(l10n, trip.tripDistanceMeters)}'
            ' · ${l10n.marketFare(TripText.price(l10n, trip.estimatedFare))}',
            style: AtaText.small,
          ),
          if (exclusive != null)
            Text(
              l10n.marketExclusiveUntil(DateText.time(exclusive, locale)),
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          const SizedBox(height: AtaSpacing.md),
          AtaButton(
            key: ValueKey<String>('reserve-${trip.tripId}'),
            label: l10n.marketReserve,
            height: AtaSizes.buttonCompact,
            loading: reserving,
            onPressed: onReserve,
          ),
        ],
      ),
    );
  }
}
