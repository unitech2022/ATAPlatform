import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:equatable/equatable.dart';

/// Payout history.
class PayoutsState extends Equatable {
  const PayoutsState({
    this.payouts = const <Payout>[],
    this.loading = false,
    this.failure,
    this.cancellingId,
    this.actionFailure,
  });

  final List<Payout> payouts;
  final bool loading;
  final Failure? failure;
  final String? cancellingId;
  final Failure? actionFailure;

  bool get isEmpty => !loading && failure == null && payouts.isEmpty;

  PayoutsState copyWith({
    List<Payout>? payouts,
    bool? loading,
    Failure? failure,
    String? cancellingId,
    Failure? actionFailure,
    bool clearFailure = false,
    bool clearCancelling = false,
  }) => PayoutsState(
    payouts: payouts ?? this.payouts,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    cancellingId: clearCancelling ? null : cancellingId ?? this.cancellingId,
    actionFailure: clearFailure ? null : actionFailure ?? this.actionFailure,
  );

  @override
  List<Object?> get props => <Object?>[
    payouts,
    loading,
    failure,
    cancellingId,
    actionFailure,
  ];
}
