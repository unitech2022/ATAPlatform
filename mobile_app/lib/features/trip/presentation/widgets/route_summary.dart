import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Pickup, extra stops and destination as a compact list.
class RouteSummary extends StatelessWidget {
  const RouteSummary({
    super.key,
    required this.pickup,
    required this.dropoff,
    this.stops = const <TripStop>[],
    this.background = AtaColors.cloud,
  });

  final TripStop pickup;
  final TripStop dropoff;
  final List<TripStop> stops;
  final Color background;

  static const double _dot = 12;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: background,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          _RouteLine(
            label: l10n.pickupTitle,
            value: pickup.name,
            marker: const _Dot(border: AtaColors.brand),
          ),
          for (int i = 0; i < stops.length; i++) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            _RouteLine(
              label: l10n.stopN(i + 1),
              value: stops[i].name,
              marker: const _Dot(border: AtaColors.muted),
            ),
          ],
          const SizedBox(height: AtaSpacing.sm),
          _RouteLine(
            label: l10n.dropoffTitle,
            value: dropoff.name,
            marker: Container(
              width: _dot,
              height: _dot,
              decoration: const BoxDecoration(
                color: AtaColors.ink,
                borderRadius: BorderRadius.all(Radius.circular(2)),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Dot extends StatelessWidget {
  const _Dot({required this.border});

  final Color border;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: RouteSummary._dot,
      height: RouteSummary._dot,
      decoration: BoxDecoration(
        color: AtaColors.white,
        shape: BoxShape.circle,
        border: Border.all(color: border, width: 4),
      ),
    );
  }
}

class _RouteLine extends StatelessWidget {
  const _RouteLine({
    required this.label,
    required this.value,
    required this.marker,
  });

  final String label;
  final String value;
  final Widget marker;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: <Widget>[
        marker,
        const SizedBox(width: AtaSpacing.sm),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Text(label, style: AtaText.caption),
              Text(
                value,
                style: AtaText.bodyStrong,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ],
    );
  }
}
