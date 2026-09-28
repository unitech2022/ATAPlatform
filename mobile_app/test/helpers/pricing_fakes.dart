import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

const DemandLevel testHighDemand = DemandLevel(
  code: DemandCode.high,
  name: 'مرتفع',
  multiplier: 1.5,
  color: '#D98E04',
);

const QuoteCategory testQuoteCategory = QuoteCategory(
  rideCategoryId: 'c1',
  code: 'economy',
  name: 'اقتصادي',
  etaMinutes: 4,
  total: 42,
  driverNetEarnings: 31.5,
  offerMin: 29.5,
  offerMax: 54.5,
  breakdown: FareBreakdown(
    baseFare: 8,
    distanceFare: 18,
    timeFare: 6,
    minFareApplied: false,
    timeMultiplier: 1,
    demandMultiplier: 1.5,
    bookingFee: 2,
    serviceFee: 4,
    discount: 0,
  ),
);

final FareQuote testQuote = FareQuote(
  quoteId: 'q1',
  expiresAt: DateTime.utc(2026, 9, 28, 12, 5),
  distanceMeters: 12000,
  durationSeconds: 1200,
  pickupZone: const QuoteZone(id: 'z1', name: 'شمال الرياض'),
  demand: testHighDemand,
  categories: const <QuoteCategory>[
    testQuoteCategory,
    QuoteCategory(
      rideCategoryId: 'c2',
      code: 'family',
      name: 'عائلي',
      etaMinutes: 6,
      total: 60,
      offerMin: 42,
      offerMax: 78,
    ),
  ],
);

const QuoteRequest testQuoteRequest = QuoteRequest(
  pickup: GeoPoint.riyadh,
  dropoff: GeoPoint(lat: 24.8433, lng: 46.7275),
);

/// In-memory pricing repository; counts calls and can be switched to fail.
class FakePricingRepository implements PricingRepository {
  FakePricingRepository({FareQuote? quote, this.demand = testHighDemand})
    : nextQuote = quote ?? testQuote;

  FareQuote nextQuote;
  DemandLevel demand;
  Failure? failure;
  int quoteCalls = 0;
  int demandCalls = 0;
  final List<QuoteRequest> requests = <QuoteRequest>[];

  @override
  Future<Either<Failure, FareQuote>> quote(QuoteRequest request) async {
    quoteCalls++;
    requests.add(request);
    final Failure? failure = this.failure;
    return failure == null
        ? Right<Failure, FareQuote>(nextQuote)
        : Left<Failure, FareQuote>(failure);
  }

  @override
  Future<Either<Failure, DemandLevel>> demandAt(GeoPoint point) async {
    demandCalls++;
    final Failure? failure = this.failure;
    return failure == null
        ? Right<Failure, DemandLevel>(demand)
        : Left<Failure, DemandLevel>(failure);
  }
}
