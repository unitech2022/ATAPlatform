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
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Colour of a reservation status.
Color reservationStatusColor(ReservationStatus status) => switch (status) {
  ReservationStatus.confirmed ||
  ReservationStatus.assigned ||
  ReservationStatus.completed => AtaColors.brand,
  ReservationStatus.reserved => AtaColors.warning,
  ReservationStatus.released ||
  ReservationStatus.noShow ||
  ReservationStatus.cancelled => AtaColors.danger,
  ReservationStatus.unknown => AtaColors.muted,
};

/// One reservation of "حجوزاتي": destination, status, date and earnings.
class ReservationTile extends StatelessWidget {
  const ReservationTile({super.key, required this.reservation, this.onTap});

  final Reservation reservation;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final Reservation r = reservation;
    return BorderedRow(
      key: ValueKey<String>('reservation-${r.tripId}'),
      onTap: onTap,
      leading: const IconBox.cloud(icon: AtaIcons.clock),
      title: r.dropoff?.name ?? l10n.reservationDetailEyebrow,
      titleTrailing: Flexible(
        child: Text(
          ScheduledText.reservation(l10n, r.status),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: AtaText.captionStrong.copyWith(
            color: reservationStatusColor(r.status),
          ),
        ),
      ),
      subtitle: DateText.fullDayAndTime(r.scheduledAt, context.localeCode),
      trailing: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: <Widget>[
          Text(
            l10n.priceWithCurrency(Money.compact(r.driverNetEarnings)),
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
