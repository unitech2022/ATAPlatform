import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:ata_app/features/favorite_drivers/presentation/pages/favorite_drivers_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/favorites_fakes.dart';
import '../../helpers/test_app.dart';

void main() {
  late FakeFavoriteDriversRepository repo;

  setUp(() async {
    await registerTestDependencies();
    repo = getIt<FavoriteDriversRepository>() as FakeFavoriteDriversRepository;
  });
  tearDown(getIt.reset);

  Future<void> openPage(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(430, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(wrapForTest(const FavoriteDriversPage()));
    // The availability lookup is debounced by 600 ms.
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 1));
  }

  testWidgets('lists the favourites with rating, vehicle, trips together, '
      'last trip and the available-now ETA badge', (WidgetTester tester) async {
    repo
      ..favorites = <FavoriteDriver>[
        FavoriteDriver(
          driverId: 'd1',
          firstName: 'محمد',
          ratingAvg: 4.93,
          vehicle: testFavoriteDriver.vehicle,
          tripsTogether: 6,
          lastTripAt: DateTime.utc(2026, 9, 20),
        ),
        const FavoriteDriver(driverId: 'd2', firstName: 'سالم', ratingAvg: 4.7),
      ]
      ..available = <AvailableFavorite>[testAvailableFavorite];
    await openPage(tester);

    expect(find.text('السائقون المفضلون'), findsOneWidget);
    expect(find.text('محمد'), findsOneWidget);
    expect(find.text('سالم'), findsOneWidget);
    expect(find.text('تقييم 4.93'), findsOneWidget);
    expect(find.text('Toyota Camry · أبيض'), findsOneWidget);
    expect(find.text('6 رحلات معاً'), findsOneWidget);
    expect(find.textContaining('آخر رحلة'), findsOneWidget);
    // Only the driver who can take a trip now gets the badge.
    expect(find.byKey(const ValueKey<String>('available-d1')), findsOneWidget);
    expect(find.text('متاح الآن · يصل خلال 4 دقائق'), findsOneWidget);
    expect(find.byKey(const ValueKey<String>('available-d2')), findsNothing);
    expect(repo.queries.single.pickup.lat, isNonZero);
    await leave(tester);
  });

  testWidgets('removing asks for confirmation; cancel keeps the driver', (
    WidgetTester tester,
  ) async {
    repo.favorites = <FavoriteDriver>[testFavoriteDriver];
    await openPage(tester);

    await tester.tap(find.byKey(const ValueKey<String>('remove-d1')));
    await tester.pumpAndSettle();
    expect(find.text('إزالة محمد من المفضلة؟'), findsOneWidget);
    await tester.tap(find.text('إلغاء'));
    await tester.pumpAndSettle();
    expect(repo.removed, isEmpty);
    expect(find.text('محمد'), findsOneWidget);
    await leave(tester);
  });

  testWidgets('confirming removes the driver, shows a snackbar and then the '
      'empty state', (WidgetTester tester) async {
    repo.favorites = <FavoriteDriver>[testFavoriteDriver];
    await openPage(tester);

    await tester.tap(find.byKey(const ValueKey<String>('remove-d1')));
    await tester.pumpAndSettle();
    await tester.tap(
      find.byKey(const ValueKey<String>('confirm-remove-favorite')),
    );
    await tester.pumpAndSettle();

    expect(repo.removed, <String>['d1']);
    expect(find.text('تمت إزالة محمد من المفضلة'), findsOneWidget);
    expect(find.text('لا يوجد كباتن مفضلون بعد'), findsOneWidget);
    await leave(tester);
  });

  testWidgets('an empty list shows the empty state', (
    WidgetTester tester,
  ) async {
    await openPage(tester);
    expect(find.text('لا يوجد كباتن مفضلون بعد'), findsOneWidget);
    expect(find.textContaining('من شاشة التقييم'), findsOneWidget);
    await leave(tester);
  });

  testWidgets('a failed removal keeps the driver and shows the error', (
    WidgetTester tester,
  ) async {
    repo
      ..favorites = <FavoriteDriver>[testFavoriteDriver]
      ..removeFailure = const NetworkFailure(message: 'offline');
    await openPage(tester);
    await tester.tap(find.byKey(const ValueKey<String>('remove-d1')));
    await tester.pumpAndSettle();
    await tester.tap(
      find.byKey(const ValueKey<String>('confirm-remove-favorite')),
    );
    await tester.pumpAndSettle();
    expect(find.text('محمد'), findsOneWidget);
    expect(find.textContaining('تعذر الاتصال'), findsOneWidget);
    await leave(tester);
  });
}
