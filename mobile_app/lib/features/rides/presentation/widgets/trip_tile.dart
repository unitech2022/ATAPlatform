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
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Label and color for a [TripStatus].
(String, Color) tripStatusLabel(AppLocalizations l10n, TripStatus status) =>
    switch (status) {
      TripStatus.completed => (l10n.tripStatusCompleted, AtaColors.brand),
      TripStatus.cancelled => (l10n.tripStatusCancelled, AtaColors.muted),
      TripStatus.active => (l10n.tripStatusActive, AtaColors.ink),
    };

/// One trip in a list: car box, place, status, date and fare.
class TripTile extends StatelessWidget {
  const TripTile({super.key, required this.trip});

  final TripSummary trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final (String statusText, Color statusColor) = tripStatusLabel(
      l10n,
      trip.status,
    );
    final DateTime? date = trip.displayDate;
    return BorderedRow(
      leading: const IconBox.cloud(
        icon: AtaIcons.car,
        size: AtaSizes.iconBox + 4,
        iconSize: AtaSizes.iconMedium,
        radius: AtaSpacing.md,
      ),
      title: trip.destinationName,
      titleTrailing: Text(
        statusText,
        style: AtaText.captionStrong.copyWith(color: statusColor),
      ),
      subtitle: date == null
          ? trip.categoryName
          : DateText.dayAndTime(date, context.localeCode),
      trailing: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: <Widget>[
          Text(
            l10n.priceWithCurrency(Money.compact(trip.fare)),
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
