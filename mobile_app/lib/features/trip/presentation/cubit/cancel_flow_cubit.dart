import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_cancellation_reasons.dart';
import 'package:ata_app/features/trip/domain/usecases/preview_cancellation.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Cancel sheet (F14): loads the reasons for the actor / stage, previews
/// the fee (passenger) or penalty points (driver) of the chosen reason,
/// then cancels with the previewed amount. `409 cancellation_fee_changed`
/// re-runs the preview so the user confirms the new amount.
class CancelFlowCubit extends Cubit<CancelFlowState> {
  CancelFlowCubit({
    required this._getReasons,
    required this._preview,
    required this._cancelTrip,
    required this.tripId,
    required this.actor,
    this.stage,
  }) : super(const CancelFlowState());

  final GetCancellationReasons _getReasons;
  final PreviewCancellation _preview;
  final CancelTrip _cancelTrip;
  final String tripId;
  final TripActor actor;

  /// `CancellationStage.apiValue` used to filter the reasons.
  final String? stage;

  Future<void> load() async {
    emit(state.copyWith(status: CancelFlowStatus.loading, clearFailure: true));
    final result = await _getReasons(
      CancellationReasonsParams(actor: actor, stage: stage),
    );
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        state.copyWith(status: CancelFlowStatus.failure, failure: failure),
      ),
      (List<CancellationReason> reasons) => emit(
        state.copyWith(status: CancelFlowStatus.ready, reasons: reasons),
      ),
    );
  }

  /// Chooses [reason] and previews its cost.
  Future<void> select(CancellationReason reason) async {
    if (state.isBusy || state.status == CancelFlowStatus.done) return;
    emit(
      state.copyWith(
        selected: reason,
        clearPreview: true,
        clearFailure: true,
        noteMissing: false,
        feeChanged: false,
      ),
    );
    await _runPreview();
  }

  void noteChanged(String note) =>
      emit(state.copyWith(note: note, noteMissing: false));

  Future<void> confirm() async {
    final CancellationReason? reason = state.selected;
    final CancelPreview? preview = state.preview;
    if (reason == null || preview == null || state.isBusy) return;
    if (state.needsNote && !state.hasNote) {
      emit(state.copyWith(noteMissing: true));
      return;
    }
    emit(
      state.copyWith(
        status: CancelFlowStatus.confirming,
        clearFailure: true,
        feeChanged: false,
      ),
    );
    final bool passenger = actor == TripActor.passenger;
    final result = await _cancelTrip(
      CancelTripParams(
        tripId: tripId,
        actor: actor,
        reasonCode: reason.code,
        note: state.hasNote ? state.note.trim() : null,
        expectedFee: passenger ? preview.fee : null,
        expectedPenaltyPoints: passenger ? null : preview.penaltyPoints,
      ),
    );
    if (isClosed) return;
    await result.fold(_onCancelFailure, (Trip trip) async {
      emit(state.copyWith(status: CancelFlowStatus.done, cancelledTrip: trip));
    });
  }

  Future<void> _onCancelFailure(Failure failure) async {
    if (failure.code == ErrorCodes.cancellationFeeChanged) {
      emit(state.copyWith(feeChanged: true, clearPreview: true));
      await _runPreview(keepFeeChanged: true);
      return;
    }
    final bool noteRequired =
        failure.code == ErrorCodes.validationFailed &&
        failure.details?['note'] != null;
    emit(
      state.copyWith(
        status: CancelFlowStatus.ready,
        failure: noteRequired ? null : failure,
        noteMissing: noteRequired,
      ),
    );
  }

  Future<void> _runPreview({bool keepFeeChanged = false}) async {
    final CancellationReason? reason = state.selected;
    if (reason == null) return;
    emit(state.copyWith(status: CancelFlowStatus.previewing));
    final result = await _preview(
      PreviewCancellationParams(
        tripId: tripId,
        actor: actor,
        reasonCode: reason.code,
      ),
    );
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        state.copyWith(status: CancelFlowStatus.ready, failure: failure),
      ),
      (CancelPreview preview) => emit(
        state.copyWith(
          status: CancelFlowStatus.ready,
          preview: preview,
          feeChanged: keepFeeChanged,
        ),
      ),
    );
  }
}
