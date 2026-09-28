import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_check_prompt.dart';
import 'package:ata_app/features/safety/presentation/widgets/sos_button.dart';
import 'package:ata_app/features/safety/presentation/widgets/sos_panel.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter/material.dart';

/// Top overlay of the active trip screens: the SOS button (hold to
/// confirm), the SOS status and the "are you OK?" prompt.
class TripSafetyOverlay extends StatelessWidget {
  const TripSafetyOverlay({
    super.key,
    required this.tripId,
    required this.role,
    this.fallback,
    this.showSos = true,
  });

  final String? tripId;
  final String role;
  final GeoPoint? fallback;
  final bool showSos;

  static const double _sosSize = 56;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AtaSpacing.gutter,
        AtaSpacing.md,
        AtaSpacing.gutter,
        0,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          if (showSos)
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: SosButton(
                tripId: tripId,
                role: role,
                fallback: fallback,
                size: _sosSize,
              ),
            ),
          const SizedBox(height: AtaSpacing.xs),
          const SafetyCheckPrompt(),
          const SizedBox(height: AtaSpacing.xs),
          const SosPanel(),
        ],
      ),
    );
  }
}
