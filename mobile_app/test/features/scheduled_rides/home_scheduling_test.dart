import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/scheduled_trip_page.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/trip/presentation/pages/active_trip_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/scheduling_fakes.dart';
import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';

/// Signed-in rider who accepted the terms (the router opens /home).
final AuthSession _rider = testSession.copyWith(
  user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
);

void main() {
  late FakeTripRepository trips;
  late FakePricingRepository pricing;

  setUp(() async {
    await registerTestDependencies(auth: FakeAuthRepository(restored: _rider));
    trips = getIt<TripRepository>() as FakeTripRepository;
    pricing = getIt<PricingRepository>() as FakePricingRepository;
  });
  tearDown(getIt.reset);

  Future<void> openHome(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 3200));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(buildTestApp());
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(find.byType(HomePage), findsOneWidget);
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  Future<void> pickTime(WidgetTester tester) async {
    await tester.tap(find.text('جدولة'));
    await tester.pumpAndSettle();
    expect(find.text('متى تريد رحلتك؟'), findsOneWidget);
    // Two days from now, 18:30 (always inside the window).
    final DateTime day = DateTime.now().add(const Duration(days: 2));
    final Finder dayChip = find.byKey(
      ValueKey<String>('sched-day-${day.month}-${day.day}'),
    );
    await tester.ensureVisible(dayChip);
    await tester.tap(dayChip);
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey<String>('sched-hour-18')));
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey<String>('sched-minute-30')));
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey<String>('schedule-confirm')));
    await tester.pumpAndSettle();
  }

  testWidgets('"جدولة" opens the picker; the confirmed time is shown, the '
      'quote is priced for it and the surge badge is hidden', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    // Immediate ride: the demand badge shows and the button says "اطلب".
    expect(find.textContaining('الطلب'), findsWidgets);
    expect(find.textContaining('اطلب اقتصادي'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('scheduled-summary')),
      findsNothing,
    );

    await pickTime(tester);
    expect(
      find.byKey(const ValueKey<String>('scheduled-summary')),
      findsOneWidget,
    );
    expect(find.text('موعد الرحلة'), findsOneWidget);
    expect(find.text('تعديل'), findsOneWidget);
    // The booked date and time are spelled out.
    expect(find.textContaining('6:30 م'), findsWidgets);
    expect(find.textContaining('احجز اقتصادي'), findsOneWidget);
    expect(find.textContaining('اطلب اقتصادي'), findsNothing);
    expect(find.textContaining('سعر ثابت'), findsOneWidget);
    // No surge badge for a scheduled ride.
    expect(find.textContaining('الطلب مرتفع'), findsNothing);

    await tester.pump(const Duration(seconds: 1));
    final request = pricing.requests.last;
    expect(request.bookingType, 'scheduled');
    expect(request.scheduledAt?.hour, 18);
    expect(request.scheduledAt?.minute, 30);
    await leave(tester);
  });

  testWidgets('"الآن" goes back to an immediate ride', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    await pickTime(tester);
    await tester.tap(find.text('الآن'));
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey<String>('scheduled-summary')),
      findsNothing,
    );
    expect(find.textContaining('اطلب اقتصادي'), findsOneWidget);
    await tester.pump(const Duration(seconds: 1));
    expect(pricing.requests.last.bookingType, 'now');
    await leave(tester);
  });

  testWidgets('booking creates a scheduled trip and opens its page instead '
      'of the active trip screen', (WidgetTester tester) async {
    final DateTime at = DateTime.now().add(const Duration(days: 2));
    trips
      ..requestResult = scheduledTripAt(at, id: 't7')
      ..active = scheduledTripAt(at, id: 't7');
    await openHome(tester);
    await pickTime(tester);
    await tester.pump(const Duration(seconds: 1));

    await tester.tap(find.byType(AtaButton).last);
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();

    final request = trips.requests.single;
    expect(request.bookingType, 'scheduled');
    expect(request.scheduledAt, isNotNull);
    expect(request.scheduledAt!.hour, 18);
    // The rider lands on the booking, not on the pinned /trip page.
    expect(find.byType(ScheduledTripPage), findsOneWidget);
    expect(find.byType(ActiveTripPage), findsNothing);
    expect(find.text('تم حجز رحلتك المجدولة'), findsOneWidget);
    await leave(tester);
  });

  testWidgets('an airport pickup needs a zone before the ride can be '
      'requested, then sends the zone and its point', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    await tester.tap(find.byKey(const ValueKey<String>('airport-row')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey<String>('airport-RUH-pickup')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(
      find.byKey(const ValueKey<String>('airport-done')),
    );
    await tester.tap(find.byKey(const ValueKey<String>('airport-done')));
    await tester.pumpAndSettle();

    // The pickup row shows the airport and the request is blocked.
    expect(find.text('اختر منطقة الالتقاط في المطار'), findsOneWidget);
    final Finder request = find.byType(AtaButton).last;
    expect(tester.widget<AtaButton>(request).onPressed, isNull);

    await tester.tap(find.byKey(const ValueKey<String>('airport-row')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey<String>('zone-z1')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(
      find.byKey(const ValueKey<String>('airport-done')),
    );
    await tester.tap(find.byKey(const ValueKey<String>('airport-done')));
    await tester.pumpAndSettle();

    await tester.pump(const Duration(seconds: 1));
    final quote = pricing.requests.last;
    expect(quote.airportPickupZoneId, 'z1');
    expect(quote.pickup.lat, 24.9601);

    expect(
      tester.widget<AtaButton>(find.byType(AtaButton).last).onPressed,
      isNotNull,
    );
    await tester.tap(find.byType(AtaButton).last);
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    final sent = trips.requests.single;
    expect(sent.airportPickupZoneId, 'z1');
    expect(sent.pickup.point.lat, 24.9601);
    expect(sent.pickup.name, contains('مطار الملك خالد'));
    await leave(tester);
  });
}
