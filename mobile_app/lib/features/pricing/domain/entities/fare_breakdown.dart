import 'package:equatable/equatable.dart';

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
  ];
}
