import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:equatable/equatable.dart';

/// State of the receipt page.
class ReceiptState extends Equatable {
  const ReceiptState({this.receipt, this.loading = false, this.failure});

  final Receipt? receipt;
  final bool loading;
  final Failure? failure;

  /// `409 conflict`: the trip has no receipt (not completed, no fee).
  bool get unavailable => failure?.code == 'conflict';

  ReceiptState copyWith({
    Receipt? receipt,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => ReceiptState(
    receipt: receipt ?? this.receipt,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[receipt, loading, failure];
}
