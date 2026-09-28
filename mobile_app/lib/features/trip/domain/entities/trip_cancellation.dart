import 'package:equatable/equatable.dart';

/// The `cancellation` object of a cancelled `Trip` (F14).
class TripCancellation extends Equatable {
  const TripCancellation({
    required this.stage,
    required this.reasonCode,
    this.reasonName = '',
    this.atFault = 'none',
    this.fee,
    this.feeCharged,
    this.feeStatus = 'none',
    this.excuseStatus = 'not_applicable',
    this.compensation,
  });

  /// Reason recorded by the no-show endpoint.
  static const String passengerNoShow = 'passenger_no_show';

  final String stage;
  final String reasonCode;
  final String reasonName;
  final String atFault;

  /// Passenger view only.
  final double? fee;
  final double? feeCharged;
  final String feeStatus;
  final String excuseStatus;

  /// Driver view only.
  final double? compensation;

  bool get isNoShow => reasonCode == passengerNoShow;
  bool get isPendingReview =>
      feeStatus == 'pending_review' || excuseStatus == 'pending';
  bool get wasCharged => (feeCharged ?? 0) > 0;

  @override
  List<Object?> get props => <Object?>[
    stage,
    reasonCode,
    reasonName,
    atFault,
    fee,
    feeCharged,
    feeStatus,
    excuseStatus,
    compensation,
  ];
}
