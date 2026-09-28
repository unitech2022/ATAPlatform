import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:equatable/equatable.dart';

/// Balance, cash debt and payout eligibility.
class PayoutSummaryState extends Equatable {
  const PayoutSummaryState({this.summary, this.loading = false, this.failure});

  final PayoutSummary? summary;
  final bool loading;
  final Failure? failure;

  PayoutSummaryState copyWith({
    PayoutSummary? summary,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => PayoutSummaryState(
    summary: summary ?? this.summary,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[summary, loading, failure];
}
