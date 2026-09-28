import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/widgets/route_summary.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/features/trip_chat/presentation/widgets/trip_contact_actions.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Passenger first name, masked call / chat (F12), fare and the route.
class DriverTripInfo extends StatelessWidget {
  const DriverTripInfo({super.key, required this.trip});

  final Trip trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TripPassenger? passenger = trip.passenger;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          children: <Widget>[
            const IconBox.cloud(icon: AtaIcons.user, round: true),
            const SizedBox(width: AtaSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(
                    passenger?.firstName ?? l10n.offerPassenger,
                    style: AtaText.bodyStrong,
                  ),
                  Text(
                    TripText.price(l10n, trip.fare),
                    style: AtaText.caption,
                    textDirection: TextDirection.ltr,
                  ),
                ],
              ),
            ),
            if (trip.status.hasDriver)
              TripContactActions(
                tripId: trip.id,
                actor: TripActor.driver,
                compact: true,
              ),
          ],
        ),
        const SizedBox(height: AtaSpacing.md),
        RouteSummary(
          pickup: trip.pickup,
          dropoff: trip.dropoff,
          stops: trip.stops,
        ),
      ],
    );
  }
}
