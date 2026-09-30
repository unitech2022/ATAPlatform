import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_status_chip.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One row of "تذاكري": subject, number / trip, type, status chip and the
/// unread badge.
class TicketTile extends StatelessWidget {
  const TicketTile({super.key, required this.ticket, required this.onTap});

  final TicketSummary ticket;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? at = ticket.displayDate;
    final String? trip = ticket.tripNumber;
    return Padding(
      padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
      child: AtaCard(
        onTap: onTap,
        padding: const EdgeInsets.all(AtaSpacing.md),
        borderColor: ticket.hasUnread ? AtaColors.brand : null,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    ticket.subject,
                    style: AtaText.bodyStrong,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                if (ticket.hasUnread) ...<Widget>[
                  const SizedBox(width: AtaSpacing.xs),
                  AtaBadge(
                    label: l10n.supportUnreadBadge(ticket.unread),
                    background: AtaColors.brand,
                    foreground: AtaColors.white,
                  ),
                ],
              ],
            ),
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              trip == null || trip.isEmpty
                  ? ticket.ticketNumber
                  : l10n.ticketNumberAndTrip(ticket.ticketNumber, trip),
              style: AtaText.caption,
            ),
            const SizedBox(height: AtaSpacing.xs),
            Wrap(
              spacing: AtaSpacing.xs,
              runSpacing: AtaSpacing.xxs,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: <Widget>[
                TicketStatusChip(status: ticket.status),
                Text(
                  SupportText.ticketType(l10n, ticket.type),
                  style: AtaText.caption,
                ),
                if (at != null)
                  Text(
                    DateText.dayAndTime(at, context.localeCode),
                    style: AtaText.caption,
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
