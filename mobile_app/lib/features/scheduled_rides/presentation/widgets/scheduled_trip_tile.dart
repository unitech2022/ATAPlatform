import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/bordered_row.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Colour of a booking status.
Color scheduledPhaseColor(ScheduledPhase phase) => switch (phase) {
  ScheduledPhase.waitingForDriver => AtaColors.warning,
  ScheduledPhase.driverReserved => AtaColors.ink,
  ScheduledPhase.driverConfirmed ||
  ScheduledPhase.searching ||
  ScheduledPhase.inProgress => AtaColors.brand,
  ScheduledPhase.ended => AtaColors.muted,
};

/// One booking of "رحلاتي المجدولة": destination, status, date and fare.
class ScheduledTripTile extends StatelessWidget {
  const ScheduledTripTile({super.key, required this.trip, this.onTap});

  final ScheduledTrip trip;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final ScheduledPhase phase = trip.phase;
    final DateTime? at = trip.scheduledAt;
    final String? driver = trip.reservation?.driverFirstName;
    final String when = at == null
        ? ''
        : DateText.fullDayAndTime(at, context.localeCode);
    return BorderedRow(
      key: ValueKey<String>('scheduled-${trip.id}'),
      onTap: onTap,
      leading: const IconBox.cloud(icon: AtaIcons.clock),
      title: trip.trip.dropoff.name,
      titleTrailing: Flexible(
        child: Text(
          ScheduledText.phase(l10n, phase),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: AtaText.captionStrong.copyWith(
            color: scheduledPhaseColor(phase),
          ),
        ),
      ),
      subtitle: driver == null || driver.isEmpty ? when : '$when · $driver',
      trailing: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: <Widget>[
          Text(
            l10n.priceWithCurrency(Money.compact(trip.trip.fare)),
            style: AtaText.bodyStrong,
          ),
          const SizedBox(height: AtaSpacing.xxs),
          const AtaIcon(
            AtaIcons.chevron,
            size: AtaSizes.iconSmall,
            color: AtaColors.muted,
          ),
        ],
      ),
    );
  }
}
