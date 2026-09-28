import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Distance / ETA / trip-distance chips of the offer sheet.
class OfferStats extends StatelessWidget {
  const OfferStats({super.key, required this.offer});

  final Offer offer;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Row(
      children: <Widget>[
        Expanded(
          child: _Stat(
            icon: AtaIcons.location,
            label: l10n.offerDistanceToPickup,
            value: TripText.distance(l10n, offer.distanceToPickupMeters),
          ),
        ),
        const SizedBox(width: AtaSpacing.xs),
        Expanded(
          child: _Stat(
            icon: AtaIcons.clock,
            label: l10n.offerEta,
            value: l10n.minutesLabel(offer.etaMinutes),
          ),
        ),
        const SizedBox(width: AtaSpacing.xs),
        Expanded(
          child: _Stat(
            icon: AtaIcons.pin,
            label: l10n.offerTripDistance,
            value: TripText.distance(l10n, offer.tripDistanceMeters),
          ),
        ),
      ],
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.icon, required this.label, required this.value});

  final AtaIcons icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.sm),
      decoration: const BoxDecoration(
        color: AtaColors.cloud,
        borderRadius: AtaRadii.smallRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          AtaIcon(icon, size: AtaSizes.iconSmall, color: AtaColors.brand),
          const SizedBox(height: AtaSpacing.xs),
          Text(label, style: AtaText.caption, overflow: TextOverflow.ellipsis),
          Text(
            value,
            style: AtaText.label,
            textDirection: TextDirection.ltr,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

/// Passenger price and the highlighted driver net earnings.
class OfferEarnings extends StatelessWidget {
  const OfferEarnings({super.key, required this.offer});

  final Offer offer;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return DarkCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Row(
        children: <Widget>[
          const IconBox.brand(icon: AtaIcons.wallet, round: true),
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  l10n.offerNetEarnings,
                  style: AtaText.caption.copyWith(color: AtaColors.white70),
                ),
                Text(
                  TripText.price(l10n, offer.driverNetEarnings),
                  style: AtaText.stat.copyWith(color: AtaColors.brand),
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: <Widget>[
              Text(
                l10n.offerPassengerPrice,
                style: AtaText.caption.copyWith(color: AtaColors.white70),
              ),
              Text(
                TripText.price(l10n, offer.passengerPrice),
                style: AtaText.bodyStrong.copyWith(color: AtaColors.white),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
