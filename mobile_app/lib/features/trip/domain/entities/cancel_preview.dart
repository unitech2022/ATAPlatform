import 'package:equatable/equatable.dart';

/// `POST /passenger|driver/trips/{id}/cancel/preview` (F14).
class CancelPreview extends Equatable {
  const CancelPreview({
    required this.stage,
    this.bookingType = 'now',
    this.fee = 0,
    this.penaltyPoints = 0,
    this.isFree = true,
    this.freeUntil,
    this.requiresReview = false,
    this.message = '',
  });

  final String stage;
  final String bookingType;

  /// Passenger fee (a potential value when [requiresReview]).
  final double fee;

  /// Driver reliability points.
  final int penaltyPoints;
  final bool isFree;

  /// End of the free window when it is still in the future.
  final DateTime? freeUntil;

  /// An excusable reason: operations review it before fees / points.
  final bool requiresReview;

  /// Localized explanation from the API.
  final String message;

  /// Scheduled bookings follow the scheduled-ride rules (F17).
  bool get isScheduled => bookingType == 'scheduled';

  bool get hasFee => fee > 0;

  @override
  List<Object?> get props => <Object?>[
    stage,
    bookingType,
    fee,
    penaltyPoints,
    isFree,
    freeUntil,
    requiresReview,
    message,
  ];
}
