import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:equatable/equatable.dart';

/// Client-side problem with the typed amount.
enum PayoutAmountError { invalid, belowMinimum, aboveAvailable }

/// Payout request form.
class PayoutRequestState extends Equatable {
  const PayoutRequestState({
    this.summary,
    this.loading = false,
    this.amountText = '',
    this.submitting = false,
    this.payout,
    this.failure,
  });

  final PayoutSummary? summary;
  final bool loading;
  final String amountText;
  final bool submitting;

  /// The created payout (success).
  final Payout? payout;
  final Failure? failure;

  double? get amount => double.tryParse(amountText.trim());

  PayoutAmountError? get amountError {
    final PayoutSummary? s = summary;
    final double? value = amount;
    if (amountText.trim().isEmpty) return null;
    if (value == null || value <= 0) return PayoutAmountError.invalid;
    if (s == null) return null;
    if (value < s.minPayoutAmount) return PayoutAmountError.belowMinimum;
    if (value > s.availableForPayout) return PayoutAmountError.aboveAvailable;
    return null;
  }

  bool get canSubmit =>
      !submitting &&
      payout == null &&
      (summary?.canRequest ?? false) &&
      amount != null &&
      amountError == null;

  PayoutRequestState copyWith({
    PayoutSummary? summary,
    bool? loading,
    String? amountText,
    bool? submitting,
    Payout? payout,
    Failure? failure,
    bool clearFailure = false,
  }) => PayoutRequestState(
    summary: summary ?? this.summary,
    loading: loading ?? this.loading,
    amountText: amountText ?? this.amountText,
    submitting: submitting ?? this.submitting,
    payout: payout ?? this.payout,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    summary,
    loading,
    amountText,
    submitting,
    payout,
    failure,
  ];
}
