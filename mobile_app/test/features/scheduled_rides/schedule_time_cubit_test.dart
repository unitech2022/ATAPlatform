import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_state.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/scheduling_fakes.dart';

void main() {
  late FakeScheduledRepository repository;
  late DateTime clock;

  setUp(() {
    repository = FakeScheduledRepository();
    clock = schedulingNow;
  });

  ScheduleTimeCubit build() => ScheduleTimeCubit(
    getRules: GetSchedulingRules(repository),
    now: () => clock,
  );

  const Duration minute = Duration(minutes: 1);

  group('window and lead time (injectable clock)', () {
    test('bounds are now + min lead and now + max days × 24 h', () {
      final ScheduleTimeState state = build().state;
      expect(state.minAt, schedulingNow.add(const Duration(minutes: 30)));
      expect(state.maxAt, schedulingNow.add(const Duration(days: 7)));
    });

    test('the lead time is inclusive: exactly now + 30 min is bookable', () {
      final ScheduleTimeCubit cubit = build();
      cubit.setDateTime(schedulingNow.add(const Duration(minutes: 30)));
      expect(cubit.state.draftIssue, isNull);
      expect(cubit.state.canConfirm, isTrue);

      cubit.setDateTime(schedulingNow.add(const Duration(minutes: 29)));
      expect(cubit.state.draftIssue, ScheduleIssue.tooSoon);
      expect(cubit.state.canConfirm, isFalse);

      cubit.setDateTime(schedulingNow.add(const Duration(minutes: 20)));
      expect(cubit.state.draftIssue, ScheduleIssue.tooSoon);
    });

    test('the window is inclusive: now + 7 days ok, one minute more not', () {
      final ScheduleTimeCubit cubit = build();
      cubit.setDateTime(schedulingNow.add(const Duration(days: 7)));
      expect(cubit.state.draftIssue, isNull);

      cubit.setDateTime(schedulingNow.add(const Duration(days: 7)).add(minute));
      expect(cubit.state.draftIssue, ScheduleIssue.tooFar);
      expect(cubit.state.canConfirm, isFalse);
    });

    test('the window is measured from the booking moment, not the day', () {
      // Booked at 10:00: 7 days later at 23:00 is beyond the window even
      // though it is still "the seventh day".
      final ScheduleTimeCubit cubit = build();
      cubit.setDateTime(DateTime(2026, 10, 4, 23));
      expect(cubit.state.draftIssue, ScheduleIssue.tooFar);
      clock = DateTime(2026, 9, 27, 23, 30);
      cubit.open();
      expect(cubit.state.issueFor(DateTime(2026, 10, 4, 23)), isNull);
    });

    test('rules from the API replace the defaults', () async {
      repository.rules = const SchedulingRules(
        maxDaysAhead: 3,
        minLeadMinutes: 60,
      );
      final ScheduleTimeCubit cubit = build();
      await cubit.loadRules(rideCategoryId: 'c1');
      expect(cubit.state.rulesLoaded, isTrue);
      expect(repository.rulesCategories, <String?>['c1']);
      expect(cubit.state.minAt, schedulingNow.add(const Duration(hours: 1)));
      expect(cubit.state.maxAt, schedulingNow.add(const Duration(days: 3)));
      expect(cubit.state.days.length, 4);
    });

    test('a failing rules call keeps the defaults and reports it', () async {
      repository.rulesFailure = const NetworkFailure(message: 'x');
      final ScheduleTimeCubit cubit = build();
      await cubit.loadRules();
      expect(cubit.state.rulesFailure, isNotNull);
      expect(cubit.state.rulesLoaded, isFalse);
      expect(cubit.state.rules, SchedulingRules.fallback);
    });
  });

  group('picker', () {
    test('open starts at the earliest slot and lists every bookable day', () {
      final ScheduleTimeCubit cubit = build()..open();
      // 10:30 is the first 5-minute slot at or after now + 30 min.
      expect(cubit.state.draft, DateTime(2026, 9, 27, 10, 30));
      expect(cubit.state.days.first, DateTime(2026, 9, 27));
      expect(cubit.state.days.last, DateTime(2026, 10, 4));
      expect(cubit.state.days.length, 8);
      expect(cubit.state.canConfirm, isTrue);
    });

    test('the earliest slot is rounded up to the next 5 minutes', () {
      clock = DateTime(2026, 9, 27, 10, 7, 30);
      final ScheduleTimeCubit cubit = build()..open();
      // 10:37:30 -> 10:40.
      expect(cubit.state.draft, DateTime(2026, 9, 27, 10, 40));
    });

    test('day, hour and minute build the draft', () {
      final ScheduleTimeCubit cubit = build()
        ..open()
        ..selectDay(DateTime(2026, 9, 29))
        ..selectHour(18)
        ..selectMinute(45);
      expect(cubit.state.draft, DateTime(2026, 9, 29, 18, 45));
      expect(cubit.state.canConfirm, isTrue);
    });

    test(
      'picking a day snaps a time that does not fit to the nearest slot',
      () {
        final ScheduleTimeCubit cubit = build()
          ..open()
          ..selectDay(DateTime(2026, 9, 28))
          ..selectHour(23)
          ..selectMinute(55)
          ..selectDay(DateTime(2026, 10, 4));
        // Oct 4 23:55 is past now + 7 days (10:00): snapped to the limit.
        expect(cubit.state.draft, DateTime(2026, 10, 4, 10));
        expect(cubit.state.draftIssue, isNull);

        cubit.selectDay(DateTime(2026, 9, 27));
        // Sep 27 10:00 is before now + 30 min: snapped to 10:30.
        expect(cubit.state.draft, DateTime(2026, 9, 27, 10, 30));
      },
    );

    test('hours and minutes outside the window are not available', () {
      final ScheduleTimeCubit cubit = build()..open();
      final ScheduleTimeState state = cubit.state;
      final DateTime today = DateTime(2026, 9, 27);
      expect(state.isHourAvailable(today, 9), isFalse);
      expect(state.isHourAvailable(today, 10), isTrue);
      expect(state.isAvailable(today, hour: 10, minute: 25), isFalse);
      expect(state.isAvailable(today, hour: 10, minute: 30), isTrue);
      final DateTime last = DateTime(2026, 10, 4);
      expect(state.isAvailable(last, hour: 10, minute: 0), isTrue);
      expect(state.isAvailable(last, hour: 10, minute: 5), isFalse);
      expect(state.isHourAvailable(last, 11), isFalse);
    });
  });

  group('confirmation', () {
    test('confirm keeps a valid time and rejects an invalid one', () {
      final ScheduleTimeCubit cubit = build()..open();
      cubit.setDateTime(schedulingNow.add(const Duration(minutes: 10)));
      expect(cubit.confirm(), isFalse);
      expect(cubit.state.confirmed, isNull);

      cubit.setDateTime(DateTime(2026, 9, 28, 9));
      expect(cubit.confirm(), isTrue);
      expect(cubit.state.confirmed, DateTime(2026, 9, 28, 9));
      expect(cubit.state.hasConfirmed, isTrue);
    });

    test('reopening starts from the confirmed time', () {
      final ScheduleTimeCubit cubit = build()
        ..open()
        ..setDateTime(DateTime(2026, 9, 29, 8, 15))
        ..confirm()
        ..open();
      expect(cubit.state.draft, DateTime(2026, 9, 29, 8, 15));
    });

    test('recheck fails once the clock moved past the window', () {
      final ScheduleTimeCubit cubit = build()
        ..open()
        ..setDateTime(DateTime(2026, 9, 27, 10, 35))
        ..confirm();
      expect(cubit.recheck(), isTrue);

      // Ten minutes later 10:35 is only 25 minutes away.
      clock = DateTime(2026, 9, 27, 10, 10);
      expect(cubit.recheck(), isFalse);
      expect(cubit.state.confirmedIssue, ScheduleIssue.tooSoon);
    });

    test('clear goes back to "now"', () {
      final ScheduleTimeCubit cubit = build()
        ..open()
        ..confirm()
        ..clear();
      expect(cubit.state.confirmed, isNull);
      expect(cubit.state.draft, isNull);
      expect(cubit.recheck(), isFalse);
    });
  });
}
