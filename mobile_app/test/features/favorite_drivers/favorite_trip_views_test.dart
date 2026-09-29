import 'package:ata_app/app/safety_cubits.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:ata_app/features/favorite_drivers/presentation/widgets/add_favorite_button.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_types.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:ata_app/features/payments/data/models/receipt_model.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:ata_app/features/payments/presentation/pages/receipt_page.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/widgets/fare_breakdown_sheet.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/presentation/widgets/trip_tile.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_rewards.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:ata_app/features/trip/presentation/widgets/driver_card.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_receipt_view.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_searching_view.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/favorites_fakes.dart';
import '../../helpers/payments_fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';
import '../payments/receipt_test.dart' show receiptJson;

Trip _trip(TripStage stage, {TripFavorite? favorite, bool withDriver = true}) =>
    Trip(
      id: 't1',
      tripNumber: 'T-1',
      status: stage,
      pickup: testPickup,
      dropoff: testDropoff,
      category: testTrip.category,
      estimatedFare: 38,
      finalFare: stage == TripStage.completed ? 34 : null,
      estimatedDistanceMeters: 12000,
      estimatedDurationSeconds: 1200,
      driver: withDriver ? testDriver : null,
      vehicle: withDriver ? testVehicle : null,
      favorite: favorite,
    );

TripFavorite _favorite(
  FavoriteStatus status, {
  bool discount = false,
  String driverId = 'd1',
}) => TripFavorite(
  driverId: driverId,
  driverName: 'محمد',
  status: status,
  discountApplied: discount,
);

void main() {
  late FakeFavoriteDriversRepository repo;

  setUp(() async {
    await registerTestDependencies();
    repo = getIt<FavoriteDriversRepository>() as FakeFavoriteDriversRepository;
  });
  tearDown(getIt.reset);

  Future<void> tall(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(430, 2000));
    addTearDown(() => tester.binding.setSurfaceSize(null));
  }

  group('searching state', () {
    testWidgets('an exclusive favourite offer says we are contacting them', (
      WidgetTester tester,
    ) async {
      await tall(tester);
      await tester.pumpWidget(
        wrapForTest(
          TripSearchingView(
            trip: _trip(
              TripStage.searching,
              favorite: _favorite(FavoriteStatus.requested),
              withDriver: false,
            ),
            onCancel: () {},
          ),
        ),
      );
      expect(find.text('نتواصل مع كابتنك المفضل...'), findsOneWidget);
      expect(find.text('جاري البحث عن كابتن'), findsNothing);
      expect(find.textContaining('طلبك موجّه أولاً إلى محمد'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('favorite-fallback-notice')),
        findsNothing,
      );
    });

    testWidgets('after the fallback the normal copy returns with a notice', (
      WidgetTester tester,
    ) async {
      await tall(tester);
      for (final (FavoriteStatus status, String notice)
          in <(FavoriteStatus, String)>[
            (
              FavoriteStatus.rejected,
              'لم يتمكن محمد من الرد، نبحث لك عن كابتن آخر',
            ),
            (
              FavoriteStatus.expired,
              'لم يتمكن محمد من الرد، نبحث لك عن كابتن آخر',
            ),
            (
              FavoriteStatus.unavailable,
              'محمد غير متاح حالياً، نبحث لك عن أقرب كابتن',
            ),
          ]) {
        await tester.pumpWidget(
          wrapForTest(
            TripSearchingView(
              trip: _trip(
                TripStage.searching,
                favorite: _favorite(status),
                withDriver: false,
              ),
              onCancel: () {},
            ),
          ),
        );
        expect(find.text('جاري البحث عن كابتن'), findsOneWidget);
        expect(find.text('نتواصل مع كابتنك المفضل...'), findsNothing);
        expect(find.text(notice), findsOneWidget, reason: status.name);
      }
    });

    testWidgets('a normal request keeps the standard copy', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        wrapForTest(
          TripSearchingView(
            trip: _trip(TripStage.searching, withDriver: false),
            onCancel: () {},
          ),
        ),
      );
      expect(find.text('جاري البحث عن كابتن'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('favorite-fallback-notice')),
        findsNothing,
      );
    });
  });

  group('trip receipt view', () {
    Future<void> pumpReceipt(WidgetTester tester, Trip trip) async {
      await tall(tester);
      final PendingRatingCubit pending = PendingRatingCubit(
        getPending: getIt(),
      );
      addTearDown(pending.close);
      await tester.pumpWidget(
        wrapForTest(
          BlocProvider<PendingRatingCubit>.value(
            value: pending,
            child: SingleChildScrollView(
              child: TripReceiptView(trip: trip, onDone: () {}),
            ),
          ),
        ),
      );
      await tester.pump();
    }

    testWidgets('shows the favourite discount line and adds the driver by '
        'trip id', (WidgetTester tester) async {
      await pumpReceipt(
        tester,
        _trip(
          TripStage.completed,
          favorite: _favorite(FavoriteStatus.accepted, discount: true),
        ),
      );
      expect(find.text('خصم الكابتن المفضل'), findsOneWidget);
      expect(find.text('مطبّق'), findsOneWidget);
      // The accepted favourite is already one: heart instead of the button.
      expect(
        find.byKey(const ValueKey<String>('favorite-heart-badge')),
        findsOneWidget,
      );
      expect(
        find.byKey(const ValueKey<String>('add-favorite-button')),
        findsNothing,
      );
    });

    testWidgets('"أضف إلى المفضلة" adds the driver of the completed trip', (
      WidgetTester tester,
    ) async {
      await pumpReceipt(tester, _trip(TripStage.completed));
      expect(find.text('خصم الكابتن المفضل'), findsNothing);
      await tester.tap(
        find.byKey(const ValueKey<String>('add-favorite-button')),
      );
      await tester.pumpAndSettle();
      expect(repo.added.single.tripId, 't1');
      expect(find.text('تمت الإضافة إلى المفضلة'), findsOneWidget);
    });
  });

  testWidgets('the driver card shows a heart when the driver is a favourite', (
    WidgetTester tester,
  ) async {
    await tall(tester);
    final TripCubits trips = TripCubits.fromInjector();
    final SafetyCubits safety = SafetyCubits.fromInjector();
    addTearDown(() async {
      await safety.close();
      await trips.close();
    });
    Future<void> pump(Trip trip) => tester.pumpWidget(
      trips.provide(
        child: safety.provide(
          child: wrapForTest(
            SingleChildScrollView(child: DriverCard(trip: trip)),
          ),
        ),
      ),
    );

    await pump(
      _trip(
        TripStage.driverAssigned,
        favorite: _favorite(FavoriteStatus.accepted),
      ),
    );
    expect(
      find.byKey(const ValueKey<String>('favorite-heart-badge')),
      findsOneWidget,
    );
    expect(find.bySemanticsLabel('مفضل'), findsOneWidget);

    await pump(_trip(TripStage.driverAssigned));
    expect(
      find.byKey(const ValueKey<String>('favorite-heart-badge')),
      findsNothing,
    );
  });

  testWidgets('the breakdown sheet labels the favourite discount with its '
      'source', (WidgetTester tester) async {
    const QuoteCategory category = QuoteCategory(
      rideCategoryId: 'c1',
      code: 'economy',
      name: 'اقتصادي',
      etaMinutes: 4,
      total: 38,
      offerMin: 29.5,
      offerMax: 54.5,
      breakdown: FareBreakdown(
        baseFare: 8,
        distanceFare: 18,
        timeFare: 6,
        bookingFee: 2,
        serviceFee: 4,
        discount: 4,
        discounts: <FareDiscount>[
          FareDiscount(
            source: DiscountSource.favoriteDriver,
            amount: 4,
            label: 'خصم الكابتن المفضل',
          ),
          FareDiscount(source: DiscountSource.favoriteDriver, amount: 0),
        ],
      ),
    );
    await tester.pumpWidget(
      wrapForTest(FareBreakdownSheet(quote: testQuote, category: category)),
    );
    expect(find.text('خصم الكابتن المفضل · الكابتن المفضل'), findsOneWidget);
    expect(find.text('- 4 ر.س'), findsOneWidget);
    expect(find.text('قبل الخصم'), findsOneWidget);
    // A line without a label falls back to the source name alone.
    expect(find.text('الكابتن المفضل'), findsOneWidget);
  });

  testWidgets('the itemised receipt page offers "أضف إلى المفضلة"', (
    WidgetTester tester,
  ) async {
    await tall(tester);
    getIt.registerSingleton<PaymentsRepository>(
      FakePaymentsRepository()..receipt = ReceiptModel.fromJson(receiptJson),
    );
    await tester.pumpWidget(wrapForTest(const ReceiptPage(tripId: 't1')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(
      find.byKey(const ValueKey<String>('add-favorite-button')),
    );
    await tester.tap(find.byKey(const ValueKey<String>('add-favorite-button')));
    await tester.pumpAndSettle();
    expect(repo.added.single.tripId, 't1');
  });

  testWidgets('a completed trip of the history has a heart that adds the '
      'driver; a duplicate reads as already a favourite', (
    WidgetTester tester,
  ) async {
    repo.addFailure = const ServerFailure(
      code: 'favorite_exists',
      message: '',
      statusCode: 409,
    );
    const TripSummary summary = TripSummary(
      id: 't7',
      destinationName: 'العمل',
      pickupName: 'المنزل',
      status: TripStatus.completed,
      fare: 34,
      categoryName: 'اقتصادي',
      driverName: 'محمد',
    );
    await tester.pumpWidget(
      wrapForTest(
        const TripTile(
          trip: summary,
          favoriteAction: AddFavoriteButton(tripId: 't7', compact: true),
        ),
      ),
    );
    await tester.tap(find.byKey(const ValueKey<String>('add-favorite-heart')));
    await tester.pumpAndSettle();
    expect(repo.added.single.tripId, 't7');
    expect(find.byTooltip('الكابتن في مفضلتك'), findsOneWidget);
  });

  test('trip.favorite_fallback opens the trip (docs/08 §F13.7)', () {
    const ParseDeepLink parse = ParseDeepLink();
    expect(
      NotificationTypes.fallbackLink(
        NotificationTypes.tripFavoriteFallback,
        <String, dynamic>{'tripId': 't1'},
      ),
      'ata://trip/t1',
    );
    expect(
      parse(
        const DeepLinkParams(
          link: 'ata://trip/t1',
          isDriver: false,
          hasActiveTrip: true,
        ),
      )?.route,
      '/trip',
    );
    expect(NotificationTypes.categoryOf('trip.favorite_fallback'), 'trips');
  });
}
