import 'package:ata_app/features/corporate/domain/entities/trip_corporate.dart';
import 'package:equatable/equatable.dart';

/// One line of a receipt (`base_fare`, `discount`, `rounding`, …); the
/// label comes localized from the API.
class ReceiptLine extends Equatable {
  const ReceiptLine({
    required this.code,
    required this.label,
    required this.amount,
    this.source,
    this.reference,
  });

  static const String discountCode = 'discount';

  final String code;
  final String label;
  final double amount;

  /// Discount origin: `promotion` or `favorite_driver`.
  final String? source;
  final String? reference;

  bool get isDiscount => code == discountCode;

  @override
  List<Object?> get props => <Object?>[code, label, amount, source, reference];
}

/// A discount applied to the trip.
class ReceiptDiscount extends Equatable {
  const ReceiptDiscount({
    required this.source,
    required this.label,
    required this.amount,
    this.reference,
  });

  static const String promotion = 'promotion';
  static const String favoriteDriver = 'favorite_driver';

  final String source;
  final String label;
  final double amount;
  final String? reference;

  @override
  List<Object?> get props => <Object?>[source, label, amount, reference];
}

/// How the trip was paid.
class ReceiptPayment extends Equatable {
  const ReceiptPayment({
    required this.method,
    required this.status,
    required this.paidAmount,
    this.brand,
    this.last4,
    this.fallbackToCash = false,
  });

  /// `cash`, `wallet`, `card`.
  final String method;
  final String status;
  final double paidAmount;
  final String? brand;
  final String? last4;

  /// The card could not be charged and the trip was settled in cash.
  final bool fallbackToCash;

  @override
  List<Object?> get props => <Object?>[
    method,
    status,
    paidAmount,
    brand,
    last4,
    fallbackToCash,
  ];
}

/// `GET /passenger/trips/{id}/receipt` (`docs/08` §F11.6).
class Receipt extends Equatable {
  const Receipt({
    required this.tripId,
    required this.tripNumber,
    required this.status,
    required this.lines,
    required this.subtotal,
    required this.total,
    required this.payment,
    this.issuedAt,
    this.currency = 'SAR',
    this.driverName,
    this.vehicle,
    this.rideCategory,
    this.pickupName,
    this.dropoffName,
    this.distanceMeters = 0,
    this.durationSeconds = 0,
    this.discounts = const <ReceiptDiscount>[],
    this.discountTotal = 0,
    this.vatRate = 0,
    this.vatIncluded = 0,
    this.refundedTotal = 0,
    this.netPaid,
    this.corporate,
  });

  final String tripId;
  final String tripNumber;
  final String status;
  final DateTime? issuedAt;
  final String currency;
  final String? driverName;
  final String? vehicle;
  final String? rideCategory;
  final String? pickupName;
  final String? dropoffName;
  final int distanceMeters;
  final int durationSeconds;
  final List<ReceiptLine> lines;
  final List<ReceiptDiscount> discounts;
  final double subtotal;
  final double discountTotal;
  final double total;
  final double vatRate;
  final double vatIncluded;
  final ReceiptPayment payment;
  final double refundedTotal;
  final double? netPaid;

  /// Company, purpose and cost center of a corporate trip (F19).
  final TripCorporate? corporate;

  bool get isCorporate => payment.method == 'corporate' || corporate != null;

  bool get hasRefunds => refundedTotal > 0;

  @override
  List<Object?> get props => <Object?>[
    tripId,
    tripNumber,
    status,
    issuedAt,
    currency,
    driverName,
    vehicle,
    rideCategory,
    pickupName,
    dropoffName,
    distanceMeters,
    durationSeconds,
    lines,
    discounts,
    subtotal,
    discountTotal,
    total,
    vatRate,
    vatIncluded,
    payment,
    refundedTotal,
    netPaid,
    corporate,
  ];
}
