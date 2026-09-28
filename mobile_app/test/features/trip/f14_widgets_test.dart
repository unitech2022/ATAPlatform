import 'package:ata_app/app/safety_cubits.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:ata_app/features/trip/presentation/pages/driver_trip_page.dart';
import 'package:ata_app/features/trip/presentation/widgets/reliability_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  late TripCubits trips;
  late SafetyCubits safety;

  setUp(() async {
    await registerTestDependencies();
    trips = TripCubits.fromInjector();
    safety = SafetyCubits.fromInjector();
  });

  tearDown(() async {
    await safety.close();
    await trips.close();
    await getIt.reset();
  });

  testWidgets('driver waiting screen: no-show after the wait, confirm', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 1600));
    final FakeTripRepository repo =
        getIt<TripRepository>() as FakeTripRepository;
    trips.driverTrip.start();
    await tester.pumpWidget(
      trips.provide(
        child: safety.provide(child: wrapForTest(const DriverTripPage())),
      ),
    );
    repo.trips.add(
      tripAt(
        TripStage.waiting,
        arrivedAt: DateTime.now().subtract(const Duration(minutes: 6)),
      ),
    );
    await tester.pump();
    await tester.pump();

    expect(find.text('SOS'), findsOneWidget);
    await tester.ensureVisible(find.text('لم يحضر الراكب'));
    await tester.tap(find.text('لم يحضر الراكب'));
    await tester.pumpAndSettle();
    expect(find.text('تأكيد عدم حضور الراكب؟'), findsOneWidget);
    await tester.tap(find.text('لم يحضر الراكب').last);
    await tester.pumpAndSettle();
    expect(find.text('تم إلغاء الرحلة'), findsOneWidget);
  });

  testWidgets('the reliability card shows the rate, points and level', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      wrapForTest(
        BlocProvider<ReliabilityCubit>(
          create: (_) => ReliabilityCubit(
            getReliability: getIt(),
            role: TripActor.passenger,
          )..load(),
          child: const SingleChildScrollView(child: ReliabilityCard()),
        ),
      ),
    );
    await tester.pump();
    expect(find.text('موثوقيتك'), findsOneWidget);
    expect(find.text('18%'), findsOneWidget);
    expect(find.text('6'), findsOneWidget);
    expect(find.text('ممتاز'), findsOneWidget);
  });
}
