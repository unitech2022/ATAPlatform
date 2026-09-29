import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/estimate_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/request_trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/trip_fakes.dart';

class _MockEstimate extends Mock implements EstimateTrip {}

class _MockRequest extends Mock implements RequestTrip {}

class _MockCancel extends Mock implements CancelTrip {}

void main() {
  final AppLocalizationsAr l10n = AppLocalizationsAr();

  HomeCubit home() => HomeCubit(
    getRideCategories: GetRideCategories(FakeCatalogRepository()),
    updatePreferences: UpdatePassengerPreferences(FakePassengerRepository()),
  );

  test('the applied code goes to the quote and the request, but never '
      'with "offer your price"', () async {
    final HomeCubit cubit = home();
    await cubit.loadCategories();
    cubit
      ..applyQuote(testQuote)
      ..applyPromoCode('ATA10');
    HomeState state = cubit.state;
    expect(state.effectivePromoCode, 'ATA10');
    expect(buildQuoteRequest(state, l10n).promoCode, 'ATA10');
    expect(buildTripRequest(state, l10n).promoCode, 'ATA10');
    final PromoValidationParams draft = buildPromoContext(state);
    expect(draft.quoteId, 'q1');
    expect(draft.rideCategoryId, 'c1');
    expect(draft.paymentMethod, 'cash');
    expect(draft.bookingType, 'now');

    cubit.toggleOfferedPrice();
    state = cubit.state;
    expect(state.canUsePromo, isFalse);
    expect(state.promoCode, 'ATA10');
    expect(state.effectivePromoCode, isNull);
    expect(buildQuoteRequest(state, l10n).promoCode, isNull);
    expect(buildTripRequest(state, l10n).promoCode, isNull);

    cubit
      ..toggleOfferedPrice()
      ..applyPromoCode(null);
    expect(cubit.state.promoCode, isNull);
    await cubit.close();
  });

  test('quote request and trip request bodies carry promoCode', () {
    expect(
      QuoteRequestMapper.body(
        const QuoteRequest(
          pickup: GeoPoint.riyadh,
          dropoff: GeoPoint.riyadh,
          promoCode: 'ATA10',
        ),
      )['promoCode'],
      'ATA10',
    );
    expect(
      TripRequestMapper.requestBody(
        testTripRequest.copyWith(),
      ).containsKey('promoCode'),
      isFalse,
    );
    expect(
      TripRequestMapper.requestBody(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          promoCode: 'ATA10',
        ),
      )['promoCode'],
      'ATA10',
    );
  });

  test('the quote parses promotion and discounts[]', () {
    final FareQuote quote = QuoteModel.fromJson(const <String, dynamic>{
      'quoteId': 'q9',
      'expiresAt': '2026-09-29T12:05:00Z',
      'categories': <Map<String, dynamic>>[
        <String, dynamic>{
          'rideCategoryId': 'c1',
          'code': 'economy',
          'name': 'اقتصادي',
          'total': 46,
          'breakdown': <String, dynamic>{
            'discount': 5,
            'discounts': <Map<String, dynamic>>[
              <String, dynamic>{
                'source': 'promotion',
                'reference': 'ATA10',
                'label': 'خصم ATA10',
                'amount': 5,
              },
            ],
          },
        },
      ],
      'promotion': <String, dynamic>{
        'code': 'ATA10',
        'valid': false,
        'reason': 'min_fare',
      },
    });
    final QuoteCategory c = quote.categories.single;
    expect(c.hasDiscount, isTrue);
    expect(c.totalBeforeDiscount, 51);
    expect(c.breakdown.discounts.single.source, DiscountSource.promotion);
    expect(
      quote.promotion,
      const QuotePromotion(code: 'ATA10', valid: false, reason: 'min_fare'),
    );
    final FareQuote back = QuoteModel.fromJson((quote as QuoteModel).toJson());
    expect(back.promotion, quote.promotion);
    expect(back.categories, quote.categories);
  });

  group('TripRequestCubit and promo codes', () {
    late _MockRequest request;
    late _MockEstimate estimate;

    setUpAll(() => registerFallbackValue(testTripRequest));

    setUp(() {
      request = _MockRequest();
      estimate = _MockEstimate();
      when(() => estimate(any())).thenAnswer(
        (_) async => const Right<Failure, TripEstimate>(testEstimate),
      );
      when(
        () => request(any()),
      ).thenAnswer((_) async => const Right<Failure, Trip>(testTrip));
    });

    TripRequestCubit build() => TripRequestCubit(
      estimateTrip: estimate,
      requestTrip: request,
      cancelTrip: _MockCancel(),
    );

    test('sends the promo code with a fixed price', () async {
      await build().request(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          quoteId: 'q1',
          promoCode: 'ATA10',
        ),
      );
      final TripRequest sent =
          verify(() => request(captureAny())).captured.single as TripRequest;
      expect(sent.promoCode, 'ATA10');
      expect(sent.pricingMode, PricingMode.fixed);
    });

    test('drops the promo code when offering a price', () async {
      await build().request(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          quoteId: 'q1',
          offeredPrice: 40,
          promoCode: 'ATA10',
        ),
      );
      final TripRequest sent =
          verify(() => request(captureAny())).captured.single as TripRequest;
      expect(sent.promoCode, isNull);
      expect(sent.pricingMode, PricingMode.offer);
    });

    test('flags promo_* failures', () async {
      when(() => request(any())).thenAnswer(
        (_) async => const Left<Failure, Trip>(
          ServerFailure(code: 'promo_usage_limit_reached', message: ''),
        ),
      );
      final TripRequestCubit cubit = build();
      await cubit.request(testTripRequest.copyWith(quoteId: 'q1'));
      final TripRequestState state = cubit.state;
      expect(state.isPromoRejected, isTrue);
    });
  });
}
