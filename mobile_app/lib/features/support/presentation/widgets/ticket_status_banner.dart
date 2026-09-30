import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_status_chip.dart';
import 'package:flutter/material.dart';

/// Status line of a thread: the chip and a plain sentence (no response
/// time is promised). Waiting-for-you and closed tickets stand out.
class TicketStatusBanner extends StatelessWidget {
  const TicketStatusBanner({super.key, required this.status});

  final TicketStatus status;

  @override
  Widget build(BuildContext context) {
    final bool attention =
        status == TicketStatus.pendingUser || status == TicketStatus.closed;
    return Container(
      margin: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.gutter,
        vertical: AtaSpacing.xs,
      ),
      padding: const EdgeInsets.all(AtaSpacing.sm),
      decoration: BoxDecoration(
        color: attention ? AtaColors.warningSoft : AtaColors.cloud,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Row(
        children: <Widget>[
          TicketStatusChip(status: status),
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Text(
              SupportText.ticketBanner(context.l10n, status),
              style: AtaText.small,
            ),
          ),
        ],
      ),
    );
  }
}
