import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/progress_bar.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/presentation/widgets/route_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "Your trip is in progress": progress bar and the route.
class TripProgressView extends StatelessWidget {
  const TripProgressView({super.key, required this.trip});

  final Trip trip;

  static const double _startedProgress = 0.15;
  static const double _ridingProgress = 0.6;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool riding = trip.status == TripStage.inTrip;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          children: <Widget>[
            const IconBox.brand(
              icon: AtaIcons.car,
              size: AtaSizes.iconBoxLarge,
              iconSize: AtaSizes.iconLarge,
            ),
            const SizedBox(width: AtaSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(
                    riding ? l10n.inTripTitle : l10n.readyToStartTitle,
                    style: AtaText.headline,
                  ),
                  Text(
                    riding ? l10n.inTripCopy : l10n.readyToStartCopy,
                    style: AtaText.small,
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: AtaSpacing.lg),
        ProgressBar(
          value: riding ? _ridingProgress : _startedProgress,
          track: AtaColors.cloud,
        ),
        const SizedBox(height: AtaSpacing.lg),
        RouteSummary(
          pickup: trip.pickup,
          dropoff: trip.dropoff,
          stops: trip.stops,
        ),
      ],
    );
  }
}
