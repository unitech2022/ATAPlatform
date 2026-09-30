import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// "متابعة مع الدعم": opens the support ticket thread linked to a safety
/// case or a lost item report (`supportTicketId`).
class TicketLinkButton extends StatelessWidget {
  const TicketLinkButton({super.key, required this.ticketId});

  final String ticketId;

  @override
  Widget build(BuildContext context) {
    return AtaButton(
      key: const ValueKey<String>('support-ticket-link'),
      label: context.l10n.supportFollowTicket,
      icon: AtaIcons.document,
      variant: AtaButtonVariant.soft,
      height: AtaSizes.buttonCompact,
      onPressed: () => context.push(AppRoutes.supportTicket(ticketId)),
    );
  }
}
