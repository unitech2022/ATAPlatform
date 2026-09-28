import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:equatable/equatable.dart';

/// Inclusive `offerMin..offerMax` range accepted for "offer your price".
class OfferBounds extends Equatable {
  const OfferBounds({required this.min, required this.max});

  final double min;
  final double max;

  bool get isValid => max > min;

  double clamp(double value) => value < min ? min : (value > max ? max : value);

  @override
  List<Object?> get props => <Object?>[min, max];
}

/// One priced ride category of a [FareQuote].
class QuoteCategory extends Equatable {
  const QuoteCategory({
    required this.rideCategoryId,
    required this.code,
    required this.name,
    required this.etaMinutes,
    required this.total,
    required this.offerMin,
    required this.offerMax,
    this.driverNetEarnings = 0,
    this.breakdown = const FareBreakdown(),
  });

  final String rideCategoryId;
  final String code;
  final String name;

  /// Minutes until the nearest driver arrives; `null` when no driver is
  /// nearby for this category.
  final int? etaMinutes;
  final double total;
  final double driverNetEarnings;
  final double offerMin;
  final double offerMax;
  final FareBreakdown breakdown;

  /// The category was priced but no driver is close enough right now.
  bool get hasNoNearbyDrivers => etaMinutes == null;

  OfferBounds get offerBounds => OfferBounds(min: offerMin, max: offerMax);

  @override
  List<Object?> get props => <Object?>[
    rideCategoryId,
    code,
    name,
    etaMinutes,
    total,
    driverNetEarnings,
    offerMin,
    offerMax,
    breakdown,
  ];
}
