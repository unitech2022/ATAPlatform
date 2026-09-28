import 'package:equatable/equatable.dart';

/// Lifecycle of a payout request.
enum PayoutStatus {
  requested('requested'),
  approved('approved'),
  paid('paid'),
  rejected('rejected'),
  cancelled('cancelled');

  const PayoutStatus(this.apiValue);

  final String apiValue;

  static PayoutStatus parse(String? value) {
    for (final PayoutStatus status in values) {
      if (status.apiValue == value) return status;
    }
    return requested;
  }
}

/// A driver payout (`Payout` of `docs/08` §F11.5).
class Payout extends Equatable {
  const Payout({
    required this.id,
    required this.payoutNumber,
    required this.amount,
    required this.status,
    this.ibanMasked,
    this.requestedAt,
    this.approvedAt,
    this.paidAt,
    this.rejectedReason,
    this.bankReference,
  });

  final String id;
  final String payoutNumber;
  final double amount;
  final PayoutStatus status;
  final String? ibanMasked;
  final DateTime? requestedAt;
  final DateTime? approvedAt;
  final DateTime? paidAt;
  final String? rejectedReason;
  final String? bankReference;

  /// Only a `requested` payout can be cancelled by the driver.
  bool get canCancel => status == PayoutStatus.requested;

  @override
  List<Object?> get props => <Object?>[
    id,
    payoutNumber,
    amount,
    status,
    ibanMasked,
    requestedAt,
    approvedAt,
    paidAt,
    rejectedReason,
    bankReference,
  ];
}
