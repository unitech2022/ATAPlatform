import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_active_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_driver_location.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';

import '../../helpers/trip_fakes.dart';

void main() {
  late FakeTripRepository repository;
  final DateTime now = DateTime.utc(2026, 9, 28, 12);

  setUp(() => repository = FakeTripRepository());

  ActiveTripCubit build() => ActiveTripCubit(
    watchActiveTrip: WatchActiveTrip(repository),
    watchDriverLocation: WatchDriverLocation(repository),
    getTrip: GetTrip(repository),
    cancelTrip: CancelTrip(repository),
    ticker: (_) => const Stream<void>.empty(),
    now: () => now,
  );

  Future<void> push(Trip? trip) async {
    repository.trips.add(trip);
    await Future<void>.delayed(Duration.zero);
  }

  blocTest<ActiveTripCubit, ActiveTripState>(
    'follows the feed through the whole lifecycle',
    build: build,
    act: (ActiveTripCubit cubit) async {
      cubit.start();
      await push(null);
      await push(tripAt(TripStage.searching));
      await push(tripAt(TripStage.driverAssigned));
      await push(tripAt(TripStage.driverEnRoute));
      await push(tripAt(TripStage.inTrip));
      await push(tripAt(TripStage.completed));
    },
    expect: () => <dynamic>[
      const ActiveTripState(status: ActiveTripStatus.loading),
      const ActiveTripState(status: ActiveTripStatus.watching),
      isA<ActiveTripState>().having(
        (ActiveTripState s) => s.stage,
        'stage',
        TripStage.searching,
      ),
      isA<ActiveTripState>().having(
        (ActiveTripState s) => s.trip?.pin,
        'pin',
        '4821',
      ),
      isA<ActiveTripState>().having(
        (ActiveTripState s) => s.stage,
        'stage',
        TripStage.driverEnRoute,
      ),
      isA<ActiveTripState>().having(
        (ActiveTripState s) => s.stage,
        'stage',
        TripStage.inTrip,
      ),
      isA<ActiveTripState>().having(
        (ActiveTripState s) => s.trip?.fare,
        'final fare',
        41,
      ),
    ],
  );

  blocTest<ActiveTripCubit, ActiveTripState>(
    'a completed trip stays visible until dismissed',
    build: build,
    act: (ActiveTripCubit cubit) async {
      cubit.start();
      await push(tripAt(TripStage.completed));
      await push(null);
      expect(cubit.state.hasTrip, isTrue);
      cubit.dismiss();
    },
    verify: (ActiveTripCubit cubit) {
      expect(cubit.state.hasTrip, isFalse);
      expect(cubit.state.status, ActiveTripStatus.watching);
    },
  );

  blocTest<ActiveTripCubit, ActiveTripState>(
    'when the feed loses an active trip its final state is fetched',
    build: build,
    act: (ActiveTripCubit cubit) async {
      cubit.start();
      await push(tripAt(TripStage.driverEnRoute));
      repository.active = tripAt(TripStage.cancelled);
      await push(null);
    },
    verify: (ActiveTripCubit cubit) =>
        expect(cubit.state.stage, TripStage.cancelled),
  );

  blocTest<ActiveTripCubit, ActiveTripState>(
    'driver location updates the ETA and waiting time counts from arrival',
    build: build,
    act: (ActiveTripCubit cubit) async {
      cubit.start();
      await push(tripAt(TripStage.driverEnRoute));
      repository.locations.add(
        const DriverLocationUpdate(
          tripId: 't1',
          point: GeoPoint.riyadh,
          etaSeconds: 150,
        ),
      );
      repository.locations.add(
        const DriverLocationUpdate(
          tripId: 'other',
          point: GeoPoint.riyadh,
          etaSeconds: 1,
        ),
      );
      await Future<void>.delayed(Duration.zero);
      await push(
        tripAt(
          TripStage.waiting,
          arrivedAt: now.subtract(const Duration(seconds: 75)),
        ),
      );
    },
    verify: (ActiveTripCubit cubit) {
      expect(cubit.state.etaMinutes, 3);
      expect(cubit.state.waitingSeconds, 75);
    },
  );

  blocTest<ActiveTripCubit, ActiveTripState>(
    'cancel applies the cancelled trip and a failure keeps the trip',
    build: build,
    act: (ActiveTripCubit cubit) async {
      cubit.start();
      await push(tripAt(TripStage.driverAssigned));
      await cubit.cancel(CancelReason.driverLate);
    },
    verify: (ActiveTripCubit cubit) {
      expect(cubit.state.stage, TripStage.cancelled);
      expect(cubit.state.cancelling, isFalse);
      expect(cubit.state.canCancel, isFalse);
    },
  );

  test('adopt shows a trip immediately and stop resets', () async {
    final ActiveTripCubit cubit = build()..start();
    cubit.adopt(testTrip);
    expect(cubit.state.hasTrip, isTrue);
    await cubit.stop();
    expect(cubit.state, const ActiveTripState());
    await cubit.close();
    expect(const Right<Failure, Trip>(testTrip).isRight(), isTrue);
  });
}
