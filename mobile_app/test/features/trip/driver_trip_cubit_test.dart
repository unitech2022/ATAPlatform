import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/advance_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/verify_pin.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_active_trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/trip_fakes.dart';

void main() {
  late FakeTripRepository repository;

  setUp(() => repository = FakeTripRepository());

  DriverTripCubit build() => DriverTripCubit(
    watchActiveTrip: WatchActiveTrip(repository),
    advanceTrip: AdvanceTrip(repository),
    verifyPin: VerifyPin(repository),
    cancelTrip: CancelTrip(repository),
  );

  Future<void> push(Trip? trip) async {
    repository.trips.add(trip);
    await Future<void>.delayed(Duration.zero);
  }

  test('nextStep follows the status', () {
    expect(
      DriverTripState(trip: tripAt(TripStage.driverAssigned)).nextStep,
      TripStep.enRoute,
    );
    expect(
      DriverTripState(trip: tripAt(TripStage.driverEnRoute)).nextStep,
      TripStep.arrived,
    );
    expect(DriverTripState(trip: tripAt(TripStage.waiting)).nextStep, isNull);
    expect(DriverTripState(trip: tripAt(TripStage.waiting)).needsPin, isTrue);
    expect(
      DriverTripState(trip: tripAt(TripStage.pinVerified)).nextStep,
      TripStep.start,
    );
    expect(
      DriverTripState(trip: tripAt(TripStage.inTrip)).nextStep,
      TripStep.complete,
    );
    expect(DriverTripState(trip: tripAt(TripStage.inTrip)).canCancel, isFalse);
  });

  blocTest<DriverTripCubit, DriverTripState>(
    'advance walks en-route -> arrived, PIN, start -> complete',
    build: build,
    act: (DriverTripCubit cubit) async {
      cubit.adopt(tripAt(TripStage.driverAssigned));
      await cubit.advance();
      expect(cubit.state.stage, TripStage.driverEnRoute);
      await cubit.advance();
      expect(cubit.state.stage, TripStage.waiting);
      '4821'.split('').forEach(cubit.addPinDigit);
      await cubit.submitPin();
      expect(cubit.state.stage, TripStage.pinVerified);
      await cubit.advance();
      expect(cubit.state.stage, TripStage.inTrip);
      await cubit.advance(at: GeoPoint.riyadh);
      expect(cubit.state.stage, TripStage.completed);
      expect(cubit.state.busy, isFalse);
      expect(cubit.state.nextStep, isNull);
      cubit.dismiss();
    },
    verify: (DriverTripCubit cubit) => expect(cubit.state.hasTrip, isFalse),
  );

  blocTest<DriverTripCubit, DriverTripState>(
    'a wrong PIN clears the boxes and keeps attemptsLeft',
    build: build,
    act: (DriverTripCubit cubit) async {
      cubit.adopt(tripAt(TripStage.waiting));
      '99999'.split('').forEach(cubit.addPinDigit);
      expect(cubit.state.pin, '9999');
      await cubit.submitPin();
      expect(cubit.state.pin, isEmpty);
      expect(cubit.state.stage, TripStage.waiting);
      expect(cubit.state.failure?.code, 'pin_invalid');
      expect(cubit.state.failure?.intDetail('attemptsLeft'), 4);
      cubit
        ..deletePinDigit()
        ..addPinDigit('1');
    },
    verify: (DriverTripCubit cubit) {
      expect(cubit.state.failure, isNull);
      expect(cubit.state.pin, '1');
    },
  );

  blocTest<DriverTripCubit, DriverTripState>(
    'the feed restores and clears the trip',
    build: build,
    act: (DriverTripCubit cubit) async {
      cubit.start();
      await push(tripAt(TripStage.driverEnRoute));
      await push(null);
    },
    expect: () => <dynamic>[
      const DriverTripState(status: DriverTripStatus.loading),
      isA<DriverTripState>().having(
        (DriverTripState s) => s.stage,
        'stage',
        TripStage.driverEnRoute,
      ),
      const DriverTripState(status: DriverTripStatus.watching),
    ],
  );

  blocTest<DriverTripCubit, DriverTripState>(
    'cancel with a reason applies the cancelled trip',
    build: build,
    act: (DriverTripCubit cubit) async {
      cubit.adopt(tripAt(TripStage.driverEnRoute));
      await cubit.cancel('wrong_pickup');
    },
    verify: (DriverTripCubit cubit) {
      expect(cubit.state.stage, TripStage.cancelled);
      expect(cubit.state.canCancel, isFalse);
    },
  );
}
