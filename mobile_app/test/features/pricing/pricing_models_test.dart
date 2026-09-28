import 'package:ata_app/features/pricing/data/models/demand_model.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/pricing_fakes.dart';
import '../../helpers/trip_fakes.dart';

const Map<String, dynamic> _quoteJson = <String, dynamic>{
  'quoteId': 'q1',
  'expiresAt': '2026-09-28T12:05:00Z',
  'distanceMeters': 12000,
  'durationSeconds': 1200,
  'pickupZone': <String, dynamic>{'id': 'z1', 'name': 'شمال الرياض'},
  'demand': <String, dynamic>{
    'code': 'high',
    'name': 'مرتفع',
    'multiplier': 1.5,
    'color': '#D98E04',
  },
  'categories': <Map<String, dynamic>>[
    <String, dynamic>{
      'rideCategoryId': 'c1',
      'code': 'economy',
      'name': 'اقتصادي',
      'etaMinutes': 4,
      'total': 42,
      'driverNetEarnings': 31.5,
      'offerMin': 29.5,
      'offerMax': 54.5,
      'breakdown': <String, dynamic>{
        'baseFare': 8,
        'distanceFare': 18,
        'timeFare': 6,
        'minFareApplied': false,
        'timeMultiplier': 1,
        'demandMultiplier': 1.5,
        'bookingFee': 2,
        'serviceFee': 4,
        'discount': 0,
      },
    },
  ],
};

void main() {
  test('QuoteModel parses the /pricing/quote response', () {
    final QuoteModel quote = QuoteModel.fromJson(_quoteJson);
    expect(quote.quoteId, 'q1');
    expect(quote.expiresAt, DateTime.utc(2026, 9, 28, 12, 5));
    expect(quote.pickupZone?.name, 'شمال الرياض');
    expect(quote.demand.code, DemandCode.high);
    expect(quote.demand.multiplier, 1.5);
    expect(quote.isExpiredAt(DateTime.utc(2026, 9, 28, 12, 4, 59)), isFalse);
    expect(quote.isExpiredAt(DateTime.utc(2026, 9, 28, 12, 5)), isTrue);

    final QuoteCategory category = quote.forCategory('c1')!;
    expect(category.total, 42);
    expect(category.offerBounds, const OfferBounds(min: 29.5, max: 54.5));
    expect(category.breakdown.demandMultiplier, 1.5);
    expect(category.breakdown.hasTimeMultiplier, isFalse);
    expect(category.breakdown.bookingFee, 2);
    expect(quote.forCategory('missing'), isNull);
    expect(QuoteModel.fromJson(quote.toJson()), quote);
  });

  test('QuoteModel tolerates missing optional fields', () {
    final QuoteModel quote = QuoteModel.fromJson(const <String, dynamic>{
      'quoteId': 'q2',
      'distanceMeters': 100,
      'durationSeconds': 60,
      'categories': <Map<String, dynamic>>[
        <String, dynamic>{'rideCategoryId': 'c1', 'total': 20},
      ],
    });
    expect(quote.pickupZone, isNull);
    expect(quote.demand, DemandLevel.normal);
    expect(quote.expiresAt.isAfter(DateTime.now()), isTrue);
    final QuoteCategory category = quote.categories.single;
    expect(category.offerMin, 20);
    expect(category.offerMax, 20);
    expect(category.offerBounds.isValid, isFalse);
    expect(category.breakdown.timeMultiplier, 1);
  });

  test('DemandModel maps unknown codes to normal', () {
    final DemandModel demand = DemandModel.fromJson(const <String, dynamic>{
      'code': 'very_high',
      'name': 'مرتفع جداً',
      'multiplier': 1.9,
    });
    expect(demand.code, DemandCode.veryHigh);
    expect(demand.code.isElevated, isTrue);
    expect(DemandModel.fromJson(demand.toJson()), demand);
    expect(
      DemandModel.fromJson(const <String, dynamic>{'code': 'weird'}).code,
      DemandCode.normal,
    );
  });

  test('QuoteRequestMapper mirrors the API contract', () {
    final Map<String, dynamic> body = QuoteRequestMapper.body(
      QuoteRequest(
        pickup: GeoPoint.riyadh,
        dropoff: const GeoPoint(lat: 1, lng: 2),
        stops: const <GeoPoint>[GeoPoint(lat: 3, lng: 4)],
        rideCategoryId: 'c1',
        bookingType: 'scheduled',
        scheduledAt: DateTime.utc(2026, 9, 28, 15),
      ),
    );
    expect(body['pickup'], <String, dynamic>{'lat': 24.7136, 'lng': 46.6753});
    expect(body['stops'], <Map<String, dynamic>>[
      <String, dynamic>{'lat': 3, 'lng': 4},
    ]);
    expect(body['rideCategoryId'], 'c1');
    expect(body['bookingType'], 'scheduled');
    expect(body['scheduledAt'], '2026-09-28T15:00:00.000Z');
    expect(
      QuoteRequestMapper.body(testQuoteRequest).containsKey('rideCategoryId'),
      isFalse,
    );
  });

  test('the trip request body carries quoteId and offeredPrice', () {
    final Map<String, dynamic> body = TripRequestMapper.requestBody(
      testTripRequest.copyWith(quoteId: 'q1', offeredPrice: 35),
    );
    expect(body['quoteId'], 'q1');
    expect(body['offeredPrice'], 35);
    expect(
      TripRequestMapper.requestBody(testTripRequest).containsKey('quoteId'),
      isFalse,
    );
  });

  test('OfferModel reads passengerOffered and round', () {
    final OfferModel offer = OfferModel.fromJson(const <String, dynamic>{
      'id': 'o1',
      'tripId': 't1',
      'pricingMode': 'offer',
      'round': 2,
      'passengerPrice': 35,
      'driverNetEarnings': 28,
      'expiresAt': '2026-09-28T12:00:20Z',
    });
    expect(offer.passengerOffered, isTrue);
    expect(offer.round, 2);
    expect(OfferModel.fromJson(offer.toJson()), offer);
    expect(testOffer().passengerOffered, isFalse);
    expect(testOffer().round, 1);
  });
}
