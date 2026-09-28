import 'package:ata_app/app/safety_cubits.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_page.dart';
import 'package:ata_app/features/safety/presentation/pages/trusted_contacts_page.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';
import 'package:ata_app/features/trip/presentation/pages/active_trip_page.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:ata_app/features/trip_chat/presentation/pages/trip_chat_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/safety_fakes.dart';
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

  Widget app(Widget child) =>
      trips.provide(child: safety.provide(child: wrapForTest(child)));

  testWidgets('holding SOS on the safety page raises the case', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 1600));
    await tester.pumpWidget(app(const SafetyPage()));
    await tester.pump();
    expect(find.text('جهات موثوقة'), findsOneWidget);
    expect(find.text('بلاغاتي'), findsOneWidget);

    final TestGesture hold = await tester.startGesture(
      tester.getCenter(find.text('SOS')),
    );
    await tester.pump(const Duration(milliseconds: 500));
    expect(find.text('تم إرسال نداء الطوارئ لفريق السلامة'), findsNothing);
    await tester.pump(const Duration(milliseconds: 1600));
    await hold.up();
    await tester.pump(const Duration(seconds: 6));
    await tester.pump();
    expect(find.text('تم إرسال نداء الطوارئ لفريق السلامة'), findsOneWidget);
    expect(find.text('ضغطت بالخطأ'), findsOneWidget);
    expect(find.text('اتصل بالطوارئ 911'), findsWidgets);

    await tester.ensureVisible(find.text('ضغطت بالخطأ'));
    await tester.tap(find.text('ضغطت بالخطأ'));
    await tester.pump();
    expect(find.text('تم إلغاء نداء الطوارئ'), findsOneWidget);
  });

  testWidgets('the rider trip screen shows SOS, chat, share; cancel flow', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 1400));
    final FakeTripRepository repo =
        getIt<TripRepository>() as FakeTripRepository;
    trips.activeTrip.start();
    await tester.pumpWidget(app(const ActiveTripPage()));
    repo.trips.add(tripAt(TripStage.driverEnRoute));
    await tester.pump();
    await tester.pump();

    expect(find.text('SOS'), findsOneWidget);
    expect(find.text('محادثة'), findsOneWidget);
    expect(find.text('مشاركة'), findsOneWidget);

    await tester.ensureVisible(find.text('إلغاء الرحلة'));
    await tester.tap(find.text('إلغاء الرحلة'));
    await tester.pumpAndSettle();
    expect(find.text('غيرت رأيي'), findsOneWidget);
    await tester.tap(find.text('غيرت رأيي'));
    await tester.pumpAndSettle();
    expect(find.text('مجاني'), findsOneWidget);
    await tester.tap(find.text('تأكيد الإلغاء'));
    await tester.pumpAndSettle();
    expect(find.text('تم إلغاء الرحلة'), findsOneWidget);
  });

  testWidgets('the chat page lists messages and sends one', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 932));
    final FakeTripChatRepository repo =
        getIt<TripChatRepository>() as FakeTripChatRepository;
    await safety.chat.bind(
      const ChatTarget(tripId: 't1', actor: TripActor.passenger),
    );
    await tester.pumpWidget(
      app(const TripChatPage(actor: TripActor.passenger)),
    );
    repo.feed.add(const <TripMessage>[
      TripMessage(
        id: 'm1',
        tripId: 't1',
        body: 'وصلت إلى نقطة الالتقاط',
        senderRole: 'driver',
      ),
    ]);
    await tester.pump();
    expect(find.text('وصلت إلى نقطة الالتقاط'), findsOneWidget);
    expect(safety.chat.state.unreadCount, 1);

    await tester.enterText(find.byType(TextField), 'قادم');
    await tester.pump();
    await tester.tap(find.byTooltip('إرسال'));
    await tester.pump();
    expect(find.text('قادم'), findsOneWidget);
  });

  testWidgets('trusted contacts: add a contact through the inline form', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 1400));
    await tester.pumpWidget(app(const TrustedContactsPage()));
    await tester.pump();
    expect(find.text('لم تضف أي جهة موثوقة بعد.'), findsOneWidget);

    await tester.tap(find.text('إضافة جهة موثوقة'));
    await tester.pump();
    await tester.enterText(find.byType(TextFormField).at(0), 'سارة');
    await tester.enterText(find.byType(TextFormField).at(1), '0551234567');
    await tester.tap(find.text('حفظ'));
    await tester.pump();
    expect(find.text('سارة'), findsOneWidget);
    expect(find.text('+966551234567'), findsOneWidget);
  });
}
