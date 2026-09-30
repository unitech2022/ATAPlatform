import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/airport/domain/entities/flight_number.dart';
import 'package:ata_app/features/airport/domain/usecases/get_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/get_airports.dart';
import 'package:ata_app/features/airport/domain/usecases/join_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/leave_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/resolve_airport.dart';
import 'package:ata_app/features/airport/domain/usecases/watch_airport_queue.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_state.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';

import '../../helpers/scheduling_fakes.dart';

void main() {
  late FakeAirportRepository repository;

  setUp(() => repository = FakeAirportRepository());

  group('FlightNumber', () {
    test('normalizes to uppercase without spaces', () {
      expect(FlightNumber.normalize(' sv 1020 '), 'SV1020');
      expect(FlightNumber.normalize('   '), isNull);
    });

    test('accepts the documented pattern only', () {
      for (final String ok in <String>['SV1020', 'EK1', 'XY9999', 'BA123A']) {
        expect(FlightNumber.isValid(ok), isTrue, reason: ok);
      }
      expect(FlightNumber.isValid(null), isTrue);
      for (final String bad in <String>[
        'S',
        'SV',
        'SV12345',
        'SV10-20',
        'S1',
      ]) {
        expect(FlightNumber.isValid(bad), isFalse, reason: bad);
      }
    });
  });

  group('AirportPickupCubit', () {
    AirportPickupCubit build() => AirportPickupCubit(
      getAirports: GetAirports(repository),
      resolve: ResolveAirport(repository),
    );

    test('loads the airports catalog', () async {
      final AirportPickupCubit cubit = build();
      await cubit.loadAirports();
      expect(cubit.state.status, AirportCatalogStatus.ready);
      expect(cubit.state.airports.single.code, 'RUH');
    });

    test('a catalog failure is reported', () async {
      repository.airportsFailure = const NetworkFailure(message: 'x');
      final AirportPickupCubit cubit = build();
      await cubit.loadAirports();
      expect(cubit.state.status, AirportCatalogStatus.failure);
      expect(cubit.state.failure, isNotNull);
    });

    test('a pickup needs its zone; picking it completes the selection', () {
      final AirportPickupCubit cubit = build()
        ..choose(testAirport, AirportDirection.pickup);
      expect(cubit.state.needsZone, isTrue);
      expect(cubit.state.selection?.isComplete, isFalse);

      cubit.selectZone(testAirport.pickupZones.first);
      final AirportSelection selection = cubit.state.selection!;
      expect(cubit.state.needsZone, isFalse);
      expect(selection.isComplete, isTrue);
      // The zone point replaces the pickup coordinates.
      expect(selection.point, const GeoPoint(lat: 24.9601, lng: 46.7002));
      expect(selection.pickupZoneId, 'z1');
      expect(selection.dropoffTerminal, isNull);
      expect(selection.placeName, contains('منطقة الالتقاط 1'));
      // The zone defines the free waiting time.
      expect(selection.freeWaitingMinutes, 20);
    });

    test(
      'a zone without its own waiting policy uses the 15 minute default',
      () {
        final AirportPickupCubit cubit = build()
          ..choose(testAirport, AirportDirection.pickup)
          ..selectZone(testAirport.pickupZones.last);
        expect(cubit.state.selection?.freeWaitingMinutes, 15);
      },
    );

    test('a dropoff needs no zone; the terminal is optional', () {
      final AirportPickupCubit cubit = build()
        ..choose(testAirport, AirportDirection.dropoff);
      expect(cubit.state.selection?.isComplete, isTrue);
      expect(cubit.state.selection?.point, testAirport.point);
      expect(cubit.state.selection?.pickupZoneId, isNull);

      cubit.selectTerminal('T1');
      expect(cubit.state.selection?.dropoffTerminal, 'T1');
      cubit.selectTerminal(null);
      expect(cubit.state.selection?.dropoffTerminal, isNull);
    });

    test('switching the direction keeps the airport but drops the zone', () {
      final AirportPickupCubit cubit = build()
        ..choose(testAirport, AirportDirection.pickup)
        ..selectZone(testAirport.pickupZones.first)
        ..setDirection(AirportDirection.dropoff);
      expect(cubit.state.selection?.direction, AirportDirection.dropoff);
      expect(cubit.state.selection?.zone, isNull);
      expect(cubit.state.selection?.airport.code, 'RUH');
    });

    test('the flight number is normalized, validated and kept on changes', () {
      final AirportPickupCubit cubit = build()
        ..choose(testAirport, AirportDirection.dropoff)
        ..setFlightNumber('sv 1020');
      expect(cubit.state.selection?.flightNumber, 'SV1020');
      expect(cubit.state.flightInvalid, isFalse);

      cubit.setFlightNumber('nope!');
      expect(cubit.state.flightInvalid, isTrue);
      expect(cubit.state.selection?.isComplete, isFalse);

      cubit.setFlightNumber('');
      expect(cubit.state.selection?.flightNumber, isNull);
      expect(cubit.state.selection?.isComplete, isTrue);

      cubit
        ..setFlightNumber('ek 5')
        ..setDirection(AirportDirection.pickup);
      expect(cubit.state.selection?.flightNumber, 'EK5');
    });

    test('clear goes back to a city trip', () {
      final AirportPickupCubit cubit = build()
        ..choose(testAirport, AirportDirection.pickup)
        ..setFlightNumber('SV1')
        ..clear();
      expect(cubit.state.selection, isNull);
      expect(cubit.state.flightInput, isEmpty);
    });

    group('detect (resolve)', () {
      const GeoPoint pickup = GeoPoint(lat: 24.9576, lng: 46.6988);
      const GeoPoint dropoff = GeoPoint(lat: 24.84, lng: 46.72);

      test('no airport around the route selects nothing', () async {
        final AirportPickupCubit cubit = build();
        await cubit.detect(pickup, dropoff: dropoff);
        expect(cubit.state.selection, isNull);
        expect(repository.resolved, <GeoPoint>[pickup, dropoff]);
      });

      test('an airport pickup is detected with its zones', () async {
        repository.resolution = const AirportResolution(
          airportId: 'a1',
          code: 'RUH',
          name: 'مطار الملك خالد',
          pickupZones: <AirportZone>[AirportZone(id: 'z9', name: 'منطقة 9')],
        );
        final AirportPickupCubit cubit = build();
        await cubit.detect(pickup, dropoff: dropoff);
        final AirportSelection selection = cubit.state.selection!;
        expect(selection.direction, AirportDirection.pickup);
        expect(selection.airport.pickupZones.single.id, 'z9');
        expect(selection.needsZone, isTrue);
        expect(cubit.state.detected, isTrue);
        // Only the pickup was needed.
        expect(repository.resolved, <GeoPoint>[pickup]);
      });

      test(
        'the known catalog airport is used and requiresPickupZone kept',
        () async {
          repository.resolution = const AirportResolution(
            airportId: 'a1',
            code: 'RUH',
            name: 'x',
            requiresPickupZone: false,
          );
          final AirportPickupCubit cubit = build();
          await cubit.loadAirports();
          await cubit.detect(pickup);
          expect(cubit.state.selection?.airport.name, testAirport.name);
          expect(cubit.state.selection?.airport.pickupZones, hasLength(2));
          expect(cubit.state.selection?.needsZone, isFalse);
        },
      );

      test(
        'an airport dropoff is detected when the pickup is not one',
        () async {
          // The resolver only knows the dropoff point.
          final _DropoffOnly only = _DropoffOnly(dropoff);
          final AirportPickupCubit cubit = AirportPickupCubit(
            getAirports: GetAirports(only),
            resolve: ResolveAirport(only),
          );
          await cubit.detect(pickup, dropoff: dropoff);
          expect(cubit.state.selection?.direction, AirportDirection.dropoff);
          expect(cubit.state.selection?.isComplete, isTrue);
        },
      );

      test('a choice made by the rider is never overridden', () async {
        repository.resolution = const AirportResolution(
          airportId: 'other',
          code: 'JED',
          name: 'jeddah',
        );
        final AirportPickupCubit cubit = build()
          ..choose(testAirport, AirportDirection.dropoff);
        await cubit.detect(pickup);
        expect(cubit.state.selection?.airport.code, 'RUH');
        expect(repository.resolved, isEmpty);
      });

      test('a detected airport is dropped when the route leaves it', () async {
        repository.resolution = const AirportResolution(
          airportId: 'a1',
          code: 'RUH',
          name: 'مطار',
        );
        final AirportPickupCubit cubit = build();
        await cubit.detect(pickup);
        expect(cubit.state.selection, isNotNull);
        repository.resolution = null;
        await cubit.detect(dropoff);
        expect(cubit.state.selection, isNull);
      });
    });
  });

  group('AirportQueueCubit', () {
    late StreamController<void> ticks;

    setUp(() => ticks = StreamController<void>.broadcast());
    tearDown(() => ticks.close());

    AirportQueueCubit build() => AirportQueueCubit(
      getQueue: GetAirportQueue(repository),
      join: JoinAirportQueue(repository),
      leave: LeaveAirportQueue(repository),
      watch: WatchAirportQueue(repository),
      ticker: (Duration _) => ticks.stream,
    );

    const AirportQueueStatus queued = AirportQueueStatus(
      inQueue: true,
      airport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار الملك خالد'),
      position: 7,
      total: 23,
      estimatedWaitMinutes: 25,
    );

    test('start loads the status; an eligible driver can join', () async {
      repository.queue = const AirportQueueStatus(
        eligibleAirport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار'),
      );
      final AirportQueueCubit cubit = build();
      await cubit.start();
      expect(cubit.state.inQueue, isFalse);
      expect(cubit.state.isRelevant, isTrue);

      await cubit.join(const GeoPoint(lat: 24.96, lng: 46.7));
      expect(repository.joined.single.lat, 24.96);
      expect(cubit.state.inQueue, isTrue);
      expect(cubit.state.queue?.position, 7);
      await cubit.close();
    });

    test('a driver outside any airport has nothing to show', () async {
      final AirportQueueCubit cubit = build();
      await cubit.start();
      expect(cubit.state.isRelevant, isFalse);
      await cubit.close();
    });

    test('joining outside the waiting area reports the error', () async {
      repository
        ..queue = const AirportQueueStatus(
          eligibleAirport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار'),
        )
        ..joinFailure = const ServerFailure(
          code: 'not_in_airport_waiting_area',
          message: '',
          statusCode: 422,
        );
      final AirportQueueCubit cubit = build();
      await cubit.start();
      await cubit.join(GeoPoint.riyadh);
      expect(cubit.state.actionFailure?.code, 'not_in_airport_waiting_area');
      expect(cubit.state.inQueue, isFalse);
      await cubit.close();
    });

    test('leaving returns to the "eligible" state', () async {
      repository.queue = queued;
      final AirportQueueCubit cubit = build();
      await cubit.start();
      expect(cubit.state.inQueue, isTrue);
      await cubit.leave();
      expect(repository.leaves, 1);
      expect(cubit.state.inQueue, isFalse);
      expect(cubit.state.queue?.eligibleAirport?.code, 'RUH');
      await cubit.close();
    });

    test('AirportQueueUpdated moves the position', () async {
      repository.queue = queued;
      final AirportQueueCubit cubit = build();
      await cubit.start();
      repository.updates.add(
        const AirportQueuePosition(
          position: 3,
          total: 20,
          estimatedWaitMinutes: 9,
        ),
      );
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.queue?.position, 3);
      expect(cubit.state.queue?.total, 20);
      expect(cubit.state.queue?.estimatedWaitMinutes, 9);
      expect(cubit.state.queue?.airport?.code, 'RUH');
      await cubit.close();
    });

    test('polls every 30 seconds', () async {
      final AirportQueueCubit cubit = build();
      await cubit.start();
      expect(cubit.state.inQueue, isFalse);
      repository.queue = queued;
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.inQueue, isTrue);
      await cubit.close();
    });

    test(
      'a hub update for an unknown queue entry refreshes the status',
      () async {
        final AirportQueueCubit cubit = build();
        await cubit.start();
        repository.queue = queued;
        repository.updates.add(
          const AirportQueuePosition(position: 7, total: 23),
        );
        await Future<void>.delayed(Duration.zero);
        await Future<void>.delayed(Duration.zero);
        expect(cubit.state.inQueue, isTrue);
        await cubit.close();
      },
    );
  });
}

/// Resolves an airport only around [_airportPoint].
class _DropoffOnly extends FakeAirportRepository {
  _DropoffOnly(this._airportPoint);

  final GeoPoint _airportPoint;

  @override
  Future<Either<Failure, AirportResolution?>> resolve(GeoPoint point) async =>
      Right<Failure, AirportResolution?>(
        point == _airportPoint
            ? const AirportResolution(
                airportId: 'a1',
                code: 'RUH',
                name: 'مطار',
              )
            : null,
      );
}
