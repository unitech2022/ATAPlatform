import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancellation_text.dart';
import 'package:flutter/material.dart';

/// Coloured badge for a reliability [RestrictionLevel].
class ReliabilityLevelBadge extends StatelessWidget {
  const ReliabilityLevelBadge({super.key, required this.level});

  final RestrictionLevel level;

  @override
  Widget build(BuildContext context) {
    final (Color background, Color foreground) = switch (level) {
      RestrictionLevel.none => (AtaColors.brandSoft, AtaColors.brand),
      RestrictionLevel.warning ||
      RestrictionLevel.matchingDeprioritized ||
      RestrictionLevel.incentivesReduced => (
        AtaColors.warningSoft,
        AtaColors.warning,
      ),
      RestrictionLevel.temporarilyRestricted ||
      RestrictionLevel.suspended => (AtaColors.dangerSoft, AtaColors.danger),
    };
    return AtaBadge(
      label: CancellationText.levelLabel(context.l10n, level),
      background: background,
      foreground: foreground,
    );
  }
}
