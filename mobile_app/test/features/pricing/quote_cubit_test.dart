import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/pricing/domain/usecases/get_fare_quote.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/pricing_fakes.dart';

void main() {
  late FakePricingRepository repository;
  final DateTime now = DateTime.utc(2026, 9, 28, 12);
  const Duration debounce = Duration(milliseconds: 20);
  const QuoteRequest withStop = QuoteRequest(
    pickup: GeoPoint.riyadh,
    dropoff: GeoPoint(lat: 24.8433, lng: 46.7275),
    stops: <GeoPoint>[GeoPoint(lat: 24.755, lng: 46.626)],
  );

  setUp(() => repository = FakePricingRepository());

  QuoteCubit build({DateTime Function()? clock}) => QuoteCubit(
    getFareQuote: GetFareQuote(repository),
    debounce: debounce,
    now: clock ?? () => now,
  );

  blocTest<QuoteCubit, QuoteState>(
    'update debounces rapid changes into a single quote call',
    build: build,
    act: (QuoteCubit cubit) => cubit
      ..update(testQuoteRequest)
      ..update(withStop),
    wait: debounce * 3,
    expect: () => <QuoteState>[
      const QuoteState(status: QuoteStatus.loading, request: testQuoteRequest),
      const QuoteState(status: QuoteStatus.loading, request: withStop),
      QuoteState(
        status: QuoteStatus.ready,
        request: withStop,
        quote: testQuote,
      ),
    ],
    verify: (QuoteCubit cubit) {
      expect(repository.quoteCalls, 1);
      expect(repository.requests.single, withStop);
      expect(cubit.isExpired, isFalse);
      expect(cubit.state.usableQuote, testQuote);
    },
  );

  blocTest<QuoteCubit, QuoteState>(
    'the same input is not re-quoted',
    build: build,
    act: (QuoteCubit cubit) async {
      cubit.update(testQuoteRequest);
      await Future<void>.delayed(debounce * 2);
      cubit.update(testQuoteRequest);
    },
    wait: debounce * 2,
    verify: (_) => expect(repository.quoteCalls, 1),
  );

  blocTest<QuoteCubit, QuoteState>(
    'a failed quote keeps the input and exposes the failure',
    build: build,
    setUp: () => repository.failure = const NetworkFailure(message: 'offline'),
    act: (QuoteCubit cubit) => cubit.update(testQuoteRequest),
    wait: debounce * 2,
    expect: () => <QuoteState>[
      const QuoteState(status: QuoteStatus.loading, request: testQuoteRequest),
      const QuoteState(
        status: QuoteStatus.failure,
        request: testQuoteRequest,
        failure: NetworkFailure(message: 'offline'),
      ),
    ],
  );

  blocTest<QuoteCubit, QuoteState>(
    'refresh re-quotes immediately without the debounce',
    build: build,
    act: (QuoteCubit cubit) async {
      cubit.update(testQuoteRequest);
      await Future<void>.delayed(debounce * 2);
      repository.nextQuote = FareQuote(
        quoteId: 'q2',
        expiresAt: now.add(const Duration(minutes: 5)),
        distanceMeters: 12000,
        durationSeconds: 1200,
      );
      await cubit.refresh();
    },
    verify: (QuoteCubit cubit) {
      expect(repository.quoteCalls, 2);
      expect(cubit.state.status, QuoteStatus.ready);
      expect(cubit.state.quote?.quoteId, 'q2');
      expect(cubit.state.expired, isFalse);
    },
  );

  blocTest<QuoteCubit, QuoteState>(
    'the quote is flagged expired once expiresAt passes',
    build: build,
    setUp: () => repository.nextQuote = FareQuote(
      quoteId: 'short',
      expiresAt: now.add(const Duration(milliseconds: 30)),
      distanceMeters: 1,
      durationSeconds: 1,
    ),
    act: (QuoteCubit cubit) => cubit.update(testQuoteRequest),
    wait: debounce * 5,
    verify: (QuoteCubit cubit) {
      expect(cubit.state.status, QuoteStatus.ready);
      expect(cubit.state.expired, isTrue);
      expect(cubit.state.usableQuote, isNull);
      expect(cubit.state.quote?.quoteId, 'short');
    },
  );

  test('isExpired follows the injected clock', () async {
    final QuoteCubit cubit = build(
      clock: () => testQuote.expiresAt.add(const Duration(seconds: 1)),
    );
    expect(cubit.isExpired, isTrue);
    cubit.update(testQuoteRequest);
    await Future<void>.delayed(debounce * 2);
    expect(cubit.state.expired, isTrue);
    expect(cubit.isExpired, isTrue);
    await cubit.close();
  });
}
