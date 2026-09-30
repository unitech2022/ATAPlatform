import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:ata_app/features/safety/presentation/widgets/status_badge.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_link_button.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// One lost item report with its status (and optional actions).
class LostItemTile extends StatelessWidget {
  const LostItemTile({
    super.key,
    required this.report,
    this.actions,
    this.highlighted = false,
  });

  final LostItemReport report;
  final Widget? actions;
  final bool highlighted;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? at = report.createdAt;
    return Padding(
      padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
      child: AtaCard(
        padding: const EdgeInsets.all(AtaSpacing.md),
        borderColor: highlighted ? Theme.of(context).primaryColor : null,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Row(
              children: <Widget>[
                Expanded(
                  child: Text(
                    SafetyText.lostCategory(l10n, report.itemCategory),
                    style: AtaText.bodyStrong,
                  ),
                ),
                StatusBadge(
                  status: report.status,
                  label: SafetyText.lostStatus(l10n, report.status),
                ),
              ],
            ),
            if (report.description.isNotEmpty)
              Text(report.description, style: AtaText.small),
            Text(
              <String>[
                report.reportNumber,
                if (report.tripNumber.isNotEmpty) report.tripNumber,
                if (at != null) DateText.dayAndTime(at, context.localeCode),
              ].join(' · '),
              style: AtaText.caption,
            ),
            if (report.supportTicketId != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              TicketLinkButton(ticketId: report.supportTicketId!),
            ],
            if (actions != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              actions!,
            ],
          ],
        ),
      ),
    );
  }
}
