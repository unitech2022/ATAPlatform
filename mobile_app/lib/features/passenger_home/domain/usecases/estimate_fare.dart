import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/passenger_home/domain/entities/fare_estimate.dart';

/// Pure Step-1 estimate: category price plus a flat surcharge per stop
/// (the prototype adds 12 SAR per extra stop).
class EstimateFare {
  const EstimateFare();

  static const double stopSurcharge = 12;

  FareEstimate call({required RideCategory category, required int stops}) {
    final RideEstimate? base = category.estimate;
    return FareEstimate(
      price: (base?.price ?? 0) + stops * stopSurcharge,
      etaMinutes: base?.etaMinutes ?? 0,
    );
  }
}
