import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_header.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:ata_app/features/trip/presentation/widgets/route_summary.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

/// Passenger first name + masked call button, fare and the route.
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
            if (passenger?.phoneMasked != null)
              Semantics(
                label: l10n.callPassenger,
                child: HeaderIconButton(
                  background: AtaColors.brandSoft,
                  onTap: () => _call(context, passenger!.phoneMasked!),
                  child: const AtaIcon(AtaIcons.phone, color: AtaColors.brand),
                ),
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

  Future<void> _call(BuildContext context, String phone) async {
    final ScaffoldMessengerState messenger = ScaffoldMessenger.of(context);
    final String failed = context.l10n.callFailed;
    final Uri uri = Uri(scheme: 'tel', path: phone.replaceAll(' ', ''));
    final bool ok = await launchUrl(uri);
    if (!ok) messenger.showSnackBar(SnackBar(content: Text(failed)));
  }
}
