import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:ata_app/features/favorite_drivers/presentation/pages/favorite_drivers_page.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/offered_price_row.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/favorites_fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/test_app.dart';

final AuthSession _rider = testSession.copyWith(
  user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
);

const AvailableFavorite _mohammed = AvailableFavorite(
  driverId: 'd1',
  firstName: 'محمد',
  ratingAvg: 4.9,
  etaMinutes: 4,
  discount: FavoriteDiscount(percent: 10, maxAmount: 15),
);

FareQuote _quote({List<FareDiscount> discounts = const <FareDiscount>[]}) {
  final double discount = discounts.fold(0, (double s, d) => s + d.amount);
  return FareQuote(
    quoteId: 'q-fav',
    expiresAt: DateTime.now().add(const Duration(hours: 1)),
    distanceMeters: 12000,
    durationSeconds: 1200,
    favoriteDiscountConditional: discounts.isNotEmpty,
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

void main() {
  late FakeFavoriteDriversRepository favorites;
  late FakePricingRepository pricing;

  setUp(() async {
    await registerTestDependencies(auth: FakeAuthRepository(restored: _rider));
    favorites =
        getIt<FavoriteDriversRepository>() as FakeFavoriteDriversRepository
          ..available = <AvailableFavorite>[_mohammed];
    pricing = getIt<PricingRepository>() as FakePricingRepository
      ..nextQuote = _quote();
  });
  tearDown(getIt.reset);

  Future<void> openHome(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 3000));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(buildTestApp());
    await tester.pump();
    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();
    expect(find.byType(HomePage), findsOneWidget);
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  Future<void> settleQuote(WidgetTester tester) async {
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
  }

  testWidgets('shows the available favourites as chips with rating, ETA and '
      'the discount badge, and asks the API around the pickup', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    expect(find.text('السائقون المفضلون'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('favorite-chip-d1')),
      findsOneWidget,
    );
    expect(find.text('محمد'), findsOneWidget);
    expect(find.textContaining('تقييم 4.9'), findsOneWidget);
    expect(find.text('خصم 10%'), findsOneWidget);
    // Once the categories load the lookup carries the selected category.
    expect(favorites.queries.last.rideCategoryId, 'c1');
    await leave(tester);
  });

  testWidgets('selecting a favourite sends favoriteDriverId with the quote, '
      'explains the fallback and shows the favourite discount', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    pricing.nextQuote = _quote(
      discounts: const <FareDiscount>[
        FareDiscount(
          source: DiscountSource.favoriteDriver,
          label: 'خصم الكابتن المفضل',
          amount: 4,
        ),
      ],
    );
    await tester.tap(find.byKey(const ValueKey<String>('favorite-chip-d1')));
    await tester.pump();
    expect(find.text('سيصل طلبك أولاً إلى محمد'), findsOneWidget);
    expect(find.text('إن لم يكن متاحاً سنبحث عن أقرب كابتن'), findsOneWidget);
    await settleQuote(tester);

    expect(pricing.requests.last.favoriteDriverId, 'd1');
    expect(find.text('خصم الكابتن المفضل · وفّرت 4 ر.س'), findsOneWidget);
    // The category tile shows the price before and after the discount.
    expect(find.text('42 ر.س'), findsWidgets);
    expect(find.text('38 ر.س'), findsWidgets);

    // Tapping "إزالة" deselects and re-quotes without the driver.
    await tester.tap(find.byKey(const ValueKey<String>('favorite-deselect')));
    await tester.pump();
    expect(find.text('سيصل طلبك أولاً إلى محمد'), findsNothing);
    await settleQuote(tester);
    expect(pricing.requests.last.favoriteDriverId, isNull);
    await leave(tester);
  });

  testWidgets('a promo code that the favourite discount beats is explained '
      'by the sheet', (WidgetTester tester) async {
    await openHome(tester);
    await tester.tap(find.byKey(const ValueKey<String>('promo-row')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField), 'ata10');
    await tester.pump();
    await tester.tap(find.text('تطبيق'));
    await tester.pumpAndSettle();

    pricing.nextQuote = _quote(
      discounts: const <FareDiscount>[
        FareDiscount(source: DiscountSource.favoriteDriver, amount: 4),
      ],
    );
    await tester.tap(find.byKey(const ValueKey<String>('favorite-chip-d1')));
    await settleQuote(tester);

    expect(pricing.requests.last.promoCode, 'ATA10');
    expect(pricing.requests.last.favoriteDriverId, 'd1');
    // Shown in the favourite block and on the promo row; the code stays.
    expect(
      find.text('لم يُطبَّق كود الخصم: لا يُجمع مع خصم الكابتن المفضل الأكبر'),
      findsNWidgets(2),
    );
    await leave(tester);
  });

  testWidgets('offering your own price disables the favourites row and the '
      'driver is not sent', (WidgetTester tester) async {
    await openHome(tester);
    await tester.tap(find.byKey(const ValueKey<String>('favorite-chip-d1')));
    await settleQuote(tester);
    expect(pricing.requests.last.favoriteDriverId, 'd1');

    await tester.tap(
      find.descendant(
        of: find.byType(OfferedPriceRow),
        matching: find.byType(AtaToggle),
      ),
    );
    await settleQuote(tester);
    expect(find.text('غير متاح مع اقتراح السعر'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('favorite-chip-d1')),
      findsNothing,
    );
    expect(pricing.requests.last.favoriteDriverId, isNull);
    await leave(tester);
  });

  testWidgets('no favourite available now says so', (
    WidgetTester tester,
  ) async {
    favorites.available = <AvailableFavorite>[];
    await openHome(tester);
    expect(find.text('لا أحد من كباتنك المفضلين متاح الآن'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('favorites-manage')),
      findsOneWidget,
    );
    await leave(tester);
  });

  testWidgets('"إدارة" on the sheet opens the favourites page', (
    WidgetTester tester,
  ) async {
    favorites.favorites = <FavoriteDriver>[testFavoriteDriver];
    await openHome(tester);
    await tester.tap(find.byKey(const ValueKey<String>('favorites-manage')));
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(find.byType(FavoriteDriversPage), findsOneWidget);
    expect(find.text('محمد'), findsOneWidget);
    await leave(tester);
  });
}
