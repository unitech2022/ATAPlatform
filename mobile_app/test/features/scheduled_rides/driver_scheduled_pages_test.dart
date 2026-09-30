import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/driver_scheduled_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/pages/reservation_page.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_overview_card.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/routed_app.dart';
import '../../helpers/scheduling_fakes.dart';
import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  late FakeScheduledRepository repository;

  setUpAll(() => initializeDateFormatting('ar'));

  setUp(() async {
    await registerTestDependencies();
    repository = getIt<ScheduledRepository>() as FakeScheduledRepository;
  });
  tearDown(getIt.reset);

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  DateTime after(Duration d) => DateTime.now().add(d);

  /// Hosts [child] with the app-wide cubits the driver pages read.
  Widget host(Widget child) => MultiBlocProvider(
    providers: <BlocProvider<dynamic>>[
      BlocProvider<LocationStreamCubit>(
        create: (_) => LocationStreamCubit(
          requestAccess: getIt(),
          watchPosition: getIt(),
          sendLocation: getIt(),
        ),
      ),
      BlocProvider<DriverTripCubit>(
        create: (_) => DriverTripCubit(
          watchActiveTrip: getIt(),
          advanceTrip: getIt(),
          verifyPin: getIt(),
          cancelTrip: getIt(),
        ),
      ),
    ],
    child: wrapRouted(child),
  );

  Future<void> open(WidgetTester tester, Widget page) async {
    await tester.binding.setSurfaceSize(const Size(430, 2600));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(host(page));
    await tester.pump();
    await tester.pumpAndSettle();
  }

  group('marketplace', () {
    setUp(() {
      repository.market = pageOf<MarketplaceTrip>(<MarketplaceTrip>[
        marketTrip('m1'),
        marketTrip('m2', airport: true),
      ]);
    });

    testWidgets('lists the upcoming requests with areas, distance and net '
        'earnings', (WidgetTester tester) async {
      await open(tester, const DriverScheduledPage());
      expect(find.text('رحلاتي المجدولة'), findsOneWidget);
      expect(find.text('السوق'), findsOneWidget);
      expect(find.text('حجوزاتي'), findsOneWidget);
      expect(find.byKey(const ValueKey<String>('market-m1')), findsOneWidget);
      expect(find.byKey(const ValueKey<String>('market-m2')), findsOneWidget);
      expect(find.textContaining('حي الملقا'), findsNWidgets(2));
      expect(find.text('76 ر.س'), findsNWidgets(2));
      expect(find.text('مطار'), findsOneWidget);
      expect(find.textContaining('8.4 كم عنك'), findsNWidgets(2));
      expect(find.textContaining('32 كم'), findsNWidgets(2));
      expect(find.text('موقع الالتقاط تقريبي حتى الحجز'), findsNWidgets(2));
      await leave(tester);
    });

    testWidgets('reserving a trip confirms it and opens my reservations', (
      WidgetTester tester,
    ) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf('m1'),
      ]);
      await open(tester, const DriverScheduledPage());
      await tester.tap(find.byKey(const ValueKey<String>('reserve-m1')));
      await tester.pumpAndSettle();

      expect(repository.reserved, <String>['m1']);
      expect(find.text('تم حجز الرحلة، ستجدها في حجوزاتي'), findsOneWidget);
      // The tab moved to "my reservations", which lists the new one.
      expect(
        find.byKey(const ValueKey<String>('reservation-m1')),
        findsOneWidget,
      );
      expect(find.byKey(const ValueKey<String>('market-m2')), findsNothing);
      await leave(tester);
    });

    testWidgets('a trip taken by another driver disappears with the reason', (
      WidgetTester tester,
    ) async {
      repository.reserveFailure = const ServerFailure(
        code: 'reservation_taken',
        message: '',
        statusCode: 409,
      );
      await open(tester, const DriverScheduledPage());
      await tester.tap(find.byKey(const ValueKey<String>('reserve-m1')));
      await tester.pumpAndSettle();
      expect(find.text('تم حجز هذه الرحلة من كابتن آخر'), findsOneWidget);
      expect(find.byKey(const ValueKey<String>('market-m1')), findsNothing);
      expect(find.byKey(const ValueKey<String>('market-m2')), findsOneWidget);
      await leave(tester);
    });

    testWidgets('the day filter asks the API for that day', (
      WidgetTester tester,
    ) async {
      await open(tester, const DriverScheduledPage());
      final DateTime tomorrow = after(const Duration(days: 1));
      final Finder chip = find.byKey(
        ValueKey<String>('market-day-${tomorrow.month}-${tomorrow.day}'),
      );
      await tester.ensureVisible(chip);
      await tester.pumpAndSettle();
      await tester.tap(chip);
      await tester.pumpAndSettle();
      final DateTime from = repository.marketQueries.last.from!;
      expect(from.day, tomorrow.day);
      expect(repository.marketQueries.last.to!.difference(from).inHours, 24);
      await leave(tester);
    });

    testWidgets('an empty marketplace says so', (WidgetTester tester) async {
      repository.market = pageOf<MarketplaceTrip>(<MarketplaceTrip>[]);
      await open(tester, const DriverScheduledPage());
      expect(find.text('لا توجد طلبات مجدولة الآن'), findsOneWidget);
      await leave(tester);
    });
  });

  group('my reservations and the confirmation prompt', () {
    testWidgets('a due prompt is on top with a countdown and confirms', (
      WidgetTester tester,
    ) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf(
          't1',
          at: after(const Duration(hours: 1)),
          confirmDeadline: after(const Duration(minutes: 8)),
        ),
        reservationOf('t2', at: after(const Duration(days: 2))),
      ]);
      await open(tester, const DriverScheduledPage());
      await tester.tap(
        find.byKey(const ValueKey<String>('scheduled-tab-mine')),
      );
      await tester.pumpAndSettle();

      expect(
        find.byKey(const ValueKey<String>('confirm-prompt-t1')),
        findsOneWidget,
      );
      expect(
        find.byKey(const ValueKey<String>('confirm-prompt-t2')),
        findsNothing,
      );
      expect(find.text('أكّد حجزك'), findsOneWidget);
      expect(find.textContaining('المتبقي 07:'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('reservation-t2')),
        findsOneWidget,
      );

      await tester.tap(
        find.byKey(const ValueKey<String>('confirm-reservation')),
      );
      await tester.pumpAndSettle();
      expect(repository.confirmed, <String>['t1']);
      expect(find.text('تم تأكيد الحجز'), findsOneWidget);
      // Confirmed: the prompt is gone.
      expect(
        find.byKey(const ValueKey<String>('confirm-prompt-t1')),
        findsNothing,
      );
      await leave(tester);
    });

    testWidgets('past reservations are listed apart', (
      WidgetTester tester,
    ) async {
      repository.history = pageOf<Reservation>(<Reservation>[
        reservationOf('old', status: ReservationStatus.completed),
      ]);
      await open(tester, const DriverScheduledPage());
      await tester.tap(
        find.byKey(const ValueKey<String>('scheduled-tab-mine')),
      );
      await tester.pumpAndSettle();
      expect(find.text('لا توجد حجوزات هنا'), findsOneWidget);
      await tester.tap(
        find.byKey(const ValueKey<String>('reservations-history')),
      );
      await tester.pumpAndSettle();
      expect(
        find.byKey(const ValueKey<String>('reservation-old')),
        findsOneWidget,
      );
      expect(find.text('مكتملة'), findsOneWidget);
      await leave(tester);
    });
  });

  group('reservation page', () {
    testWidgets('the first prompt (T-60) shows the countdown to the pickup', (
      WidgetTester tester,
    ) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf(
          't1',
          at: after(const Duration(hours: 1)),
          confirmDeadline: after(const Duration(minutes: 9)),
        ),
      ]);
      await open(tester, const ReservationPage(tripId: 't1'));
      expect(find.text('أكّد حجزك'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('reservation-countdown')),
        findsOneWidget,
      );
      expect(find.text('سارة'), findsOneWidget);
      expect(find.text('76 ر.س'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('the final confirmation assigns the trip to the driver', (
      WidgetTester tester,
    ) async {
      repository
        ..active = pageOf<Reservation>(<Reservation>[
          reservationOf(
            't1',
            status: ReservationStatus.confirmed,
            at: after(const Duration(minutes: 15)),
            finalConfirmDeadline: after(const Duration(minutes: 5)),
          ),
        ])
        ..confirmResult = ConfirmResult(
          reservation: reservationOf('t1', status: ReservationStatus.assigned),
          trip: tripAt(TripStage.driverAssigned),
        );
      await open(tester, const ReservationPage(tripId: 't1'));
      expect(find.text('التأكيد النهائي: استعد للانطلاق'), findsOneWidget);
      await tester.tap(
        find.byKey(const ValueKey<String>('confirm-reservation')),
      );
      await tester.pumpAndSettle();

      expect(
        find.text('تم التأكيد النهائي، ستبدأ الرحلة في موعدها'),
        findsOneWidget,
      );
      // The trip went to the driver's trip feed: the router opens /driver/trip.
      final DriverTripCubit driverTrip = tester
          .element(find.byType(ReservationPage))
          .read<DriverTripCubit>();
      expect(driverTrip.state.trip?.status, TripStage.driverAssigned);
      await leave(tester);
    });

    testWidgets('an offline driver is told to go online', (
      WidgetTester tester,
    ) async {
      repository
        ..active = pageOf<Reservation>(<Reservation>[
          reservationOf(
            't1',
            at: after(const Duration(hours: 1)),
            confirmDeadline: after(const Duration(minutes: 9)),
          ),
        ])
        ..confirmFailure = const ServerFailure(
          code: 'reservation_not_confirmable',
          message: '',
          details: <String, dynamic>{'reason': 'offline'},
          statusCode: 409,
        );
      await open(tester, const ReservationPage(tripId: 't1'));
      await tester.tap(
        find.byKey(const ValueKey<String>('confirm-reservation')),
      );
      await tester.pumpAndSettle();
      expect(find.text('فعّل حالة «متصل» لتأكيد الحجز'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('releasing before the free deadline is free', (
      WidgetTester tester,
    ) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf(
          't1',
          at: after(const Duration(days: 1)),
          freeReleaseUntil: after(const Duration(hours: 20)),
        ),
      ]);
      await open(tester, const ReservationPage(tripId: 't1'));
      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('release-reservation')),
      );
      await tester.tap(
        find.byKey(const ValueKey<String>('release-reservation')),
      );
      await tester.pumpAndSettle();
      expect(find.text('تحرير الحجز؟'), findsOneWidget);
      expect(find.textContaining('التحرير مجاني حتى'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('release-late-warning')),
        findsNothing,
      );

      await tester.tap(find.byKey(const ValueKey<String>('confirm-release')));
      await tester.pumpAndSettle();
      expect(repository.released.single.tripId, 't1');
      // Back on the list of reservations.
      expect(find.text('at:/driver/scheduled'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a late release warns about the reliability points', (
      WidgetTester tester,
    ) async {
      repository
        ..releasePenalty = 3
        ..active = pageOf<Reservation>(<Reservation>[
          reservationOf(
            't1',
            at: after(const Duration(hours: 3)),
            freeReleaseUntil: after(const Duration(hours: -1)),
          ),
        ]);
      await open(tester, const ReservationPage(tripId: 't1'));
      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('release-reservation')),
      );
      await tester.tap(
        find.byKey(const ValueKey<String>('release-reservation')),
      );
      await tester.pumpAndSettle();
      expect(
        find.byKey(const ValueKey<String>('release-late-warning')),
        findsOneWidget,
      );
      expect(find.textContaining('نقاط من موثوقيتك'), findsOneWidget);

      // Keeping the reservation releases nothing.
      await tester.tap(find.text('إلغاء'));
      await tester.pumpAndSettle();
      expect(repository.released, isEmpty);
      await leave(tester);
    });

    testWidgets('an assigned reservation cannot be released', (
      WidgetTester tester,
    ) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf('t1', status: ReservationStatus.assigned),
      ]);
      await open(tester, const ReservationPage(tripId: 't1'));
      expect(
        find.byKey(const ValueKey<String>('release-reservation')),
        findsNothing,
      );
      expect(find.textContaining('مسندة إليك'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a finished reservation is found in the history', (
      WidgetTester tester,
    ) async {
      repository.history = pageOf<Reservation>(<Reservation>[
        reservationOf('old', status: ReservationStatus.released),
      ]);
      await open(tester, const ReservationPage(tripId: 'old'));
      expect(find.textContaining('مُحرَّرة'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a reservation made through a favourite request says so', (
      WidgetTester tester,
    ) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        Reservation(
          id: 'r1',
          tripId: 't1',
          status: ReservationStatus.reserved,
          source: 'favorite',
          scheduledAt: after(const Duration(days: 1)),
        ),
      ]);
      await open(tester, const ReservationPage(tripId: 't1'));
      expect(
        find.byKey(const ValueKey<String>('reservation-favorite')),
        findsOneWidget,
      );
      await leave(tester);
    });

    testWidgets('an unknown reservation says so', (WidgetTester tester) async {
      await open(tester, const ReservationPage(tripId: 'nope'));
      expect(find.text('لم يتم العثور على هذا الحجز'), findsOneWidget);
      // e.g. scheduled.favorite_request: the trip is still in the marketplace.
      await tester.tap(find.byKey(const ValueKey<String>('open-marketplace')));
      await tester.pumpAndSettle();
      expect(find.text('at:/driver/scheduled'), findsOneWidget);
      await leave(tester);
    });
  });

  group('driver overview card', () {
    testWidgets('shows the prompt with a confirm button and links to the '
        'marketplace', (WidgetTester tester) async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf(
          't1',
          at: after(const Duration(hours: 1)),
          confirmDeadline: after(const Duration(minutes: 9)),
        ),
      ]);
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(
        host(
          BlocProvider<ReservationsCubit>(
            create: (_) => ReservationsCubit(
              getReservations: getIt(),
              confirm: getIt(),
              release: getIt(),
              watchIncoming: getIt(),
            )..load(),
            child: const ScheduledOverviewCard(),
          ),
        ),
      );
      await tester.pump();
      await tester.pumpAndSettle();
      expect(find.text('أكّد حجزك'), findsOneWidget);
      expect(find.text('رحلاتي المجدولة'), findsOneWidget);
      expect(find.textContaining('أقرب حجز'), findsOneWidget);

      await tester.tap(
        find.byKey(const ValueKey<String>('confirm-reservation')),
      );
      await tester.pumpAndSettle();
      expect(repository.confirmed, <String>['t1']);

      await tester.tap(find.text('رحلاتي المجدولة'));
      await tester.pumpAndSettle();
      expect(find.text('at:/driver/scheduled'), findsOneWidget);
      await leave(tester);
    });
  });
}
