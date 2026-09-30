import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/schedule_picker_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/scheduling_fakes.dart';
import '../../helpers/test_app.dart';

void main() {
  late FakeScheduledRepository repository;
  late ScheduleTimeCubit cubit;

  setUpAll(() => initializeDateFormatting('ar'));

  setUp(() {
    repository = FakeScheduledRepository();
    cubit = ScheduleTimeCubit(
      getRules: GetSchedulingRules(repository),
      now: () => schedulingNow,
    );
  });
  tearDown(() => cubit.close());

  Future<void> openPicker(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(430, 2400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      wrapForTest(
        BlocProvider<ScheduleTimeCubit>.value(
          value: cubit,
          child: Builder(
            builder: (BuildContext context) => TextButton(
              onPressed: () =>
                  SchedulePickerSheet.show(context, rideCategoryId: 'c1'),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();
  }

  String when(DateTime value) => DateText.fullDayAndTime(value, 'ar');

  testWidgets('shows the window, the earliest slot and the fixed-price note', (
    WidgetTester tester,
  ) async {
    await openPicker(tester);
    expect(find.text('متى تريد رحلتك؟'), findsOneWidget);
    // Lead time and window come from the rules (defaults until they load).
    expect(
      find.text('احجز بعد 30 دقيقة على الأقل وحتى 7 أيام من الآن'),
      findsOneWidget,
    );
    expect(repository.rulesCategories, <String?>['c1']);
    // The time that will be booked is shown before confirming.
    expect(find.text('الموعد المختار'), findsOneWidget);
    expect(find.text(when(DateTime(2026, 9, 27, 10, 30))), findsOneWidget);
    expect(find.textContaining('سعر ثابت'), findsOneWidget);
    // One chip per bookable day: today plus seven.
    for (final int day in <int>[27, 28, 29, 30]) {
      expect(find.byKey(ValueKey<String>('sched-day-9-$day')), findsOneWidget);
    }
    expect(
      find.byKey(const ValueKey<String>('sched-day-10-4')),
      findsOneWidget,
    );
    expect(find.byKey(const ValueKey<String>('sched-day-10-5')), findsNothing);
  });

  testWidgets('picking day, hour and minute updates the booked time and '
      'confirming stores it', (WidgetTester tester) async {
    await openPicker(tester);
    await tester.tap(find.byKey(const ValueKey<String>('sched-day-9-29')));
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey<String>('sched-hour-18')));
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey<String>('sched-minute-45')));
    await tester.pump();

    final DateTime picked = DateTime(2026, 9, 29, 18, 45);
    expect(find.text(when(picked)), findsOneWidget);
    // Free cancellation ends an hour before the pickup.
    expect(
      find.textContaining(when(DateTime(2026, 9, 29, 17, 45))),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const ValueKey<String>('schedule-confirm')));
    await tester.pumpAndSettle();
    expect(cubit.state.confirmed, picked);
    expect(find.text('متى تريد رحلتك؟'), findsNothing);
  });

  testWidgets('hours and minutes before the lead time are disabled', (
    WidgetTester tester,
  ) async {
    await openPicker(tester);
    await tester.tap(find.byKey(const ValueKey<String>('sched-hour-9')));
    await tester.pump();
    // Today at 09:xx is in the past: nothing changed.
    expect(cubit.state.hour, 10);
    expect(find.text(when(DateTime(2026, 9, 27, 10, 30))), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey<String>('sched-minute-15')));
    await tester.pump();
    expect(cubit.state.minute, 30);
  });

  testWidgets('the last day stops at now + 7 days: later hours are disabled', (
    WidgetTester tester,
  ) async {
    await openPicker(tester);
    final Finder last = find.byKey(const ValueKey<String>('sched-day-10-4'));
    await tester.ensureVisible(last);
    await tester.pumpAndSettle();
    await tester.tap(last);
    await tester.pumpAndSettle();
    // Snapped to the limit (Oct 4 10:00).
    expect(cubit.state.draft, DateTime(2026, 10, 4, 10));
    await tester.tap(find.byKey(const ValueKey<String>('sched-hour-15')));
    await tester.pump();
    expect(cubit.state.hour, 10);
    expect(cubit.state.canConfirm, isTrue);
  });

  testWidgets('a time outside the window shows why and cannot be confirmed', (
    WidgetTester tester,
  ) async {
    await openPicker(tester);
    cubit.setDateTime(schedulingNow.add(const Duration(minutes: 20)));
    await tester.pumpAndSettle();
    expect(
      find.text(
        'أقرب موعد متاح ${when(schedulingNow.add(const Duration(minutes: 30)))}',
      ),
      findsOneWidget,
    );
    await tester.tap(find.byKey(const ValueKey<String>('schedule-confirm')));
    await tester.pump();
    expect(cubit.state.confirmed, isNull);

    cubit.setDateTime(schedulingNow.add(const Duration(days: 7, minutes: 1)));
    await tester.pumpAndSettle();
    expect(find.textContaining('أبعد موعد متاح'), findsOneWidget);
  });

  testWidgets('a failed rules call is explained and the defaults are used', (
    WidgetTester tester,
  ) async {
    repository.rulesFailure = const NetworkFailure(message: 'x');
    await openPicker(tester);
    expect(find.textContaining('نستخدم القيم الافتراضية'), findsOneWidget);
    expect(cubit.state.canConfirm, isTrue);
  });
}
