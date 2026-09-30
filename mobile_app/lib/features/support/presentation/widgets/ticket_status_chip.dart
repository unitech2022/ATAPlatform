import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:flutter/material.dart';

/// Status chip of a ticket: open (brand), waiting for the user (amber),
/// in progress (ink), resolved (brand) and closed (muted).
class TicketStatusChip extends StatelessWidget {
  const TicketStatusChip({super.key, required this.status});

  final TicketStatus status;

  @override
  Widget build(BuildContext context) {
    final (Color background, Color foreground) = switch (status) {
      TicketStatus.open => (AtaColors.brandSoft, AtaColors.brand),
      TicketStatus.pendingUser => (AtaColors.warningSoft, AtaColors.warning),
      TicketStatus.inProgress => (AtaColors.cloud, AtaColors.ink),
      TicketStatus.resolved => (AtaColors.brandSoft, AtaColors.brand),
      TicketStatus.closed => (AtaColors.cloud, AtaColors.muted),
    };
    return AtaBadge(
      label: SupportText.ticketStatus(context.l10n, status),
      background: background,
      foreground: foreground,
    );
  }
}
