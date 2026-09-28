import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Lists the required documents with their review status. Without an
/// application (offline) it shows the prototype's default list.
class RequiredDocumentsCard extends StatelessWidget {
  const RequiredDocumentsCard({super.key, this.application});

  final DriverApplication? application;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverApplication? app = application;
    final List<Widget> rows = app == null || app.requiredDocuments.isEmpty
        ? _defaultRows(l10n)
        : <Widget>[
            for (final RequiredDocument required in app.requiredDocuments)
              _DocumentRow(
                name: required.name,
                status: _status(
                  l10n,
                  required,
                  app.documentFor(required.documentTypeId),
                ),
                note: app.documentFor(required.documentTypeId)?.reviewNote,
              ),
          ];
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      decoration: BoxDecoration(
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(color: AtaColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            children: <Widget>[
              const AtaIcon(
                AtaIcons.document,
                size: AtaSizes.iconMedium,
                color: AtaColors.brand,
              ),
              const SizedBox(width: AtaSpacing.sm),
              Text(l10n.requiredDocuments, style: AtaText.bodyStrong),
            ],
          ),
          const SizedBox(height: AtaSpacing.md),
          ...rows,
        ],
      ),
    );
  }

  List<Widget> _defaultRows(AppLocalizations l10n) => <Widget>[
    for (final String name in <String>[
      l10n.docNationalId,
      l10n.docDrivingLicense,
      l10n.docVehicleRegistration,
      l10n.docPersonalPhoto,
    ])
      _DocumentRow(name: name, status: null, note: null),
  ];

  _DocStatusLabel _status(
    AppLocalizations l10n,
    RequiredDocument required,
    DriverDocument? document,
  ) {
    if (document == null) {
      return _DocStatusLabel(
        l10n.docStatusRequired,
        AtaColors.muted,
        AtaColors.cloud,
      );
    }
    return switch (document.status) {
      DocumentStatus.pending => _DocStatusLabel(
        l10n.docStatusPending,
        AtaColors.ink,
        AtaColors.cloud,
      ),
      DocumentStatus.verified => _DocStatusLabel(
        l10n.docStatusVerified,
        AtaColors.brand,
        AtaColors.brandSoft,
      ),
      DocumentStatus.rejected => _DocStatusLabel(
        l10n.docStatusRejected,
        AtaColors.danger,
        AtaColors.dangerSoft,
      ),
    };
  }
}

class _DocStatusLabel {
  const _DocStatusLabel(this.text, this.foreground, this.background);

  final String text;
  final Color foreground;
  final Color background;
}

class _DocumentRow extends StatelessWidget {
  const _DocumentRow({
    required this.name,
    required this.status,
    required this.note,
  });

  final String name;
  final _DocStatusLabel? status;
  final String? note;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
      child: Row(
        children: <Widget>[
          const AtaIcon(
            AtaIcons.check,
            size: AtaSizes.iconSmall,
            color: AtaColors.brand,
          ),
          const SizedBox(width: AtaSpacing.xs),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(name, style: AtaText.small),
                if (note != null && note!.isNotEmpty)
                  Text(
                    note!,
                    style: AtaText.caption.copyWith(color: AtaColors.danger),
                  ),
              ],
            ),
          ),
          if (status != null)
            AtaBadge(
              label: status!.text,
              foreground: status!.foreground,
              background: status!.background,
            ),
        ],
      ),
    );
  }
}
