import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Confirmation before a driver gives a reservation back. Releasing before
/// `freeReleaseUntil` is free; a later release warns about the reliability
/// points it costs. Resolves to `true` when confirmed.
abstract final class ReleaseDialog {
  static Future<bool> show(
    BuildContext context, {
    required Reservation reservation,
    required DateTime now,
  }) async {
    final AppLocalizations l10n = context.l10n;
    final bool late = reservation.isLateReleaseAt(now);
    final DateTime? until = reservation.freeReleaseUntil;
    final bool? ok = await showDialog<bool>(
      context: context,
      builder: (BuildContext dialog) => AlertDialog(
        title: Text(l10n.reservationReleaseTitle),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            if (late)
              Text(
                l10n.reservationReleaseLateCopy,
                key: const ValueKey<String>('release-late-warning'),
                style: AtaText.small.copyWith(color: AtaColors.danger),
              )
            else if (until != null)
              Text(
                l10n.reservationReleaseFreeCopy(
                  DateText.fullDayAndTime(until, context.localeCode),
                ),
                style: AtaText.small,
              ),
          ],
        ),
        actions: <Widget>[
          TextButton(
            onPressed: () => Navigator.of(dialog).pop(false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            key: const ValueKey<String>('confirm-release'),
            onPressed: () => Navigator.of(dialog).pop(true),
            child: Text(
              l10n.reservationReleaseConfirm,
              style: TextStyle(color: late ? AtaColors.danger : null),
            ),
          ),
        ],
      ),
    );
    return ok ?? false;
  }
}
