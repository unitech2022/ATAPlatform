import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/favorite_heart_badge.dart';
import 'package:ata_app/features/safety/presentation/widgets/share_trip_button.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/presentation/widgets/trip_contact_actions.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Assigned-driver card: photo placeholder, name, rating, vehicle, plate,
/// ETA chip, a heart when the driver is a favourite (F16), the prominent
/// start PIN, masked call / chat and the tracking link share (F12).
class DriverCard extends StatelessWidget {
  const DriverCard({super.key, required this.trip, this.etaMinutes});

  final Trip trip;
  final int? etaMinutes;

  static const double _pinLetterSpacing = 8;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TripDriver? driver = trip.driver;
    final TripVehicle? vehicle = trip.vehicle;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              const IconBox(
                icon: AtaIcons.user,
                size: AtaSizes.iconBoxLarge,
                iconSize: AtaSizes.iconLarge,
                round: true,
              ),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Row(
                      children: <Widget>[
                        Flexible(
                          child: Text(
                            driver?.fullName ?? l10n.driverGuestName,
                            style: AtaText.bodyStrong,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        if (trip.hasFavoriteDriver) ...<Widget>[
                          const SizedBox(width: AtaSpacing.xxs),
                          const FavoriteHeartBadge(showLabel: false),
                        ],
                      ],
                    ),
                    if (driver != null)
                      Text(
                        l10n.ratingValue(Money.compact(driver.ratingAvg)),
                        style: AtaText.caption,
                      ),
                    if (vehicle != null)
                      Text(
                        l10n.vehicleLine(
                          vehicle.make,
                          vehicle.model,
                          vehicle.color,
                        ),
                        style: AtaText.caption,
                        overflow: TextOverflow.ellipsis,
                      ),
                  ],
                ),
              ),
              const SizedBox(width: AtaSpacing.xs),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: <Widget>[
                  AtaBadge(
                    label: etaMinutes == null
                        ? l10n.etaUnknown
                        : l10n.etaChip(l10n.minutesLabel(etaMinutes!)),
                    background: AtaColors.ink,
                    foreground: AtaColors.white,
                  ),
                  if (vehicle != null) ...<Widget>[
                    const SizedBox(height: AtaSpacing.xs),
                    AtaBadge(
                      label: vehicle.plateNumber,
                      background: AtaColors.cloud,
                      foreground: AtaColors.ink,
                    ),
                  ],
                ],
              ),
            ],
          ),
          if (trip.pin != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.lg),
            Container(
              padding: const EdgeInsets.all(AtaSpacing.md),
              decoration: const BoxDecoration(
                color: AtaColors.brandSoft,
                borderRadius: AtaRadii.itemRadius,
              ),
              child: Column(
                children: <Widget>[
                  Text(
                    l10n.pinTitle,
                    style: AtaText.label.copyWith(color: AtaColors.brand),
                  ),
                  Text(
                    trip.pin!,
                    textDirection: TextDirection.ltr,
                    style: AtaText.title.copyWith(
                      letterSpacing: _pinLetterSpacing,
                    ),
                  ),
                  Text(l10n.pinCopy, style: AtaText.caption),
                ],
              ),
            ),
          ],
          const SizedBox(height: AtaSpacing.lg),
          TripContactActions(tripId: trip.id, actor: TripActor.passenger),
          const SizedBox(height: AtaSpacing.sm),
          ShareTripButton(tripId: trip.id),
        ],
      ),
    );
  }
}
