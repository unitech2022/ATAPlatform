import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/bordered_row.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/presentation/widgets/trip_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "Recent trips" list for drivers, with an empty state.
class RecentTripsCard extends StatelessWidget {
  const RecentTripsCard({
    super.key,
    required this.trips,
    required this.loading,
  });

  final List<TripSummary> trips;
  final bool loading;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Text(l10n.recentTrips, style: AtaText.section),
              Text(
                l10n.viewAll,
                style: AtaText.label.copyWith(color: AtaColors.brand),
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.lg),
          if (loading)
            const CenteredLoader()
          else if (trips.isEmpty)
            Text(
              l10n.driverTripsEmpty,
              style: AtaText.small,
              textAlign: TextAlign.center,
            )
          else
            for (final TripSummary trip in trips) ...<Widget>[
              _DriverTripRow(trip: trip),
              const SizedBox(height: AtaSpacing.sm),
            ],
        ],
      ),
    );
  }
}

class _DriverTripRow extends StatelessWidget {
  const _DriverTripRow({required this.trip});

  final TripSummary trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final (String status, _) = tripStatusLabel(l10n, trip.status);
    final DateTime? date = trip.displayDate;
    final String when = date == null
        ? ''
        : DateText.dayAndTime(date, context.localeCode);
    return BorderedRow(
      leading: const IconBox.cloud(icon: AtaIcons.pin),
      title: l10n.tripRoute(trip.pickupName, trip.destinationName),
      subtitle: when.isEmpty ? status : '$when · $status',
      trailing: Text(
        l10n.priceWithCurrency(Money.compact(trip.fare)),
        style: AtaText.bodyStrong,
      ),
    );
  }
}
