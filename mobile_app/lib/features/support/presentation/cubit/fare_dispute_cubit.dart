import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// State of `FareDisputeCubit`.
class FareDisputeState extends Equatable {
  const FareDisputeState({
    this.enabled = false,
    this.reason = DisputeReason.overcharged,
    this.refundText = '',
  });

  final bool enabled;
  final DisputeReason reason;

  /// What was typed in the optional "requested refund" field.
  final String refundText;

  /// The refund the rider asks for; `null` when left empty or invalid.
  double? get refund {
    final double? value = double.tryParse(
      refundText.trim().replaceAll(',', '.'),
    );
    return value != null && value > 0 ? value : null;
  }

  /// A typed amount that is not a positive number.
  bool get refundInvalid => refundText.trim().isNotEmpty && refund == null;

  /// What `POST /support/tickets` receives; `null` when disabled.
  DisputeDraft? get draft => enabled
      ? DisputeDraft(reason: reason, requestedRefundAmount: refund)
      : null;

  bool get valid => !enabled || !refundInvalid;

  FareDisputeState copyWith({
    bool? enabled,
    DisputeReason? reason,
    String? refundText,
  }) => FareDisputeState(
    enabled: enabled ?? this.enabled,
    reason: reason ?? this.reason,
    refundText: refundText ?? this.refundText,
  );

  @override
  List<Object?> get props => <Object?>[enabled, reason, refundText];
}

/// The fare dispute fields of a payment-issue ticket: the reason and the
/// optional requested refund amount (`docs/11` §F18.2). The window and
/// duplicate rules are the API's (`dispute_window_closed`, `dispute_exists`).
class FareDisputeCubit extends Cubit<FareDisputeState> {
  FareDisputeCubit({bool enabled = false})
    : super(FareDisputeState(enabled: enabled));

  void toggle({required bool enabled}) =>
      emit(state.copyWith(enabled: enabled));

  void selectReason(DisputeReason reason) =>
      emit(state.copyWith(reason: reason));

  void refundChanged(String text) => emit(state.copyWith(refundText: text));
}
