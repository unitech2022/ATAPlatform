import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_cubit.dart';
import 'package:ata_app/features/airport/presentation/pages/airport_queue_page.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_queue_card.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_row.dart';
import 'package:ata_app/features/airport/presentation/widgets/trip_airport_info.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/routed_app.dart';
import '../../helpers/scheduling_fakes.dart';
import '../../helpers/test_app.dart';

void main() {
  late FakeAirportRepository repository;

  setUp(() async {
    await registerTestDependencies();
    repository = getIt<AirportRepository>() as FakeAirportRepository;
  });
  tearDown(getIt.reset);

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  group('airport picker on the request sheet', () {
    late AirportPickupCubit cubit;

    setUp(
      () => cubit = AirportPickupCubit(getAirports: getIt(), resolve: getIt()),
    );
    tearDown(() => cubit.close());

    Future<void> openRow(WidgetTester tester) async {
      await tester.binding.setSurfaceSize(const Size(430, 2600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(
        wrapForTest(
          BlocProvider<AirportPickupCubit>.value(
            value: cubit,
            child: const AirportRow(),
          ),
        ),
      );
    }

    Future<void> openSheet(WidgetTester tester) async {
      await tester.tap(find.byKey(const ValueKey<String>('airport-row')));
      await tester.pumpAndSettle();
    }

    testWidgets('an airport pickup needs its zone; the zone shows its '
        'instructions and the waiting policy', (WidgetTester tester) async {
      await openRow(tester);
      expect(find.text('رحلة من أو إلى مطار؟'), findsOneWidget);
      await openSheet(tester);

      expect(find.text('مطار الملك خالد الدولي (RUH)'), findsOneWidget);
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-RUH-pickup')),
      );
      await tester.pumpAndSettle();

      // Zones grouped by terminal.
      expect(find.text('صالة T1'), findsOneWidget);
      expect(find.byKey(const ValueKey<String>('zone-z1')), findsOneWidget);
      expect(find.text('عند البوابة 3'), findsNothing);
      // The waiting policy of the airport default before a zone is picked.
      expect(
        find.textContaining('تحصل على 15 دقيقة انتظار مجاناً'),
        findsOneWidget,
      );

      await tester.tap(find.byKey(const ValueKey<String>('zone-z1')));
      await tester.pumpAndSettle();
      expect(find.text('عند البوابة 3'), findsOneWidget);
      // The zone's own policy replaces the default.
      expect(
        find.textContaining('تحصل على 20 دقيقة انتظار مجاناً'),
        findsOneWidget,
      );
      expect(cubit.state.selection?.isComplete, isTrue);
    });

    testWidgets('the flight number is optional, normalized and validated', (
      WidgetTester tester,
    ) async {
      await openRow(tester);
      await openSheet(tester);
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-RUH-pickup')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey<String>('zone-z1')));
      await tester.pumpAndSettle();

      final Finder field = find.byKey(const ValueKey<String>('airport-flight'));
      await tester.ensureVisible(field);
      await tester.enterText(field, 'sv 1020');
      await tester.pump();
      expect(cubit.state.selection?.flightNumber, 'SV1020');
      expect(find.text('رقم الرحلة غير صحيح، مثال: SV1020'), findsNothing);

      await tester.enterText(field, 'nope');
      await tester.pump();
      expect(find.text('رقم الرحلة غير صحيح، مثال: SV1020'), findsOneWidget);
      expect(cubit.state.selection?.isComplete, isFalse);

      await tester.enterText(field, '');
      await tester.pump();
      expect(cubit.state.selection?.isComplete, isTrue);
    });

    testWidgets('the row summarises the choice and can be removed', (
      WidgetTester tester,
    ) async {
      await openRow(tester);
      await openSheet(tester);
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-RUH-pickup')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey<String>('zone-z1')));
      await tester.pumpAndSettle();
      final Finder field = find.byKey(const ValueKey<String>('airport-flight'));
      await tester.ensureVisible(field);
      await tester.enterText(field, 'SV1020');
      await tester.pump();
      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('airport-done')),
      );
      await tester.tap(find.byKey(const ValueKey<String>('airport-done')));
      await tester.pumpAndSettle();

      expect(find.text('مطار الملك خالد الدولي · من المطار'), findsOneWidget);
      expect(
        find.text('منطقة الالتقاط 1 - صالة 1 · رحلة SV1020'),
        findsOneWidget,
      );

      await tester.tap(find.byKey(const ValueKey<String>('airport-remove')));
      await tester.pumpAndSettle();
      expect(find.text('رحلة من أو إلى مطار؟'), findsOneWidget);
      expect(cubit.state.selection, isNull);
    });

    testWidgets('a pickup without its zone is flagged on the row', (
      WidgetTester tester,
    ) async {
      await openRow(tester);
      await openSheet(tester);
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-RUH-pickup')),
      );
      await tester.pumpAndSettle();
      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('airport-done')),
      );
      await tester.tap(find.byKey(const ValueKey<String>('airport-done')));
      await tester.pumpAndSettle();
      expect(find.text('اختر منطقة الالتقاط في المطار'), findsOneWidget);
    });

    testWidgets('a dropoff picks an optional terminal', (
      WidgetTester tester,
    ) async {
      await openRow(tester);
      await openSheet(tester);
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-RUH-dropoff')),
      );
      await tester.pumpAndSettle();
      expect(find.text('الصالة (اختياري)'), findsOneWidget);
      expect(cubit.state.selection?.isComplete, isTrue);

      await tester.tap(find.byKey(const ValueKey<String>('terminal-T1')));
      await tester.pumpAndSettle();
      expect(cubit.state.selection?.dropoffTerminal, 'T1');

      // Switching to a pickup keeps the airport and asks for a zone.
      await tester.tap(find.byKey(const ValueKey<String>('direction-pickup')));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey<String>('zone-z1')), findsOneWidget);
      expect(cubit.state.needsZone, isTrue);
    });

    testWidgets('a catalog failure can be retried', (
      WidgetTester tester,
    ) async {
      repository.airportsFailure = const NetworkFailure(message: 'x');
      await openRow(tester);
      await openSheet(tester);
      expect(find.text('إعادة المحاولة'), findsOneWidget);
      repository.airportsFailure = null;
      await tester.tap(find.text('إعادة المحاولة'));
      await tester.pumpAndSettle();
      expect(find.text('مطار الملك خالد الدولي (RUH)'), findsOneWidget);
    });
  });

  testWidgets('TripAirportInfo shows the airport, zone, flight and waiting', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      wrapForTest(
        const TripAirportInfo(
          airport: TripAirport(
            code: 'RUH',
            direction: AirportDirection.pickup,
            zoneName: 'منطقة الالتقاط 3',
            terminalCode: 'T1',
            flightNumber: 'SV1020',
            freeWaitingMinutes: 15,
          ),
        ),
      ),
    );
    expect(find.text('استلام من مطار RUH'), findsOneWidget);
    expect(
      find.text('منطقة الالتقاط 3 · صالة T1 · رحلة SV1020 · انتظار مجاني 15 د'),
      findsOneWidget,
    );

    await tester.pumpWidget(
      wrapForTest(
        const TripAirportInfo(
          airport: TripAirport(
            code: 'RUH',
            direction: AirportDirection.dropoff,
          ),
        ),
      ),
    );
    expect(find.text('توصيل إلى مطار RUH'), findsOneWidget);
  });

  group('driver airport queue', () {
    Widget host(Widget child) => BlocProvider<LocationStreamCubit>(
      create: (_) => LocationStreamCubit(
        requestAccess: getIt(),
        watchPosition: getIt(),
        sendLocation: getIt(),
      ),
      child: wrapRouted(child),
    );

    Future<void> open(WidgetTester tester, Widget page) async {
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(host(page));
      await tester.pump();
      await tester.pumpAndSettle();
    }

    testWidgets('shows the position, the wait and lets the driver leave', (
      WidgetTester tester,
    ) async {
      repository.queue = const AirportQueueStatus(
        inQueue: true,
        airport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار الملك خالد'),
        position: 7,
        total: 23,
        estimatedWaitMinutes: 25,
      );
      await open(tester, const AirportQueuePage());
      expect(find.text('طابور المطار'), findsOneWidget);
      expect(find.text('ترتيبك 7 من 23'), findsOneWidget);
      expect(find.text('الانتظار المتوقع نحو 25 دقيقة'), findsOneWidget);

      // A live update moves the driver up.
      repository.updates.add(
        const AirportQueuePosition(
          position: 2,
          total: 20,
          estimatedWaitMinutes: 8,
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('ترتيبك 2 من 20'), findsOneWidget);

      await tester.tap(
        find.byKey(const ValueKey<String>('airport-queue-leave')),
      );
      await tester.pumpAndSettle();
      expect(repository.leaves, 1);
      expect(
        find.byKey(const ValueKey<String>('airport-queue-position')),
        findsNothing,
      );
      expect(
        find.byKey(const ValueKey<String>('airport-queue-not-in')),
        findsOneWidget,
      );
      await leave(tester);
    });

    testWidgets('a driver in the waiting area can join with their position', (
      WidgetTester tester,
    ) async {
      repository.queue = const AirportQueueStatus(
        eligibleAirport: AirportRef(
          id: 'a1',
          code: 'RUH',
          name: 'مطار الملك خالد',
        ),
      );
      await open(tester, const AirportQueuePage());
      expect(
        find.text('أنت داخل منطقة انتظار مطار الملك خالد'),
        findsOneWidget,
      );
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-queue-join')),
      );
      await tester.pumpAndSettle();
      expect(repository.joined, hasLength(1));
      expect(find.text('ترتيبك 7 من 23'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('joining outside the waiting area shows the reason', (
      WidgetTester tester,
    ) async {
      repository
        ..queue = const AirportQueueStatus(
          eligibleAirport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار'),
        )
        ..joinFailure = const ServerFailure(
          code: 'not_in_airport_waiting_area',
          message: '',
          statusCode: 422,
        );
      await open(tester, const AirportQueuePage());
      await tester.tap(
        find.byKey(const ValueKey<String>('airport-queue-join')),
      );
      await tester.pumpAndSettle();
      expect(find.text('يجب أن تكون داخل منطقة انتظار المطار'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a driver away from any airport sees nothing to join', (
      WidgetTester tester,
    ) async {
      await open(tester, const AirportQueuePage());
      expect(find.text('أنت لست في منطقة انتظار مطار حالياً'), findsOneWidget);
      expect(
        find.byKey(const ValueKey<String>('airport-queue-join')),
        findsNothing,
      );
      await leave(tester);
    });

    testWidgets('the overview card appears only near an airport', (
      WidgetTester tester,
    ) async {
      Future<void> pumpCard(AirportQueueStatus status) async {
        repository.queue = status;
        await tester.pumpWidget(
          host(
            BlocProvider<AirportQueueCubit>(
              create: (_) => AirportQueueCubit(
                getQueue: getIt(),
                join: getIt(),
                leave: getIt(),
                watch: getIt(),
              )..start(),
              child: const AirportQueueCard(),
            ),
          ),
        );
        await tester.pump();
        await tester.pumpAndSettle();
      }

      await pumpCard(const AirportQueueStatus());
      expect(
        find.byKey(const ValueKey<String>('airport-queue-card')),
        findsNothing,
      );

      await pumpCard(
        const AirportQueueStatus(
          eligibleAirport: AirportRef(
            id: 'a1',
            code: 'RUH',
            name: 'مطار الملك خالد',
          ),
        ),
      );
      expect(
        find.text('أنت داخل منطقة انتظار مطار الملك خالد'),
        findsOneWidget,
      );

      await pumpCard(
        const AirportQueueStatus(
          inQueue: true,
          airport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار الملك خالد'),
          position: 4,
          total: 9,
        ),
      );
      expect(find.text('ترتيبك 4 من 9'), findsOneWidget);
      await leave(tester);
    });
  });
}
