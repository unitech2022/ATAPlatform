import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduled_trips.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_detail_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/scheduled_trips_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/usecases/get_trip.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/scheduling_fakes.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  final DateTime soon = schedulingNow.add(const Duration(days: 1));
  final DateTime later = schedulingNow.add(const Duration(days: 3));

  group('ScheduledTripsCubit', () {
    late FakeScheduledRepository repository;
    late StreamController<void> ticks;

    setUp(() {
      repository = FakeScheduledRepository();
      ticks = StreamController<void>.broadcast();
    });
    tearDown(() => ticks.close());

    ScheduledTripsCubit build() => ScheduledTripsCubit(
      getScheduled: GetScheduledTrips(repository),
      ticker: (Duration _) => ticks.stream,
    );

    test('loads the bookings soonest first', () async {
      repository.scheduled = <ScheduledTrip>[
        ScheduledTrip(scheduledTripAt(later, id: 'b')),
        ScheduledTrip(scheduledTripAt(soon, id: 'a')),
      ];
      final ScheduledTripsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.loaded, isTrue);
      expect(cubit.state.trips.map((ScheduledTrip t) => t.id), <String>[
        'a',
        'b',
      ]);
      await cubit.close();
    });

    test('an empty list is the empty state', () async {
      final ScheduledTripsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.isEmpty, isTrue);
      await cubit.close();
    });

    test('a failure is reported; a silent refresh keeps the list', () async {
      repository.scheduled = <ScheduledTrip>[
        ScheduledTrip(scheduledTripAt(soon)),
      ];
      final ScheduledTripsCubit cubit = build();
      await cubit.load();
      repository.scheduledFailure = const NetworkFailure(message: 'x');
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.trips, hasLength(1));
      expect(cubit.state.failure, isNull);

      await cubit.load();
      expect(cubit.state.failure, isNotNull);
      await cubit.close();
    });

    test('refreshes on the ticker so a reserved driver shows up', () async {
      repository.scheduled = <ScheduledTrip>[
        ScheduledTrip(scheduledTripAt(soon)),
      ];
      final ScheduledTripsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.trips.single.phase, ScheduledPhase.waitingForDriver);

      repository.scheduled = <ScheduledTrip>[
        ScheduledTrip(
          scheduledTripAt(soon, reservation: ReservationStatus.confirmed),
        ),
      ];
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.trips.single.phase, ScheduledPhase.driverConfirmed);
      await cubit.close();
    });

    test('remove drops a cancelled booking', () async {
      repository.scheduled = <ScheduledTrip>[
        ScheduledTrip(scheduledTripAt(soon, id: 'a')),
        ScheduledTrip(scheduledTripAt(later, id: 'b')),
      ];
      final ScheduledTripsCubit cubit = build();
      await cubit.load();
      cubit.remove('a');
      expect(cubit.state.trips.single.id, 'b');
      await cubit.close();
    });
  });

  group('ScheduledTrip phases', () {
    test('status and reservation give the rider-facing phase', () {
      expect(
        ScheduledTrip(scheduledTripAt(soon)).phase,
        ScheduledPhase.waitingForDriver,
      );
      expect(
        ScheduledTrip(
          scheduledTripAt(soon, reservation: ReservationStatus.reserved),
        ).phase,
        ScheduledPhase.driverReserved,
      );
      expect(
        ScheduledTrip(
          scheduledTripAt(soon, reservation: ReservationStatus.confirmed),
        ).phase,
        ScheduledPhase.driverConfirmed,
      );
      // A released reservation is the same as none.
      expect(
        ScheduledTrip(
          scheduledTripAt(soon, reservation: ReservationStatus.released),
        ).phase,
        ScheduledPhase.waitingForDriver,
      );
      expect(
        ScheduledTrip(scheduledTripAt(soon, status: TripStage.searching)).phase,
        ScheduledPhase.searching,
      );
      expect(
        ScheduledTrip(
          scheduledTripAt(soon, status: TripStage.driverAssigned),
        ).phase,
        ScheduledPhase.inProgress,
      );
      expect(
        ScheduledTrip(scheduledTripAt(soon, status: TripStage.cancelled)).phase,
        ScheduledPhase.ended,
      );
    });

    test('the free-cancel window ends at the API deadline', () {
      final ScheduledTrip trip = ScheduledTrip(scheduledTripAt(soon));
      final DateTime deadline = soon.subtract(const Duration(minutes: 60));
      expect(trip.freeCancelUntil, deadline);
      expect(
        trip.isFreeCancelAt(deadline.subtract(const Duration(seconds: 1))),
        isTrue,
      );
      expect(trip.isFreeCancelAt(deadline), isFalse);
    });

    test('untilStart never goes negative', () {
      final ScheduledTrip trip = ScheduledTrip(scheduledTripAt(soon));
      expect(trip.untilStart(schedulingNow), const Duration(days: 1));
      expect(
        trip.untilStart(soon.add(const Duration(hours: 1))),
        Duration.zero,
      );
    });

    test('a scheduled trip is not "hasDriver"', () {
      expect(TripStage.scheduled.hasDriver, isFalse);
      expect(TripStage.scheduled.isSearching, isFalse);
      expect(TripStage.scheduled.isTerminal, isFalse);
      expect(TripStage.scheduled.canCancel, isTrue);
    });
  });

  group('ScheduledDetailCubit', () {
    late FakeTripRepository trips;
    late StreamController<void> ticks;
    late DateTime clock;

    setUp(() {
      trips = FakeTripRepository()..active = scheduledTripAt(soon);
      ticks = StreamController<void>.broadcast();
      clock = schedulingNow;
    });
    tearDown(() => ticks.close());

    ScheduledDetailCubit build() => ScheduledDetailCubit(
      tripId: 't1',
      getTrip: GetTrip(trips),
      ticker: (Duration _) => ticks.stream,
      now: () => clock,
    );

    test('loads the booking with a countdown that follows the clock', () async {
      final ScheduledDetailCubit cubit = build();
      await cubit.load();
      expect(cubit.state.trip?.phase, ScheduledPhase.waitingForDriver);
      expect(cubit.state.remaining, const Duration(days: 1));
      expect(cubit.state.freeCancel, isTrue);

      clock = soon.subtract(const Duration(minutes: 59));
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.remaining, const Duration(minutes: 59));
      // 59 minutes before the pickup the free window (60 min) is over.
      expect(cubit.state.freeCancel, isFalse);
      await cubit.close();
    });

    test('re-reads the trip every 30 ticks', () async {
      final ScheduledDetailCubit cubit = build();
      await cubit.load();
      trips.active = scheduledTripAt(
        soon,
        reservation: ReservationStatus.reserved,
      );
      for (int i = 0; i < ScheduledDetailCubit.refreshEvery; i++) {
        ticks.add(null);
        await Future<void>.delayed(Duration.zero);
      }
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.trip?.phase, ScheduledPhase.driverReserved);
      await cubit.close();
    });

    test(
      'adopt shows the cancelled trip returned by the cancel flow',
      () async {
        final ScheduledDetailCubit cubit = build();
        await cubit.load();
        cubit.adopt(scheduledTripAt(soon, status: TripStage.cancelled));
        expect(cubit.state.trip?.phase, ScheduledPhase.ended);
        await cubit.close();
      },
    );
  });
}
