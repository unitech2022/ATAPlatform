import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:equatable/equatable.dart';

/// Steps of the cancel sheet.
enum CancelFlowStatus { loading, ready, previewing, confirming, done, failure }

/// State of `CancelFlowCubit`.
class CancelFlowState extends Equatable {
  const CancelFlowState({
    this.status = CancelFlowStatus.loading,
    this.reasons = const <CancellationReason>[],
    this.selected,
    this.preview,
    this.note = '',
    this.noteMissing = false,
    this.feeChanged = false,
    this.cancelledTrip,
    this.failure,
  });

  final CancelFlowStatus status;
  final List<CancellationReason> reasons;
  final CancellationReason? selected;

  /// Fee / points for [selected] (fresh after `cancellation_fee_changed`).
  final CancelPreview? preview;
  final String note;

  /// The reason requires a note and none was typed.
  final bool noteMissing;

  /// The fee went up between the preview and the confirmation.
  final bool feeChanged;

  /// Result of a successful cancel.
  final Trip? cancelledTrip;
  final Failure? failure;

  bool get isBusy =>
      status == CancelFlowStatus.loading ||
      status == CancelFlowStatus.previewing ||
      status == CancelFlowStatus.confirming;

  bool get needsNote => selected?.requiresNote ?? false;
  bool get hasNote => note.trim().isNotEmpty;

  /// A reason is chosen, its preview is loaded and the note (if required)
  /// is filled.
  bool get canConfirm =>
      selected != null &&
      preview != null &&
      !isBusy &&
      status != CancelFlowStatus.done &&
      (!needsNote || hasNote);

  CancelFlowState copyWith({
    CancelFlowStatus? status,
    List<CancellationReason>? reasons,
    CancellationReason? selected,
    CancelPreview? preview,
    String? note,
    bool? noteMissing,
    bool? feeChanged,
    Trip? cancelledTrip,
    Failure? failure,
    bool clearPreview = false,
    bool clearFailure = false,
  }) => CancelFlowState(
    status: status ?? this.status,
    reasons: reasons ?? this.reasons,
    selected: selected ?? this.selected,
    preview: clearPreview ? null : preview ?? this.preview,
    note: note ?? this.note,
    noteMissing: noteMissing ?? this.noteMissing,
    feeChanged: feeChanged ?? this.feeChanged,
    cancelledTrip: cancelledTrip ?? this.cancelledTrip,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    reasons,
    selected,
    preview,
    note,
    noteMissing,
    feeChanged,
    cancelledTrip,
    failure,
  ];
}
