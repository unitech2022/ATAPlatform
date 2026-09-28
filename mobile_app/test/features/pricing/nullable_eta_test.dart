import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/pricing/data/models/quote_category_model.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/pricing_fakes.dart';

void main() {
  const QuoteCategory noDrivers = QuoteCategory(
    rideCategoryId: 'c1',
    code: 'economy',
    name: 'اقتصادي',
    etaMinutes: null,
    total: 42,
    offerMin: 30,
    offerMax: 55,
  );

  test('a quote category without nearby drivers has a null ETA', () {
    final QuoteCategory parsed =
        QuoteCategoryModel.fromJson(const <String, dynamic>{
          'rideCategoryId': 'c1',
          'code': 'economy',
          'name': 'اقتصادي',
          'etaMinutes': null,
          'total': 42,
        });
    expect(parsed.etaMinutes, isNull);
    expect(parsed.hasNoNearbyDrivers, isTrue);
    expect(QuoteCategoryModel.toJsonOf(parsed)['etaMinutes'], isNull);
  });

  test('displayEta follows the quote, including "no drivers"', () {
    final HomeState catalogOnly = const HomeState(
      categories: testCategories,
    ).copyWith(selectedCategoryId: 'c1');
    expect(catalogOnly.displayEta, 0);

    final FareQuote quote = FareQuote(
      quoteId: 'q2',
      expiresAt: testQuote.expiresAt,
      distanceMeters: testQuote.distanceMeters,
      durationSeconds: testQuote.durationSeconds,
      pickupZone: testQuote.pickupZone,
      demand: testQuote.demand,
      categories: const <QuoteCategory>[noDrivers],
    );
    expect(catalogOnly.copyWith(quote: quote).displayEta, isNull);
    expect(catalogOnly.copyWith(quote: testQuote).displayEta, 4);
  });

  test('outstanding_balance exposes the owed amount', () {
    const TripRequestState state = TripRequestState(
      status: TripRequestStatus.failure,
      failure: ServerFailure(
        code: 'outstanding_balance',
        message: '',
        details: <String, dynamic>{'amount': -23.5},
      ),
    );
    expect(state.isOutstandingBalance, isTrue);
    expect(state.outstandingAmount, 23.5);
  });
}
