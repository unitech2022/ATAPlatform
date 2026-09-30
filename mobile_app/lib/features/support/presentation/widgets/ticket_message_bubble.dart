import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/presentation/widgets/attachment_view.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One message of a thread: mine (brand, end), the agent's (cloud, start,
/// with the agent name) and system notes (centered caption); attachments
/// follow the text.
class TicketMessageBubble extends StatelessWidget {
  const TicketMessageBubble({super.key, required this.message});

  final TicketMessage message;

  static const double _maxWidthFactor = 0.82;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    if (message.isSystem) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
        child: Text(
          message.body,
          style: AtaText.caption,
          textAlign: TextAlign.center,
        ),
      );
    }
    final bool mine = message.isMine;
    final DateTime? at = message.createdAt;
    final Color ink = mine ? AtaColors.white : AtaColors.ink;
    return Align(
      alignment: mine
          ? AlignmentDirectional.centerEnd
          : AlignmentDirectional.centerStart,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: MediaQuery.sizeOf(context).width * _maxWidthFactor,
        ),
        child: Container(
          margin: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
          padding: const EdgeInsets.symmetric(
            horizontal: AtaSpacing.md,
            vertical: AtaSpacing.sm,
          ),
          decoration: BoxDecoration(
            color: mine ? AtaColors.brand : AtaColors.cloud,
            borderRadius: AtaRadii.itemRadius,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Text(
                mine
                    ? l10n.threadYou
                    : (message.authorName ?? l10n.threadAgentName),
                style: AtaText.captionStrong.copyWith(color: ink),
              ),
              Text(message.body, style: AtaText.body.copyWith(color: ink)),
              for (final TicketAttachment a in message.attachments)
                Padding(
                  padding: const EdgeInsets.only(top: AtaSpacing.xs),
                  child: AttachmentView(attachment: a),
                ),
              if (at != null)
                Text(
                  DateText.dayAndTime(at, context.localeCode),
                  style: AtaText.caption.copyWith(
                    color: mine ? AtaColors.white80 : AtaColors.muted,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
