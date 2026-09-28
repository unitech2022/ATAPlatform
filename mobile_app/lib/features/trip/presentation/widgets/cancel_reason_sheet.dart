import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Bottom sheet listing the cancellation reasons. Tapping a reason returns
/// it; "keep the trip" returns `null`.
class CancelReasonSheet extends StatelessWidget {
  const CancelReasonSheet({super.key});

  static Future<CancelReason?> show(BuildContext context) =>
      showModalBottomSheet<CancelReason>(
        context: context,
        isScrollControlled: true,
        builder: (_) => const CancelReasonSheet(),
      );

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Padding(
      padding: EdgeInsets.fromLTRB(
        AtaSpacing.lg,
        AtaSpacing.lg,
        AtaSpacing.lg,
        AtaSpacing.lg + MediaQuery.paddingOf(context).bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const SheetHandle(),
          Text(l10n.cancelReasonTitle, style: AtaText.section),
          const SizedBox(height: AtaSpacing.xxs),
          Text(l10n.cancelReasonCopy, style: AtaText.small),
          const SizedBox(height: AtaSpacing.md),
          for (final CancelReason reason in CancelReason.values) ...<Widget>[
            SelectableTile(
              selected: false,
              onTap: () => Navigator.of(context).pop(reason),
              child: Text(
                TripText.reasonLabel(l10n, reason),
                style: AtaText.bodyStrong,
              ),
            ),
            const SizedBox(height: AtaSpacing.xs),
          ],
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: l10n.keepTrip,
            variant: AtaButtonVariant.soft,
            height: AtaSizes.buttonCompact,
            onPressed: () => Navigator.of(context).pop(),
          ),
        ],
      ),
    );
  }
}
