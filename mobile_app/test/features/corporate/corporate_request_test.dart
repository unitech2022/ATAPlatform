import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_booking.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/favorites_fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  final AppLocalizationsAr l10n = AppLocalizationsAr();

  HomeCubit home() => HomeCubit(
    getRideCategories: GetRideCategories(FakeCatalogRepository()),
    updatePreferences: UpdatePassengerPreferences(FakePassengerRepository()),
  );

  const CorporateBooking ready = CorporateBooking(
    tripPurpose: 'اجتماع عميل',
    costCenterId: 'cc1',
    ready: true,
  );

  group('request mapping', () {
    test('the trip request carries corporate, purpose and cost center', () {
      final Map<String, dynamic> body = TripRequestMapper.requestBody(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          paymentMethod: 'corporate',
          tripPurpose: 'اجتماع عميل',
          costCenterId: 'cc1',
        ),
      );
      expect(body['paymentMethod'], 'corporate');
      expect(body['tripPurpose'], 'اجتماع عميل');
      expect(body['costCenterId'], 'cc1');
    });

    test('other payment methods send neither purpose nor cost center', () {
      final Map<String, dynamic> body = TripRequestMapper.requestBody(
        testTripRequest,
      );
      expect(body['paymentMethod'], 'cash');
      expect(body.containsKey('tripPurpose'), isFalse);
      expect(body.containsKey('costCenterId'), isFalse);
    });

    test('copyWith keeps the corporate fields', () {
      final TripRequest request = const TripRequest(
        pickup: testPickup,
        dropoff: testDropoff,
        rideCategoryId: 'c1',
        paymentMethod: 'corporate',
        tripPurpose: 'p',
        costCenterId: 'cc1',
      ).copyWith(quoteId: 'q1');
      expect(request.tripPurpose, 'p');
      expect(request.costCenterId, 'cc1');
      expect(TripRequestMapper.requestBody(request)['quoteId'], 'q1');
    });

    test('the quote asks the API to evaluate the company policy', () {
      final Map<String, dynamic> body = QuoteRequestMapper.body(
        const QuoteRequest(
          pickup: GeoPoint.riyadh,
          dropoff: GeoPoint.riyadh,
          paymentMethod: 'corporate',
          tripPurpose: 'اجتماع',
          costCenterId: 'cc1',
        ),
      );
      expect(body['paymentMethod'], 'corporate');
      expect(body['tripPurpose'], 'اجتماع');
      expect(body['costCenterId'], 'cc1');
      final Map<String, dynamic> plain = QuoteRequestMapper.body(
        const QuoteRequest(pickup: GeoPoint.riyadh, dropoff: GeoPoint.riyadh),
      );
      expect(plain.containsKey('paymentMethod'), isFalse);
      expect(plain.containsKey('tripPurpose'), isFalse);
      expect(plain.containsKey('costCenterId'), isFalse);
    });
  });

  group('the home draft', () {
    test('a corporate draft builds the request with payment corporate, '
        'purpose and cost center', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit
        ..selectPayment(PaymentOption.corporate)
        ..applyCorporate(ready);
      final HomeState state = cubit.state;
      expect(state.isCorporate, isTrue);
      final TripRequest request = buildTripRequest(state, l10n);
      expect(request.paymentMethod, 'corporate');
      expect(request.tripPurpose, 'اجتماع عميل');
      expect(request.costCenterId, 'cc1');
      final QuoteRequest quote = buildQuoteRequest(state, l10n);
      expect(quote.paymentMethod, 'corporate');
      expect(quote.tripPurpose, 'اجتماع عميل');
      expect(quote.costCenterId, 'cc1');
      await cubit.close();
    });

    test('the purpose is never sent once another method is chosen', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit
        ..selectPayment(PaymentOption.corporate)
        ..applyCorporate(ready)
        ..selectPayment(PaymentOption.card);
      final HomeState state = cubit.state;
      expect(buildTripRequest(state, l10n).paymentMethod, 'card');
      expect(buildTripRequest(state, l10n).tripPurpose, isNull);
      expect(buildTripRequest(state, l10n).costCenterId, isNull);
      expect(buildQuoteRequest(state, l10n).paymentMethod, isNull);
      await cubit.close();
    });

    test('the request waits for an eligible, completed company form', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      expect(cubit.state.canRequest, isTrue);
      cubit.selectPayment(PaymentOption.corporate);
      expect(cubit.state.canRequest, isFalse);
      cubit.applyCorporate(ready);
      expect(cubit.state.canRequest, isTrue);
      cubit.applyCorporate(const CorporateBooking(tripPurpose: 'x'));
      expect(cubit.state.canRequest, isFalse);
      // Cash is never gated by the company form.
      cubit.selectPayment(PaymentOption.cash);
      expect(cubit.state.canRequest, isTrue);
      await cubit.close();
    });

    test('corporate payment disables the promo code and the favourite '
        'discount; both come back with another method', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit
        ..applyQuote(testQuote)
        ..applyPromoCode('ATA10')
        ..toggleFavorite(testAvailableFavorite);
      expect(buildQuoteRequest(cubit.state, l10n).promoCode, 'ATA10');

      cubit
        ..selectPayment(PaymentOption.corporate)
        ..applyCorporate(ready);
      final HomeState state = cubit.state;
      expect(state.canUsePromo, isFalse);
      expect(state.canUseFavorite, isFalse);
      expect(state.effectivePromoCode, isNull);
      expect(state.effectiveFavoriteDriverId, isNull);
      expect(buildQuoteRequest(state, l10n).promoCode, isNull);
      expect(buildQuoteRequest(state, l10n).favoriteDriverId, isNull);
      expect(buildTripRequest(state, l10n).promoCode, isNull);
      expect(buildTripRequest(state, l10n).favoriteDriverId, isNull);

      cubit.selectPayment(PaymentOption.cash);
      expect(cubit.state.effectivePromoCode, 'ATA10');
      expect(cubit.state.effectiveFavoriteDriverId, 'd1');
      await cubit.close();
    });

    test('scheduled rides stay bookable with the company account', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit
        ..applyScheduledAt(DateTime.utc(2026, 10, 5, 9))
        ..selectPayment(PaymentOption.corporate)
        ..applyCorporate(ready);
      final TripRequest request = buildTripRequest(cubit.state, l10n);
      expect(request.bookingType, 'scheduled');
      expect(request.paymentMethod, 'corporate');
      expect(cubit.state.canRequest, isTrue);
      await cubit.close();
    });

    test('the quote key changes with the cost center and whether a purpose '
        'exists, not with every keystroke', () {
      expect(
        const CorporateBooking(tripPurpose: 'a').quoteKey,
        const CorporateBooking(tripPurpose: 'ab').quoteKey,
      );
      expect(
        const CorporateBooking(tripPurpose: 'a').quoteKey,
        isNot(const CorporateBooking().quoteKey),
      );
      expect(
        const CorporateBooking(costCenterId: 'cc1').quoteKey,
        isNot(const CorporateBooking().quoteKey),
      );
    });
  });
}
