import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_documents_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_documents_state.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/vehicle_card.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Documents with expiry / status and the vehicle card.
class DocumentsTab extends StatelessWidget {
  const DocumentsTab({super.key});

  /// Documents expiring within this window are flagged.
  static const Duration expiringSoonWindow = Duration(days: 30);

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<DriverDocumentsCubit, DriverDocumentsState>(
      builder: (BuildContext context, DriverDocumentsState state) {
        if (state.loading) return const CenteredLoader();
        if (state.failure != null) {
          return FailureView(
            failure: state.failure!,
            onRetry: context.read<DriverDocumentsCubit>().load,
          );
        }
        final List<DriverDocument> documents =
            state.application?.documents ?? const <DriverDocument>[];
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            AtaCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: <Widget>[
                      Text(l10n.documents, style: AtaText.section),
                      AtaBadge(label: l10n.accountVerified),
                    ],
                  ),
                  const SizedBox(height: AtaSpacing.sm),
                  if (documents.isEmpty)
                    Padding(
                      padding: const EdgeInsets.symmetric(
                        vertical: AtaSpacing.md,
                      ),
                      child: Text(l10n.documentsEmpty, style: AtaText.small),
                    )
                  else
                    for (int i = 0; i < documents.length; i++)
                      _DocumentRow(
                        document: documents[i],
                        last: i == documents.length - 1,
                      ),
                ],
              ),
            ),
            const SizedBox(height: AtaSpacing.xl),
            VehicleCard(vehicle: state.application?.vehicle),
          ],
        );
      },
    );
  }
}

class _DocumentRow extends StatelessWidget {
  const _DocumentRow({required this.document, required this.last});

  final DriverDocument document;
  final bool last;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? expiry = document.expiresAt;
    final (String label, Color color) = _status(l10n, expiry);
    return SettingRow(
      leading: const IconBox.cloud(icon: AtaIcons.document),
      title: document.documentTypeName,
      subtitle: expiry == null
          ? l10n.noExpiry
          : l10n.expiresOn(DateText.longDate(expiry, context.localeCode)),
      showChevron: false,
      last: last,
      trailing: Text(
        label,
        style: AtaText.captionStrong.copyWith(color: color),
      ),
    );
  }

  (String, Color) _status(AppLocalizations l10n, DateTime? expiry) {
    final DateTime now = DateTime.now();
    if (expiry != null && expiry.isBefore(now)) {
      return (l10n.docExpired, AtaColors.danger);
    }
    if (expiry != null &&
        expiry.isBefore(now.add(DocumentsTab.expiringSoonWindow))) {
      return (l10n.docExpiringSoon, AtaColors.danger);
    }
    return switch (document.status) {
      DocumentStatus.verified => (l10n.docVerified, AtaColors.brand),
      DocumentStatus.pending => (l10n.docStatusPending, AtaColors.muted),
      DocumentStatus.rejected => (l10n.docStatusRejected, AtaColors.danger),
    };
  }
}
