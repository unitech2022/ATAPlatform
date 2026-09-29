import 'package:equatable/equatable.dart';

/// `percent`, `fixed` or `free_booking_fee`.
enum PromotionType {
  percent('percent'),
  fixed('fixed'),
  freeBookingFee('free_booking_fee'),
  unknown('');

  const PromotionType(this.apiValue);

  final String apiValue;

  static PromotionType parse(String? value) => values.firstWhere(
    (PromotionType t) => t.apiValue == value && t != unknown,
    orElse: () => unknown,
  );
}

/// Tabs of the promotions page.
enum PromotionStatus {
  available('available'),
  used('used'),
  expired('expired');

  const PromotionStatus(this.apiValue);

  final String apiValue;

  static PromotionStatus parse(String? value) => values.firstWhere(
    (PromotionStatus s) => s.apiValue == value,
    orElse: () => available,
  );
}

/// A public promotion (`GET /passenger/promotions`).
class Promotion extends Equatable {
  const Promotion({
    required this.code,
    this.name = '',
    this.description = '',
    this.type = PromotionType.unknown,
    this.value = 0,
    this.maxDiscount,
    this.minFare,
    this.validTo,
    this.firstTripOnly = false,
    this.rideCategoryCodes,
    this.paymentMethods,
    this.status = PromotionStatus.available,
  });

  final String code;
  final String name;
  final String description;
  final PromotionType type;

  /// Percentage (1–100) or amount, depending on [type].
  final double value;
  final double? maxDiscount;
  final double? minFare;
  final DateTime? validTo;
  final bool firstTripOnly;

  /// `null` = every category / payment method.
  final List<String>? rideCategoryCodes;
  final List<String>? paymentMethods;
  final PromotionStatus status;

  /// [status], downgraded to expired once [validTo] has passed.
  PromotionStatus statusAt(DateTime now) =>
      status == PromotionStatus.available &&
          validTo != null &&
          !now.isBefore(validTo!)
      ? PromotionStatus.expired
      : status;

  @override
  List<Object?> get props => <Object?>[
    code,
    name,
    description,
    type,
    value,
    maxDiscount,
    minFare,
    validTo,
    firstTripOnly,
    rideCategoryCodes,
    paymentMethods,
    status,
  ];
}
