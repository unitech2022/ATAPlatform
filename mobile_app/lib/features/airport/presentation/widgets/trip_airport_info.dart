import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_text.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:flutter/material.dart';

/// Airport details of a trip or an offer: which airport and end, the pickup
/// zone / terminal, the flight number and the free waiting time.
class TripAirportInfo extends StatelessWidget {
  const TripAirportInfo({super.key, required this.airport});

  final TripAirport airport;

  @override
  Widget build(BuildContext context) {
    final String detail = AirportText.tripDetail(context.l10n, airport);
    return Container(
      key: const ValueKey<String>('trip-airport-info'),
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.brandSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Row(
        children: <Widget>[
          const AtaIcon(AtaIcons.location, color: AtaColors.brand),
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  AirportText.tripTitle(context.l10n, airport),
                  style: AtaText.label,
                ),
                if (detail.isNotEmpty) Text(detail, style: AtaText.caption),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
