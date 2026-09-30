import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/presentation/pages/rides_page.dart';
import 'package:ata_app/features/rides/presentation/widgets/trip_tile.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/routed_app.dart';
import '../../helpers/test_app.dart';

void main() {
  setUp(registerTestDependencies);
  tearDown(getIt.reset);

  test('a scheduled trip is its own bucket in the rides list', () {
    expect(TripStatus.parse('scheduled'), TripStatus.scheduled);
    expect(TripStatus.parse('searching'), TripStatus.active);
    expect(TripStatus.parse('cancelled'), TripStatus.cancelled);
  });

  test('scheduled pages belong to the rides tab of the bottom navigation', () {
    expect(AppRoutes.tabIndexFor('/scheduled'), 1);
    expect(AppRoutes.tabIndexFor(AppRoutes.scheduledTrip('t1')), 1);
    expect(AppRoutes.tabIndexFor('/rides'), 1);
    expect(AppRoutes.tabIndexFor('/home'), 0);
  });

  testWidgets('a scheduled trip is labelled "مجدولة"', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      wrapForTest(
        const TripTile(
          trip: TripSummary(
            id: 't1',
            destinationName: 'واجهة الرياض',
            pickupName: 'حي النرجس',
            status: TripStatus.scheduled,
            fare: 38,
            categoryName: 'اقتصادي',
          ),
        ),
      ),
    );
    expect(find.text('مجدولة'), findsOneWidget);
  });

  testWidgets('the rides page links to "رحلاتي المجدولة"', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 1600));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(wrapRouted(const RidesPage()));
    await tester.pumpAndSettle();
    expect(find.text('رحلاتي المجدولة'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey<String>('scheduled-link')));
    await tester.pumpAndSettle();
    expect(find.text('at:/scheduled'), findsOneWidget);
  });
}
