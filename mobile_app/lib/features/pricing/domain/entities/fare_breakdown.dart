import 'package:equatable/equatable.dart';

/// Where a discount comes from (`docs/08` §F11.6).
enum DiscountSource {
  promotion('promotion'),
  favoriteDriver('favorite_driver'),
  other('');

  const DiscountSource(this.apiValue);

  final String apiValue;

  static DiscountSource parse(String? value) => values.firstWhere(
    (DiscountSource s) => s.apiValue == value && s != other,
    orElse: () => other,
  );
}

/// One entry of `breakdown.discounts[]`.
class FareDiscount extends Equatable {
  const FareDiscount({
    required this.source,
    required this.amount,
    this.reference = '',
    this.label = '',
  });

  final DiscountSource source;

  /// The promo code for [DiscountSource.promotion].
  final String reference;

  /// API-localized label such as "خصم ATA10".
  final String label;
  final double amount;

  @override
  List<Object?> get props => <Object?>[source, reference, label, amount];
}

/// The lines that make up one category total of a fare quote.
class FareBreakdown extends Equatable {
  const FareBreakdown({
    this.baseFare = 0,
    this.distanceFare = 0,
    this.timeFare = 0,
    this.minFareApplied = false,
    this.timeMultiplier = 1,
    this.demandMultiplier = 1,
    this.bookingFee = 0,
    this.serviceFee = 0,
    this.discount = 0,
    this.discounts = const <FareDiscount>[],
  });

  final double baseFare;
  final double distanceFare;
  final double timeFare;
  final bool minFareApplied;
  final double timeMultiplier;
  final double demandMultiplier;
  final double bookingFee;
  final double serviceFee;
  final double discount;

  /// Discount lines by source; their sum is [discount].
  final List<FareDiscount> discounts;

  bool get hasTimeMultiplier => timeMultiplier != 1;
  bool get hasDemandMultiplier => demandMultiplier != 1;
  bool get hasDiscount => discount > 0;

  @override
  List<Object?> get props => <Object?>[
    baseFare,
    distanceFare,
    timeFare,
    minFareApplied,
    timeMultiplier,
    demandMultiplier,
    bookingFee,
    serviceFee,
    discount,
    discounts,
  ];
}
