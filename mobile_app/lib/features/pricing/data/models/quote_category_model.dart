import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for [FareBreakdown].
class FareBreakdownModel extends FareBreakdown {
  const FareBreakdownModel({
    super.baseFare,
    super.distanceFare,
    super.timeFare,
    super.minFareApplied,
    super.timeMultiplier,
    super.demandMultiplier,
    super.bookingFee,
    super.serviceFee,
    super.discount,
  });

  factory FareBreakdownModel.fromJson(Map<String, dynamic> json) =>
      FareBreakdownModel(
        baseFare: JsonReaders.number(json, 'baseFare'),
        distanceFare: JsonReaders.number(json, 'distanceFare'),
        timeFare: JsonReaders.number(json, 'timeFare'),
        minFareApplied: json['minFareApplied'] == true,
        timeMultiplier: JsonReaders.optionalNumber(json, 'timeMultiplier') ?? 1,
        demandMultiplier:
            JsonReaders.optionalNumber(json, 'demandMultiplier') ?? 1,
        bookingFee: JsonReaders.number(json, 'bookingFee'),
        serviceFee: JsonReaders.number(json, 'serviceFee'),
        discount: JsonReaders.number(json, 'discount'),
      );

  static Map<String, dynamic> toJsonOf(FareBreakdown b) => <String, dynamic>{
    'baseFare': b.baseFare,
    'distanceFare': b.distanceFare,
    'timeFare': b.timeFare,
    'minFareApplied': b.minFareApplied,
    'timeMultiplier': b.timeMultiplier,
    'demandMultiplier': b.demandMultiplier,
    'bookingFee': b.bookingFee,
    'serviceFee': b.serviceFee,
    'discount': b.discount,
  };
}

/// JSON mapping for [QuoteCategory].
class QuoteCategoryModel extends QuoteCategory {
  const QuoteCategoryModel({
    required super.rideCategoryId,
    required super.code,
    required super.name,
    required super.etaMinutes,
    required super.total,
    required super.offerMin,
    required super.offerMax,
    super.driverNetEarnings,
    super.breakdown,
  });

  factory QuoteCategoryModel.fromJson(Map<String, dynamic> json) {
    final double total = JsonReaders.number(json, 'total');
    return QuoteCategoryModel(
      rideCategoryId: JsonReaders.string(json, 'rideCategoryId'),
      code: JsonReaders.string(json, 'code'),
      name: JsonReaders.string(json, 'name'),
      etaMinutes: JsonReaders.optionalInteger(json, 'etaMinutes'),
      total: total,
      driverNetEarnings: JsonReaders.number(json, 'driverNetEarnings'),
      offerMin: JsonReaders.optionalNumber(json, 'offerMin') ?? total,
      offerMax: JsonReaders.optionalNumber(json, 'offerMax') ?? total,
      breakdown: FareBreakdownModel.fromJson(
        JsonReaders.object(json, 'breakdown') ?? const <String, dynamic>{},
      ),
    );
  }

  static Map<String, dynamic> toJsonOf(QuoteCategory c) => <String, dynamic>{
    'rideCategoryId': c.rideCategoryId,
    'code': c.code,
    'name': c.name,
    'etaMinutes': c.etaMinutes,
    'total': c.total,
    'driverNetEarnings': c.driverNetEarnings,
    'offerMin': c.offerMin,
    'offerMax': c.offerMax,
    'breakdown': FareBreakdownModel.toJsonOf(c.breakdown),
  };
}
