import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/scheduled_trip_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/scheduled_trips_page.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancel_reason_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/routed_app.dart';
import '../../helpers/scheduling_fakes.dart';
import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  late FakeScheduledRepository scheduled;
  late FakeTripRepository trips;

  setUpAll(() => initializeDateFormatting('ar'));

  setUp(() async {
    await registerTestDependencies();
    scheduled = getIt<ScheduledRepository>() as FakeScheduledRepository;
    trips = getIt<TripRepository>() as FakeTripRepository;
  });
  tearDown(getIt.reset);

  /// The pages tick with real timers: tear the tree down before the test
  /// ends so they are cancelled.
  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  DateTime inFuture(Duration d) => DateTime.now().add(d);

  group('رحلاتي المجدولة (rider list)', () {
    Future<void> openList(WidgetTester tester) async {
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapRouted(const ScheduledTripsPage()));
      await tester.pump();
      await tester.pumpAndSettle();
    }

    testWidgets('lists the bookings with their status, soonest first', (
      WidgetTester tester,
    ) async {
      scheduled.scheduled = <ScheduledTrip>[
        ScheduledTrip(
          scheduledTripAt(
            inFuture(const Duration(days: 3)),
            id: 'c',
            reservation: ReservationStatus.confirmed,
          ),
        ),
        ScheduledTrip(
          scheduledTripAt(inFuture(const Duration(days: 1)), id: 'a'),
        ),
        ScheduledTrip(
          scheduledTripAt(
            inFuture(const Duration(days: 2)),
            id: 'b',
            reservation: ReservationStatus.reserved,
          ),
        ),
      ];
      await openList(tester);

      expect(find.text('رحلاتي المجدولة'), findsOneWidget);
      expect(find.text('بانتظار كابتن'), findsOneWidget);
      expect(find.text('تم حجزها من كابتن'), findsOneWidget);
      expect(find.text('الكابتن مؤكد'), findsOneWidget);
      // The reserved driver's name is shown with the date.
      expect(find.textContaining('محمد'), findsNWidgets(2));
      final double a = tester
          .getTopLeft(find.byKey(const ValueKey<String>('scheduled-a')))
          .dy;
      final double b = tester
          .getTopLeft(find.byKey(const ValueKey<String>('scheduled-b')))
          .dy;
      final double c = tester
          .getTopLeft(find.byKey(const ValueKey<String>('scheduled-c')))
          .dy;
      expect(a, lessThan(b));
      expect(b, lessThan(c));
      await leave(tester);
    });

    testWidgets('tapping a booking opens its page', (
      WidgetTester tester,
    ) async {
      scheduled.scheduled = <ScheduledTrip>[
        ScheduledTrip(
          scheduledTripAt(inFuture(const Duration(days: 1)), id: 'a'),
        ),
      ];
      await openList(tester);
      await tester.tap(find.byKey(const ValueKey<String>('scheduled-a')));
      await tester.pumpAndSettle();
      expect(find.text('at:/scheduled/a'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('empty state offers to book a ride', (
      WidgetTester tester,
    ) async {
      await openList(tester);
      expect(find.text('لا توجد رحلات مجدولة'), findsOneWidget);
      await tester.tap(find.text('احجز رحلة'));
      await tester.pumpAndSettle();
      expect(find.text('at:/home'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a failure can be retried', (WidgetTester tester) async {
      scheduled.scheduledFailure = const NetworkFailure(message: 'x');
      await openList(tester);
      expect(find.text('إعادة المحاولة'), findsOneWidget);
      scheduled
        ..scheduledFailure = null
        ..scheduled = <ScheduledTrip>[
          ScheduledTrip(scheduledTripAt(inFuture(const Duration(days: 1)))),
        ];
      await tester.tap(find.text('إعادة المحاولة'));
      await tester.pumpAndSettle();
      expect(find.text('بانتظار كابتن'), findsOneWidget);
      await leave(tester);
    });
  });

  group('scheduled trip page', () {
    Future<void> openTrip(WidgetTester tester) async {
      await tester.binding.setSurfaceSize(const Size(430, 2000));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(
        wrapRouted(const ScheduledTripPage(tripId: 't1')),
      );
      await tester.pump();
      await tester.pumpAndSettle();
    }

    testWidgets('shows the date, a live countdown, the fare and the free '
        'cancellation deadline', (WidgetTester tester) async {
      trips.active = scheduledTripAt(
        inFuture(const Duration(days: 2, hours: 3, minutes: 30)),
      );
      await openTrip(tester);

      expect(find.text('رحلة مجدولة'), findsOneWidget);
      expect(find.text('بانتظار كابتن'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('scheduled-countdown')),
        findsOneWidget,
      );
      expect(find.text('2 يوم و3 ساعة'), findsOneWidget);
      expect(find.textContaining('سعر ثابت'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('scheduled-free-cancel')),
        findsOneWidget,
      );
      expect(find.textContaining('الإلغاء مجاني حتى'), findsOneWidget);
      // No driver yet: when the search starts is explained.
      expect(find.textContaining('إن لم يحجزها كابتن'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('inside the last hour the free window is over', (
      WidgetTester tester,
    ) async {
      trips.active = scheduledTripAt(inFuture(const Duration(minutes: 40)));
      await openTrip(tester);
      expect(find.textContaining('انتهت مدة الإلغاء المجاني'), findsOneWidget);
      expect(find.textContaining('الإلغاء مجاني حتى'), findsNothing);
      await leave(tester);
    });

    testWidgets('shows the reserved driver and their confirmation', (
      WidgetTester tester,
    ) async {
      trips.active = scheduledTripAt(
        inFuture(const Duration(days: 1)),
        reservation: ReservationStatus.confirmed,
      );
      await openTrip(tester);
      expect(
        find.byKey(const ValueKey<String>('reserved-driver')),
        findsOneWidget,
      );
      expect(find.text('محمد'), findsOneWidget);
      expect(find.text('مؤكد'), findsOneWidget);
      expect(find.textContaining('أ ب ج 2841'), findsOneWidget);
      expect(find.text('الكابتن مؤكد'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('an airport booking shows the pickup details', (
      WidgetTester tester,
    ) async {
      trips.active = scheduledTripAt(
        inFuture(const Duration(days: 1)),
        airport: const TripAirport(
          code: 'RUH',
          direction: AirportDirection.pickup,
          zoneName: 'منطقة الالتقاط 3',
          terminalCode: 'T1',
          flightNumber: 'SV1020',
          freeWaitingMinutes: 15,
        ),
      );
      await openTrip(tester);
      expect(find.text('استلام من مطار RUH'), findsOneWidget);
      expect(find.textContaining('SV1020'), findsOneWidget);
      expect(find.textContaining('منطقة الالتقاط 3'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('cancelling opens the cancel flow with the fee preview', (
      WidgetTester tester,
    ) async {
      trips.active = scheduledTripAt(inFuture(const Duration(days: 1)));
      await openTrip(tester);
      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('scheduled-cancel')),
      );
      await tester.tap(find.byKey(const ValueKey<String>('scheduled-cancel')));
      await tester.pumpAndSettle();
      expect(find.byType(CancelReasonSheet), findsOneWidget);
      expect(find.text('لماذا تريد الإلغاء؟'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a cancelled booking says so and offers no cancel button', (
      WidgetTester tester,
    ) async {
      trips.active = scheduledTripAt(
        inFuture(const Duration(days: 1)),
        status: TripStage.cancelled,
      );
      await openTrip(tester);
      expect(
        find.byKey(const ValueKey<String>('scheduled-cancelled')),
        findsOneWidget,
      );
      expect(
        find.byKey(const ValueKey<String>('scheduled-cancel')),
        findsNothing,
      );
      await leave(tester);
    });

    testWidgets('once the search started the booking leads to the trip', (
      WidgetTester tester,
    ) async {
      trips.active = scheduledTripAt(
        inFuture(const Duration(minutes: 5)),
        status: TripStage.searching,
      );
      await openTrip(tester);
      expect(find.text('جارٍ البحث عن كابتن'), findsOneWidget);
      expect(find.text('تتبع الرحلة'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('scheduled-cancel')),
        findsNothing,
      );
      await tester.ensureVisible(find.text('تتبع الرحلة'));
      await tester.tap(find.text('تتبع الرحلة'));
      await tester.pumpAndSettle();
      expect(find.text('at:/trip'), findsOneWidget);
      await leave(tester);
    });
  });
}
