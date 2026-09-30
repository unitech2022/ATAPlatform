import 'package:equatable/equatable.dart';

/// `reason` of a fare dispute.
enum DisputeReason {
  overcharged('overcharged'),
  routeLonger('route_longer'),
  waitingCharged('waiting_charged'),
  cancellationFee('cancellation_fee'),
  promoNotApplied('promo_not_applied'),
  other('other');

  const DisputeReason(this.apiValue);

  final String apiValue;

  static DisputeReason parse(String? value) {
    for (final DisputeReason r in values) {
      if (r.apiValue == value) return r;
    }
    return other;
  }
}

/// `status` of a fare dispute.
enum DisputeStatus {
  open('open'),
  underReview('under_review'),
  approved('approved'),
  partiallyApproved('partially_approved'),
  rejected('rejected');

  const DisputeStatus(this.apiValue);

  final String apiValue;

  bool get isResolved =>
      this == approved || this == partiallyApproved || this == rejected;

  static DisputeStatus parse(String? value) {
    for (final DisputeStatus s in values) {
      if (s.apiValue == value) return s;
    }
    return open;
  }
}

/// `dispute` object of a ticket.
class FareDispute extends Equatable {
  const FareDispute({
    required this.reason,
    required this.status,
    this.chargedAmount = 0,
    this.requestedRefundAmount,
    this.resolution,
    this.approvedRefundAmount,
  });

  final DisputeReason reason;
  final DisputeStatus status;
  final double chargedAmount;
  final double? requestedRefundAmount;

  /// `refund_full`, `refund_partial` or `no_refund` once resolved.
  final String? resolution;
  final double? approvedRefundAmount;

  @override
  List<Object?> get props => <Object?>[
    reason,
    status,
    chargedAmount,
    requestedRefundAmount,
    resolution,
    approvedRefundAmount,
  ];
}

/// `dispute` part of `POST /support/tickets`.
class DisputeDraft extends Equatable {
  const DisputeDraft({required this.reason, this.requestedRefundAmount});

  final DisputeReason reason;
  final double? requestedRefundAmount;

  @override
  List<Object?> get props => <Object?>[reason, requestedRefundAmount];
}
