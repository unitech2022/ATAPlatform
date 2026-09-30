import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_types.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_active_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_driver_location.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:ata_app/l10n/generated/app_localizations_en.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/scheduling_fakes.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  final DateTime at = schedulingNow.add(const Duration(days: 2));

  group('scheduled trips are not the active trip', () {
    late FakeTripRepository repository;

    setUp(() => repository = FakeTripRepository());

    ActiveTripCubit build() => ActiveTripCubit(
      watchActiveTrip: WatchActiveTrip(repository),
      watchDriverLocation: WatchDriverLocation(repository),
      getTrip: GetTrip(repository),
      cancelTrip: CancelTrip(repository),
      ticker: (_) => const Stream<void>.empty(),
      now: () => schedulingNow,
    );

    test('a scheduled trip in the feed does not pin the rider', () async {
      final ActiveTripCubit cubit = build()..start();
      repository.trips.add(scheduledTripAt(at));
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.hasTrip, isFalse);
      expect(cubit.state.trip, isNull);
      await cubit.close();
    });

    test('adopting a scheduled booking is ignored', () async {
      final ActiveTripCubit cubit = build()..adopt(scheduledTripAt(at));
      expect(cubit.state.hasTrip, isFalse);
      await cubit.close();
    });

    test('when the search starts the trip becomes active', () async {
      final ActiveTripCubit cubit = build()..start();
      repository.trips.add(scheduledTripAt(at));
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.hasTrip, isFalse);

      repository.trips.add(scheduledTripAt(at, status: TripStage.searching));
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.hasTrip, isTrue);
      expect(cubit.state.stage, TripStage.searching);
      await cubit.close();
    });
  });

  group('cancellation stage', () {
    test('a waiting booking is in the scheduled stage', () {
      expect(
        CancellationStage.of(scheduledTripAt(at), now: schedulingNow),
        CancellationStage.scheduled,
      );
    });

    test('once the search started the normal stages apply', () {
      expect(
        CancellationStage.of(
          scheduledTripAt(at, status: TripStage.searching),
          now: schedulingNow,
        ),
        CancellationStage.beforeAccept,
      );
      expect(
        CancellationStage.of(
          scheduledTripAt(at, status: TripStage.driverAssigned),
          now: schedulingNow,
        ),
        CancellationStage.afterAccept,
      );
    });
  });

  group('deep links (docs/08 §F13.7)', () {
    const ParseDeepLink parse = ParseDeepLink();

    String? route(String link, {required bool driver}) =>
        parse(DeepLinkParams(link: link, isDriver: driver))?.route;

    test('rider reminders open the booking', () {
      expect(route('ata://scheduled/t1', driver: false), '/scheduled/t1');
      expect(route('ata://scheduled', driver: false), '/scheduled');
    });

    test('driver prompts open the reservation', () {
      expect(
        route('ata://driver/scheduled/t1', driver: true),
        '/driver/scheduled/t1',
      );
      expect(
        route('ata://driver/scheduled', driver: true),
        '/driver/scheduled',
      );
      expect(
        route('ata://driver/airport-queue', driver: true),
        '/driver/airport-queue',
      );
    });

    test('scheduled.reminder / reservation_released reach both roles', () {
      expect(route('ata://scheduled/t1', driver: true), '/driver/scheduled/t1');
      expect(route('ata://scheduled', driver: true), '/driver/scheduled');
    });

    test('a driver link opened by a rider falls back to /home', () {
      expect(route('ata://driver/scheduled/t1', driver: false), '/home');
      expect(route('ata://driver/airport-queue', driver: false), '/home');
    });

    test(
      'links of pushes without data.deepLink are derived from the event',
      () {
        String? link(String code, [String? tripId]) =>
            NotificationTypes.fallbackLink(
              code,
              tripId == null ? null : <String, dynamic>{'tripId': tripId},
            );
        expect(link('scheduled.booked', 't1'), 'ata://scheduled/t1');
        expect(link('scheduled.reminder', 't1'), 'ata://scheduled/t1');
        expect(link('scheduled.driver_reserved', 't1'), 'ata://scheduled/t1');
        expect(link('scheduled.rematched', 't1'), 'ata://scheduled/t1');
        expect(
          link('scheduled.confirm_request', 't1'),
          'ata://driver/scheduled/t1',
        );
        expect(
          link('scheduled.favorite_request', 't1'),
          'ata://driver/scheduled/t1',
        );
        expect(
          link('scheduled.reservation_released'),
          'ata://driver/scheduled',
        );
        expect(NotificationTypes.categoryOf('scheduled.reminder'), 'trips');
      },
    );

    test('every derived link parses to a route', () {
      for (final String code in <String>[
        'scheduled.booked',
        'scheduled.confirm_request',
      ]) {
        final String? link = NotificationTypes.fallbackLink(
          code,
          <String, dynamic>{'tripId': 't1'},
        );
        final DeepLink? target = parse(
          DeepLinkParams(link: link!, isDriver: code.contains('confirm')),
        );
        expect(target?.route, contains('scheduled/t1'));
      }
    });
  });

  group('F17 error texts', () {
    final AppLocalizationsAr ar = AppLocalizationsAr();
    final AppLocalizationsEn en = AppLocalizationsEn();

    setUpAll(() => initializeDateFormatting('ar'));

    String text(
      String code, {
      Map<String, dynamic>? details,
      AppLocalizationsAr? l10n,
    }) => failureText(
      ServerFailure(code: code, message: '', details: details),
      l10n ?? ar,
    );

    test('spec messages (docs/11 §F17.4)', () {
      expect(
        text('schedule_window_exceeded'),
        'لا يمكن الجدولة لأكثر من 7 أيام من الآن',
      );
      expect(
        text('schedule_lead_too_short'),
        'يجب أن يكون الموعد بعد 30 دقيقة على الأقل',
      );
      expect(text('scheduled_limit_reached'), contains('الأقصى'));
      expect(text('reservation_taken'), 'تم حجز هذه الرحلة من كابتن آخر');
      expect(text('reservation_conflict'), contains('يتعارض الموعد'));
      expect(text('reservation_limit_reached'), contains('الأقصى'));
      expect(text('reservation_not_confirmable'), 'لا يمكن التأكيد الآن');
    });

    test('details refine the messages', () {
      expect(
        text(
          'schedule_lead_too_short',
          details: <String, dynamic>{'minScheduledAt': '2026-09-27T10:30:00Z'},
        ),
        startsWith('أقرب موعد متاح'),
      );
      expect(
        text(
          'schedule_window_exceeded',
          details: <String, dynamic>{'maxScheduledAt': '2026-10-04T10:00:00Z'},
        ),
        startsWith('لا يمكن الجدولة بعد'),
      );
      expect(
        text('scheduled_limit_reached', details: <String, dynamic>{'max': 3}),
        contains('(3)'),
      );
      expect(
        text(
          'reservation_not_confirmable',
          details: <String, dynamic>{'reason': 'offline'},
        ),
        contains('متصل'),
      );
      expect(
        text(
          'reservation_not_confirmable',
          details: <String, dynamic>{'reason': 'on_trip'},
        ),
        contains('رحلة جارية'),
      );
      expect(
        text(
          'reservation_not_confirmable',
          details: <String, dynamic>{'reason': 'not_due'},
        ),
        'لم يحن وقت التأكيد بعد',
      );
      expect(
        text(
          'reservation_not_confirmable',
          details: <String, dynamic>{'reason': 'expired'},
        ),
        'انتهت مهلة التأكيد',
      );
    });

    test('airport messages', () {
      expect(
        text('airport_pickup_zone_required'),
        'اختر منطقة الالتقاط في المطار',
      );
      expect(
        text('airport_category_not_applicable'),
        'فئة المطار متاحة لرحلات المطار فقط',
      );
      expect(
        text('not_in_airport_waiting_area'),
        'يجب أن تكون داخل منطقة انتظار المطار',
      );
    });

    test('English texts exist', () {
      expect(
        failureText(
          const ServerFailure(code: 'reservation_taken', message: ''),
          en,
        ),
        isNot(text('reservation_taken')),
      );
    });
  });
}
