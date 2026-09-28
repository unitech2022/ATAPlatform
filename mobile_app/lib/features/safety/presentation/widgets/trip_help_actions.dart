import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Help after a completed trip: lost item and safety report (F12.6).
class TripHelpActions extends StatelessWidget {
  const TripHelpActions({super.key, required this.tripId});

  final String tripId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.tripHelpTitle, style: AtaText.section),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.search),
            title: l10n.lostItemTitle,
            subtitle: l10n.lostItemRowCopy,
            onTap: () => context.push(AppRoutes.rideLostItem(tripId)),
          ),
          SettingRow(
            leading: const IconBox.cloud(icon: AtaIcons.shield),
            title: l10n.safetyReportTitle,
            subtitle: l10n.safetyReportRowCopy,
            last: true,
            onTap: () => context.push(AppRoutes.safetyReportFor(tripId)),
          ),
        ],
      ),
    );
  }
}
