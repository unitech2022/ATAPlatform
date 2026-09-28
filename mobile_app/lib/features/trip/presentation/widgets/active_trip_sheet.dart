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
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancel_reason_sheet.dart';
import 'package:ata_app/features/trip/presentation/widgets/driver_card.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_ended_view.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_progress_view.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_receipt_view.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_searching_view.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The white sheet of the passenger trip page, rendered per status.
class ActiveTripSheet extends StatelessWidget {
  const ActiveTripSheet({super.key});

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
            BlocBuilder<ActiveTripCubit, ActiveTripState>(
              builder: (BuildContext context, ActiveTripState state) {
                final Trip? trip = state.trip;
                if (trip == null) return const CenteredLoader();
                return _StageBody(state: state, trip: trip);
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _StageBody extends StatelessWidget {
  const _StageBody({required this.state, required this.trip});

  final ActiveTripState state;
  final Trip trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final ActiveTripCubit cubit = context.read<ActiveTripCubit>();
    final TripStage stage = trip.status;
    final Widget body;
    if (stage.isSearching) {
      body = TripSearchingView(
        trip: trip,
        cancelling: state.cancelling,
        onCancel: state.canCancel ? () => _cancel(context) : null,
      );
    } else if (stage.isRiding) {
      body = TripProgressView(trip: trip);
    } else if (stage == TripStage.completed) {
      body = TripReceiptView(trip: trip, onDone: cubit.dismiss);
    } else if (stage.isTerminal) {
      body = TripEndedView(
        stage: stage,
        retryLabel: l10n.retryRequest,
        onRetry: cubit.dismiss,
      );
    } else {
      body = Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.tripEyebrow, style: AtaText.eyebrow),
          const SizedBox(height: AtaSpacing.xxs),
          Text(TripText.passengerTitle(l10n, stage), style: AtaText.headline),
          if (stage.isWaiting) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(l10n.waitingCopy, style: AtaText.small),
            const SizedBox(height: AtaSpacing.sm),
            _WaitingTimer(seconds: state.waitingSeconds),
          ],
          const SizedBox(height: AtaSpacing.md),
          DriverCard(trip: trip, etaMinutes: state.etaMinutes),
          if (stage.canCancel) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            AtaButton(
              label: l10n.cancelTrip,
              variant: AtaButtonVariant.dangerOutline,
              height: AtaSizes.buttonCompact,
              loading: state.cancelling,
              onPressed: state.canCancel ? () => _cancel(context) : null,
            ),
          ],
        ],
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        body,
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
      ],
    );
  }

  Future<void> _cancel(BuildContext context) async {
    final ActiveTripCubit cubit = context.read<ActiveTripCubit>();
    final CancelReason? reason = await CancelReasonSheet.show(context);
    if (reason != null) await cubit.cancel(reason);
  }
}

class _WaitingTimer extends StatelessWidget {
  const _WaitingTimer({required this.seconds});

  final int seconds;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.md,
        vertical: AtaSpacing.sm,
      ),
      decoration: const BoxDecoration(
        color: AtaColors.cloud,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: <Widget>[
          Text(context.l10n.waitingTimerLabel, style: AtaText.label),
          Text(
            TripText.clock(seconds),
            textDirection: TextDirection.ltr,
            style: AtaText.section.copyWith(color: AtaColors.brand),
          ),
        ],
      ),
    );
  }
}
