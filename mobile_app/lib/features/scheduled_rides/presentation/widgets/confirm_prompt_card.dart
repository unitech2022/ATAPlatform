import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// The T-60 / T-15 confirmation prompt of a reservation: what is being
/// asked, the time left and a prominent "confirm" button. Renders nothing
/// when no prompt is due at [now].
class ConfirmPromptCard extends StatelessWidget {
  const ConfirmPromptCard({
    super.key,
    required this.reservation,
    required this.now,
    required this.busy,
    required this.onConfirm,
    this.onOpen,
  });

  final Reservation reservation;
  final DateTime now;
  final bool busy;
  final VoidCallback onConfirm;

  /// Opens the reservation (overview banner); `null` on the page itself.
  final VoidCallback? onOpen;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final ConfirmationKind? kind = reservation.pendingAt(now);
    final DateTime? deadline = reservation.deadlineAt(now);
    if (kind == null || deadline == null) return const SizedBox.shrink();
    return InkWell(
      onTap: onOpen,
      borderRadius: AtaRadii.itemRadius,
      child: Container(
        key: ValueKey<String>('confirm-prompt-${reservation.tripId}'),
        padding: const EdgeInsets.all(AtaSpacing.md),
        decoration: BoxDecoration(
          color: AtaColors.warningSoft,
          borderRadius: AtaRadii.itemRadius,
          border: Border.all(color: AtaColors.warning),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Text(
              kind == ConfirmationKind.first
                  ? l10n.reservationConfirmFirstTitle
                  : l10n.reservationConfirmFinalTitle,
              style: AtaText.bodyStrong,
            ),
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              l10n.reservationConfirmCopy(
                DateText.fullDayAndTime(
                  reservation.scheduledAt,
                  context.localeCode,
                ),
              ),
              style: AtaText.small,
            ),
            const SizedBox(height: AtaSpacing.xs),
            Text(
              l10n.reservationConfirmLeft(
                ScheduledText.clock(deadline.difference(now)),
              ),
              key: const ValueKey<String>('confirm-left'),
              style: AtaText.label.copyWith(color: AtaColors.warning),
            ),
            const SizedBox(height: AtaSpacing.sm),
            AtaButton(
              key: const ValueKey<String>('confirm-reservation'),
              label: l10n.reservationConfirmAction,
              variant: AtaButtonVariant.brand,
              height: AtaSizes.buttonCompact,
              loading: busy,
              onPressed: busy ? null : onConfirm,
            ),
          ],
        ),
      ),
    );
  }
}
