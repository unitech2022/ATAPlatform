import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancel_reason_sheet.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancellation_text.dart';
import 'package:ata_app/features/trip/presentation/widgets/driver_pin_entry.dart';
import 'package:ata_app/features/trip/presentation/widgets/driver_trip_info.dart';
import 'package:ata_app/features/trip/presentation/widgets/no_show_section.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_ended_view.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Bottom sheet of the driver trip page: stage title, trip info and the
/// single primary action (or PIN entry) for the current status.
class DriverTripSheet extends StatelessWidget {
  const DriverTripSheet({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AtaColors.white,
        borderRadius: AtaRadii.sheetTopRadius,
        boxShadow: AtaShadows.panel,
      ),
      child: SingleChildScrollView(
        padding: EdgeInsets.fromLTRB(
          AtaSpacing.gutter,
          AtaSpacing.md,
          AtaSpacing.gutter,
          AtaSpacing.gutter + MediaQuery.paddingOf(context).bottom,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            const SheetHandle(),
            BlocBuilder<DriverTripCubit, DriverTripState>(
              builder: (BuildContext context, DriverTripState state) {
                final Trip? trip = state.trip;
                if (trip == null) return const CenteredLoader();
                return _Body(state: state, trip: trip);
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.state, required this.trip});

  final DriverTripState state;
  final Trip trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverTripCubit cubit = context.read<DriverTripCubit>();
    final TripStage stage = trip.status;
    if (stage.isTerminal) {
      return _Ended(trip: trip, onDone: cubit.dismiss);
    }
    final TripStep? step = state.nextStep;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.driverTripEyebrow, style: AtaText.eyebrow),
        const SizedBox(height: AtaSpacing.xxs),
        Text(TripText.driverTitle(l10n, stage), style: AtaText.headline),
        const SizedBox(height: AtaSpacing.md),
        DriverTripInfo(trip: trip),
        const SizedBox(height: AtaSpacing.lg),
        if (state.needsPin)
          const DriverPinEntry()
        else if (step != null)
          AtaButton(
            label: TripText.stepLabel(l10n, step),
            variant: step == TripStep.complete
                ? AtaButtonVariant.brand
                : AtaButtonVariant.primary,
            loading: state.busy,
            onPressed: state.busy
                ? null
                : () => cubit.advance(
                    at: context
                        .read<LocationStreamCubit>()
                        .state
                        .position
                        ?.point,
                  ),
          ),
        if (stage.isWaiting) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          NoShowSection(trip: trip),
        ],
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
        if (stage.canCancel) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: l10n.cancelTrip,
            variant: AtaButtonVariant.dangerOutline,
            height: AtaSizes.buttonCompact,
            onPressed: state.canCancel ? () => _cancel(context) : null,
          ),
        ],
      ],
    );
  }

  Future<void> _cancel(BuildContext context) async {
    final DriverTripCubit cubit = context.read<DriverTripCubit>();
    final Trip? cancelled = await CancelReasonSheet.show(
      context,
      trip: trip,
      actor: TripActor.driver,
    );
    if (cancelled != null) cubit.adopt(cancelled);
  }
}

class _Ended extends StatelessWidget {
  const _Ended({required this.trip, required this.onDone});

  final Trip trip;
  final VoidCallback onDone;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    if (trip.status != TripStage.completed) {
      return TripEndedView(
        stage: trip.status,
        copy:
            CancellationText.driverCopy(l10n, trip) ??
            l10n.driverTripCancelledCopy,
        retryLabel: l10n.backToDashboard,
        onRetry: onDone,
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.stageCompleted, style: AtaText.headline),
        const SizedBox(height: AtaSpacing.xxs),
        Text(l10n.driverTripCompletedCopy, style: AtaText.bodyMuted),
        const SizedBox(height: AtaSpacing.md),
        Container(
          padding: const EdgeInsets.all(AtaSpacing.md),
          decoration: const BoxDecoration(
            color: AtaColors.brandSoft,
            borderRadius: AtaRadii.itemRadius,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Text(l10n.earningsLine, style: AtaText.label),
              Text(
                TripText.price(l10n, trip.fare),
                textDirection: TextDirection.ltr,
                style: AtaText.section.copyWith(color: AtaColors.brand),
              ),
            ],
          ),
        ),
        if (trip.collectCashAmount != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          Container(
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: const BoxDecoration(
              color: AtaColors.warningSoft,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Text(
              l10n.collectCashLine(
                TripText.price(l10n, trip.collectCashAmount!),
              ),
              style: AtaText.label.copyWith(color: AtaColors.warning),
            ),
          ),
        ],
        const SizedBox(height: AtaSpacing.lg),
        AtaButton(label: l10n.backToDashboard, onPressed: onDone),
      ],
    );
  }
}
