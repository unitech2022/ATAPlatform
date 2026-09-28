import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Cost of the chosen reason: the API message, fee (passenger) or points
/// (driver), free window, scheduled-booking label and review notices.
class CancelPreviewBox extends StatelessWidget {
  const CancelPreviewBox({super.key, required this.state});

  final CancelFlowState state;

  static const double _loaderSize = 20;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CancellationReason reason = state.selected!;
    final CancelPreview? preview = state.preview;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        if (reason.isEmergency)
          _Notice(text: l10n.cancelEmergencyNote, danger: true)
        else if (reason.needsReview)
          _Notice(text: l10n.cancelExcusableNote),
        if (state.feeChanged) ...<Widget>[
          const SizedBox(height: AtaSpacing.xs),
          _Notice(text: l10n.cancellationFeeChangedError, warning: true),
        ],
        const SizedBox(height: AtaSpacing.xs),
        Container(
          padding: const EdgeInsets.all(AtaSpacing.md),
          decoration: const BoxDecoration(
            color: AtaColors.cloud,
            borderRadius: AtaRadii.itemRadius,
          ),
          child: preview == null
              ? const Center(
                  child: SizedBox.square(
                    dimension: _loaderSize,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                )
              : _PreviewBody(
                  preview: preview,
                  driver:
                      context.read<CancelFlowCubit>().actor == TripActor.driver,
                ),
        ),
      ],
    );
  }
}

class _PreviewBody extends StatelessWidget {
  const _PreviewBody({required this.preview, required this.driver});

  final CancelPreview preview;
  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool free = driver
        ? preview.penaltyPoints == 0 && !preview.hasFee
        : !preview.hasFee;
    final String amount = free
        ? l10n.cancelFree
        : driver && !preview.hasFee
        ? l10n.penaltyPointsValue(preview.penaltyPoints)
        : TripText.price(l10n, preview.fee);
    final DateTime? freeUntil = preview.freeUntil;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: <Widget>[
            Text(
              preview.isScheduled
                  ? l10n.scheduledCancelFeeLabel
                  : driver
                  ? l10n.cancelPointsLabel
                  : l10n.cancelFeeLabel,
              style: AtaText.label,
            ),
            Text(
              amount,
              style: AtaText.section.copyWith(
                color: free ? AtaColors.brand : AtaColors.danger,
              ),
            ),
          ],
        ),
        if (preview.message.isNotEmpty) ...<Widget>[
          const SizedBox(height: AtaSpacing.xxs),
          Text(preview.message, style: AtaText.small),
        ],
        if (freeUntil != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            l10n.cancelFreeUntil(
              DateText.dayAndTime(freeUntil, context.localeCode),
            ),
            style: AtaText.caption,
          ),
        ],
        if (preview.requiresReview) ...<Widget>[
          const SizedBox(height: AtaSpacing.xxs),
          Text(l10n.cancelRequiresReview, style: AtaText.caption),
        ],
      ],
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice({
    required this.text,
    this.danger = false,
    this.warning = false,
  });

  final String text;
  final bool danger;
  final bool warning;

  @override
  Widget build(BuildContext context) {
    final Color background = danger
        ? AtaColors.dangerSoft
        : warning
        ? AtaColors.warningSoft
        : AtaColors.brandSoft;
    final Color foreground = danger
        ? AtaColors.danger
        : warning
        ? AtaColors.warning
        : AtaColors.ink;
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.sm),
      decoration: BoxDecoration(
        color: background,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Text(text, style: AtaText.small.copyWith(color: foreground)),
    );
  }
}
