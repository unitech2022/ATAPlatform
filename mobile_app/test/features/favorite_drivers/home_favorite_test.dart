import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/passenger_home/domain/entities/favorite_selection.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
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
import '../../helpers/favorites_fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/trip_fakes.dart';

class _MockEstimate extends Mock implements EstimateTrip {}

class _MockRequest extends Mock implements RequestTrip {}

class _MockCancel extends Mock implements CancelTrip {}

FareQuote _quoteWith(
  List<FareDiscount> discounts, {
  QuotePromotion? promotion,
  bool conditional = true,
}) {
  final double discount = discounts.fold(0, (double s, d) => s + d.amount);
  return FareQuote(
    quoteId: 'q2',
    expiresAt: DateTime.utc(2026, 9, 28, 12, 5),
    distanceMeters: 12000,
    durationSeconds: 1200,
    promotion: promotion,
    favoriteDiscountConditional: conditional,
    categories: <QuoteCategory>[
      QuoteCategory(
        rideCategoryId: 'c1',
        code: 'economy',
        name: 'اقتصادي',
        etaMinutes: 4,
        total: 42 - discount,
        offerMin: 29.5,
        offerMax: 54.5,
        breakdown: FareBreakdown(discount: discount, discounts: discounts),
      ),
    ],
  );
}

const FareDiscount _fav = FareDiscount(
  source: DiscountSource.favoriteDriver,
  label: 'خصم الكابتن المفضل',
  amount: 4,
);
const FareDiscount _promo = FareDiscount(
  source: DiscountSource.promotion,
  reference: 'ATA10',
  label: 'خصم ATA10',
  amount: 5,
);

void main() {
  final AppLocalizationsAr l10n = AppLocalizationsAr();

  HomeCubit home() => HomeCubit(
    getRideCategories: GetRideCategories(FakeCatalogRepository()),
    updatePreferences: UpdatePassengerPreferences(FakePassengerRepository()),
  );

  test('selecting a favourite by name sets favoriteDriverId on the quote and '
      'the trip request; the same chip again deselects', () async {
    final HomeCubit cubit = home();
    await cubit.loadCategories();
    cubit
      ..applyQuote(testQuote)
      ..toggleFavorite(testAvailableFavorite);
    HomeState state = cubit.state;
    expect(
      state.favorite,
      const FavoriteSelection(
        driverId: 'd1',
        name: 'محمد',
        discount: FavoriteDiscount(percent: 10, maxAmount: 15),
      ),
    );
    expect(state.effectiveFavoriteDriverId, 'd1');
    expect(buildQuoteRequest(state, l10n).favoriteDriverId, 'd1');
    final TripRequest request = buildTripRequest(state, l10n);
    expect(request.favoriteDriverId, 'd1');
    expect(request.quoteId, 'q1');

    cubit.toggleFavorite(testAvailableFavorite);
    state = cubit.state;
    expect(state.favorite, isNull);
    expect(buildQuoteRequest(state, l10n).favoriteDriverId, isNull);
    expect(buildTripRequest(state, l10n).favoriteDriverId, isNull);

    // Picking another favourite replaces the selection.
    cubit
      ..toggleFavorite(testAvailableFavorite)
      ..toggleFavorite(
        const AvailableFavorite(driverId: 'd2', firstName: 'سالم'),
      );
    expect(cubit.state.favorite?.driverId, 'd2');
    cubit.clearFavorite();
    expect(cubit.state.favorite, isNull);
    await cubit.close();
  });

  test('offering your own price disables the favourite: it is kept but never '
      'sent, and comes back when the offer is cleared', () async {
    final HomeCubit cubit = home();
    await cubit.loadCategories();
    cubit
      ..applyQuote(testQuote)
      ..toggleFavorite(testAvailableFavorite)
      ..toggleOfferedPrice();
    HomeState state = cubit.state;
    expect(state.canUseFavorite, isFalse);
    expect(state.favorite?.driverId, 'd1');
    expect(state.effectiveFavoriteDriverId, isNull);
    expect(buildQuoteRequest(state, l10n).favoriteDriverId, isNull);
    expect(buildTripRequest(state, l10n).favoriteDriverId, isNull);

    cubit.toggleOfferedPrice();
    state = cubit.state;
    expect(state.canUseFavorite, isTrue);
    expect(buildTripRequest(state, l10n).favoriteDriverId, 'd1');
    await cubit.close();
  });

  group('favourite discount and promo code in the quote', () {
    Future<HomeCubit> withQuote(FareQuote quote, {bool promo = true}) async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit
        ..toggleFavorite(testAvailableFavorite)
        ..applyQuote(quote);
      if (promo) cubit.applyPromoCode('ATA10');
      return cubit;
    }

    test('the favourite line is exposed and priced per category', () async {
      final HomeCubit cubit = await withQuote(
        _quoteWith(<FareDiscount>[_fav]),
        promo: false,
      );
      expect(cubit.state.favoriteDiscountLine?.amount, 4);
      expect(cubit.state.displayPrice, 38);
      expect(cubit.state.quoteCategory?.totalBeforeDiscount, 42);
      expect(cubit.state.favoritePromoOutcome, FavoritePromoOutcome.none);
      await cubit.close();
    });

    test('favourite wins and the code is not stacked: the promo is explained '
        '(reason not_stacked), not dropped', () async {
      final HomeCubit cubit = await withQuote(
        _quoteWith(
          <FareDiscount>[_fav],
          promotion: const QuotePromotion(
            code: 'ATA10',
            valid: false,
            reason: 'not_stacked',
          ),
        ),
      );
      expect(
        cubit.state.favoritePromoOutcome,
        FavoritePromoOutcome.promoNotApplied,
      );
      expect(cubit.state.promoCode, 'ATA10');
      await cubit.close();
    });

    test('favourite wins and the quote simply lacks the promo line', () async {
      final HomeCubit cubit = await withQuote(_quoteWith(<FareDiscount>[_fav]));
      expect(
        cubit.state.favoritePromoOutcome,
        FavoritePromoOutcome.promoNotApplied,
      );
      await cubit.close();
    });

    test('the promo wins: the favourite discount is explained', () async {
      final HomeCubit cubit = await withQuote(
        _quoteWith(<FareDiscount>[_promo]),
      );
      expect(
        cubit.state.favoritePromoOutcome,
        FavoritePromoOutcome.favoriteNotApplied,
      );
      await cubit.close();
    });

    test('both stack: nothing to explain', () async {
      final HomeCubit cubit = await withQuote(
        _quoteWith(<FareDiscount>[_fav, _promo]),
      );
      expect(cubit.state.favoritePromoOutcome, FavoritePromoOutcome.none);
      expect(cubit.state.quoteCategory?.total, 33);
      await cubit.close();
    });

    test('no explanation without a code, or with the price offer', () async {
      HomeCubit cubit = await withQuote(
        _quoteWith(<FareDiscount>[_promo]),
        promo: false,
      );
      expect(cubit.state.favoritePromoOutcome, FavoritePromoOutcome.none);
      await cubit.close();
      cubit = await withQuote(_quoteWith(<FareDiscount>[_fav]));
      cubit.toggleOfferedPrice();
      expect(cubit.state.favoritePromoOutcome, FavoritePromoOutcome.none);
      await cubit.close();
    });
  });

  group('TripRequestCubit and favourites', () {
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

    test('sends favoriteDriverId with a fixed price', () async {
      await build().request(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          quoteId: 'q1',
          favoriteDriverId: 'd1',
        ),
      );
      final TripRequest sent =
          verify(() => request(captureAny())).captured.single as TripRequest;
      expect(sent.favoriteDriverId, 'd1');
      expect(sent.pricingMode, PricingMode.fixed);
    });

    test('drops the favourite when offering a price', () async {
      await build().request(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          quoteId: 'q1',
          offeredPrice: 40,
          favoriteDriverId: 'd1',
        ),
      );
      final TripRequest sent =
          verify(() => request(captureAny())).captured.single as TripRequest;
      expect(sent.favoriteDriverId, isNull);
      expect(sent.pricingMode, PricingMode.offer);
    });

    test('not_favorite is flagged and gets its own message', () async {
      when(() => request(any())).thenAnswer(
        (_) async => const Left<Failure, Trip>(
          ServerFailure(
            code: 'validation_failed',
            message: '',
            details: <String, dynamic>{'favoriteDriverId': 'not_favorite'},
            statusCode: 422,
          ),
        ),
      );
      final TripRequestCubit cubit = build();
      await cubit.request(testTripRequest.copyWith(quoteId: 'q1'));
      final TripRequestState state = cubit.state;
      expect(state.isFavoriteRejected, isTrue);
      expect(state.isPromoRejected, isFalse);
    });

    test('other validation errors are not favourite rejections', () async {
      when(() => request(any())).thenAnswer(
        (_) async => const Left<Failure, Trip>(
          ServerFailure(
            code: 'validation_failed',
            message: '',
            details: <String, dynamic>{'paymentMethod': 'invalid'},
          ),
        ),
      );
      final TripRequestCubit cubit = build();
      await cubit.request(testTripRequest.copyWith(quoteId: 'q1'));
      expect(cubit.state.isFavoriteRejected, isFalse);
    });
  });
}
