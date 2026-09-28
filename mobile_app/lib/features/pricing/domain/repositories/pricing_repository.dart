import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// `/pricing/*` (F10).
abstract interface class PricingRepository {
  Future<Either<Failure, FareQuote>> quote(QuoteRequest request);
  Future<Either<Failure, DemandLevel>> demandAt(GeoPoint point);
}
