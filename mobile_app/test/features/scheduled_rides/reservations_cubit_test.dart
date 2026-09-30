import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/confirm_reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_my_reservations.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/release_reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/scheduling_fakes.dart';
import '../../helpers/trip_fakes.dart';

/// Notifications repository whose foreground pushes are controlled.
class _PushRepository extends FakeNotificationsRepository {
  final StreamController<PushEvent> incoming =
      StreamController<PushEvent>.broadcast();

  @override
  Stream<PushEvent> watchIncoming() => incoming.stream;
}

void main() {
  late FakeScheduledRepository repository;
  late _PushRepository notifications;
  late StreamController<void> ticks;
  late DateTime clock;

  final DateTime pickup = schedulingNow.add(const Duration(hours: 1));

  setUp(() {
    repository = FakeScheduledRepository();
    notifications = _PushRepository();
    ticks = StreamController<void>.broadcast();
    clock = schedulingNow;
  });
  tearDown(() => ticks.close());

  ReservationsCubit build() => ReservationsCubit(
    getReservations: GetMyReservations(repository),
    confirm: ConfirmReservation(repository),
    release: ReleaseReservation(repository),
    watchIncoming: WatchIncomingNotifications(notifications),
    ticker: (Duration _) => ticks.stream,
    now: () => clock,
  );

  Reservation withFirstPrompt() => reservationOf(
    't1',
    at: pickup,
    confirmDeadline: schedulingNow.add(const Duration(minutes: 10)),
    freeReleaseUntil: pickup.subtract(const Duration(hours: 2)),
  );

  test('loads the active reservations', () async {
    repository.active = pageOf<Reservation>(<Reservation>[
      reservationOf('t1'),
      reservationOf('t2'),
    ]);
    final ReservationsCubit cubit = build();
    await cubit.load();
    expect(cubit.state.active, hasLength(2));
    expect(repository.reservationQueries.single.list, ReservationList.active);
    await cubit.close();
  });

  group('confirmation prompts', () {
    test('the first prompt (T-60) counts down and expires', () async {
      repository.active = pageOf<Reservation>(<Reservation>[withFirstPrompt()]);
      final ReservationsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.pending.single.tripId, 't1');
      expect(
        cubit.state.nextPending?.pendingAt(cubit.state.now),
        ConfirmationKind.first,
      );

      clock = schedulingNow.add(const Duration(minutes: 9, seconds: 59));
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.pending, hasLength(1));

      clock = schedulingNow.add(const Duration(minutes: 10));
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.pending, isEmpty);
      await cubit.close();
    });

    test('the final prompt (T-15) is asked once confirmed', () async {
      final Reservation r = reservationOf(
        't1',
        status: ReservationStatus.confirmed,
        at: pickup,
        finalConfirmDeadline: schedulingNow.add(const Duration(minutes: 5)),
      );
      expect(r.pendingAt(schedulingNow), ConfirmationKind.finalStep);
      expect(
        r.deadlineAt(schedulingNow),
        schedulingNow.add(const Duration(minutes: 5)),
      );
      // A final deadline is ignored while the first prompt is still open.
      expect(
        reservationOf(
          't1',
          finalConfirmDeadline: schedulingNow.add(const Duration(minutes: 5)),
        ).pendingAt(schedulingNow),
        isNull,
      );
    });

    test('the most urgent prompt comes first', () async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf(
          'late',
          confirmDeadline: schedulingNow.add(const Duration(minutes: 9)),
        ),
        reservationOf(
          'urgent',
          confirmDeadline: schedulingNow.add(const Duration(minutes: 2)),
        ),
      ]);
      final ReservationsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.nextPending?.tripId, 'urgent');
      await cubit.close();
    });

    test('a foreground scheduled.* push reloads the list', () async {
      final ReservationsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.pending, isEmpty);

      repository.active = pageOf<Reservation>(<Reservation>[withFirstPrompt()]);
      notifications.incoming.add(
        const PushEvent(eventCode: 'scheduled.confirm_request'),
      );
      await Future<void>.delayed(Duration.zero);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.pending, hasLength(1));

      // Other events do not trigger a reload.
      final int loads = repository.reservationQueries.length;
      notifications.incoming.add(const PushEvent(eventCode: 'promo.new'));
      await Future<void>.delayed(Duration.zero);
      expect(repository.reservationQueries.length, loads);
      await cubit.close();
    });
  });

  group('confirm', () {
    test('the first confirmation updates the reservation', () async {
      repository.active = pageOf<Reservation>(<Reservation>[withFirstPrompt()]);
      final ReservationsCubit cubit = build();
      await cubit.load();
      await cubit.confirm('t1');
      expect(repository.confirmed, <String>['t1']);
      expect(cubit.state.active.single.status, ReservationStatus.confirmed);
      expect(cubit.state.event, ReservationEvent.confirmed);
      expect(cubit.state.assignedTrip, isNull);
      expect(cubit.state.isBusy, isFalse);
      await cubit.close();
    });

    test('the final confirmation returns the assigned trip', () async {
      repository.active = pageOf<Reservation>(<Reservation>[
        reservationOf(
          't1',
          status: ReservationStatus.confirmed,
          at: pickup,
          finalConfirmDeadline: schedulingNow.add(const Duration(minutes: 5)),
        ),
      ]);
      repository.confirmResult = ConfirmResult(
        reservation: reservationOf('t1', status: ReservationStatus.assigned),
        trip: tripAt(TripStage.driverAssigned),
      );
      final ReservationsCubit cubit = build();
      await cubit.load();
      await cubit.confirm('t1');
      expect(cubit.state.event, ReservationEvent.finalConfirmed);
      expect(cubit.state.assignedTrip?.status, TripStage.driverAssigned);
      expect(cubit.state.active.single.status, ReservationStatus.assigned);

      cubit.clearNotice();
      expect(cubit.state.event, isNull);
      expect(cubit.state.assignedTrip, isNull);
      await cubit.close();
    });

    test('offline / on-trip refusals are reported', () async {
      repository
        ..active = pageOf<Reservation>(<Reservation>[withFirstPrompt()])
        ..confirmFailure = const ServerFailure(
          code: 'reservation_not_confirmable',
          message: '',
          details: <String, dynamic>{'reason': 'offline'},
        );
      final ReservationsCubit cubit = build();
      await cubit.load();
      await cubit.confirm('t1');
      expect(cubit.state.actionFailure?.code, 'reservation_not_confirmable');
      expect(cubit.state.active.single.status, ReservationStatus.reserved);
      expect(cubit.state.event, isNull);
      await cubit.close();
    });
  });

  group('release', () {
    test('a release removes the reservation and reports the points', () async {
      repository
        ..active = pageOf<Reservation>(<Reservation>[withFirstPrompt()])
        ..releasePenalty = 3;
      final ReservationsCubit cubit = build();
      await cubit.load();
      await cubit.release('t1', reason: 'sick');
      expect(repository.released.single.reason, 'sick');
      expect(cubit.state.active, isEmpty);
      expect(cubit.state.event, ReservationEvent.released);
      expect(cubit.state.releasedPenaltyPoints, 3);
      await cubit.close();
    });

    test('the release is late once the free window is over', () {
      final Reservation r = withFirstPrompt();
      final DateTime free = pickup.subtract(const Duration(hours: 2));
      expect(
        r.isLateReleaseAt(free.subtract(const Duration(minutes: 1))),
        isFalse,
      );
      expect(r.isLateReleaseAt(free), isTrue);
      expect(reservationOf('x').isLateReleaseAt(schedulingNow), isFalse);
    });

    test('only a reserved or confirmed reservation can be released', () {
      expect(reservationOf('x').canRelease, isTrue);
      expect(
        reservationOf('x', status: ReservationStatus.confirmed).canRelease,
        isTrue,
      );
      expect(
        reservationOf('x', status: ReservationStatus.assigned).canRelease,
        isFalse,
      );
    });

    test('a failed release keeps the reservation', () async {
      repository
        ..active = pageOf<Reservation>(<Reservation>[withFirstPrompt()])
        ..releaseFailure = const NetworkFailure(message: 'x');
      final ReservationsCubit cubit = build();
      await cubit.load();
      await cubit.release('t1');
      expect(cubit.state.active, hasLength(1));
      expect(cubit.state.actionFailure, isNotNull);
      await cubit.close();
    });
  });

  test(
    'openTrip falls back to the history for a finished reservation',
    () async {
      repository.history = pageOf<Reservation>(<Reservation>[
        reservationOf('old', status: ReservationStatus.completed),
      ]);
      final ReservationsCubit cubit = build();
      await cubit.openTrip('old');
      expect(cubit.state.find('old')?.status, ReservationStatus.completed);
      expect(
        repository.reservationQueries.map((ReservationsQuery q) => q.list),
        <ReservationList>[ReservationList.active, ReservationList.history],
      );
      await cubit.close();
    },
  );

  test('the history tab loads lazily', () async {
    repository.history = pageOf<Reservation>(<Reservation>[
      reservationOf('old', status: ReservationStatus.released),
    ]);
    final ReservationsCubit cubit = build();
    await cubit.load();
    await cubit.selectList(ReservationList.history);
    expect(cubit.state.current.single.tripId, 'old');
    await cubit.close();
  });
}
