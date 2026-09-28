import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancel_preview_box.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancel_reason_list.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Cancel sheet built on [CancelFlowCubit]: reasons from the API, fee /
/// points preview, required note, excused / emergency messaging. Pops the
/// cancelled [Trip], or `null` when the user keeps the trip.
class CancelReasonSheet extends StatelessWidget {
  const CancelReasonSheet({super.key});

  static Future<Trip?> show(
    BuildContext context, {
    required Trip trip,
    required TripActor actor,
  }) => showModalBottomSheet<Trip>(
    context: context,
    isScrollControlled: true,
    builder: (_) => BlocProvider<CancelFlowCubit>(
      create: (_) => CancelFlowCubit(
        getReasons: getIt(),
        preview: getIt(),
        cancelTrip: getIt(),
        tripId: trip.id,
        actor: actor,
        stage: CancellationStage.of(trip, now: DateTime.now()).apiValue,
      )..load(),
      child: const CancelReasonSheet(),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final EdgeInsets insets = MediaQuery.viewInsetsOf(context);
    return BlocConsumer<CancelFlowCubit, CancelFlowState>(
      listenWhen: (CancelFlowState p, CancelFlowState c) =>
          p.status != c.status && c.status == CancelFlowStatus.done,
      listener: (BuildContext context, CancelFlowState state) =>
          Navigator.of(context).pop(state.cancelledTrip),
      builder: (BuildContext context, CancelFlowState state) {
        final CancelFlowCubit cubit = context.read<CancelFlowCubit>();
        return SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg +
                MediaQuery.paddingOf(context).bottom +
                insets.bottom,
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
              if (state.status == CancelFlowStatus.loading)
                const CenteredLoader()
              else if (state.reasons.isEmpty && state.failure != null)
                FailureView(failure: state.failure!, onRetry: cubit.load)
              else ...<Widget>[
                CancelReasonList(state: state),
                if (state.selected != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.sm),
                  CancelPreviewBox(state: state),
                ],
                if (state.needsNote) ...<Widget>[
                  const SizedBox(height: AtaSpacing.sm),
                  TextField(
                    onChanged: cubit.noteChanged,
                    maxLength: _noteMaxLength,
                    maxLines: 2,
                    decoration: InputDecoration(
                      hintText: l10n.cancelNoteHint,
                      errorText: state.noteMissing
                          ? l10n.cancelNoteRequired
                          : null,
                    ),
                  ),
                ],
                if (state.failure != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.sm),
                  InlineError(message: failureText(state.failure!, l10n)),
                ],
              ],
              const SizedBox(height: AtaSpacing.md),
              AtaButton(
                label: l10n.confirmCancel,
                variant: AtaButtonVariant.danger,
                height: AtaSizes.buttonCompact,
                loading: state.status == CancelFlowStatus.confirming,
                onPressed: state.canConfirm ? cubit.confirm : null,
              ),
              const SizedBox(height: AtaSpacing.xs),
              AtaButton(
                label: l10n.keepTrip,
                variant: AtaButtonVariant.soft,
                height: AtaSizes.buttonCompact,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ],
          ),
        );
      },
    );
  }

  static const int _noteMaxLength = 500;
}
