import 'dart:async';

import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/usecases/request_location_access.dart';
import 'package:ata_app/features/trip/domain/usecases/send_driver_location.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_device_position.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/trip_fakes.dart';

void main() {
  late FakeTripRepository trips;
  late FakeLocationRepository location;
  late StreamController<void> heartbeat;
  DateTime now = DateTime.utc(2026, 9, 28, 12);

  setUp(() {
    trips = FakeTripRepository();
    location = FakeLocationRepository();
    heartbeat = StreamController<void>.broadcast();
    now = DateTime.utc(2026, 9, 28, 12);
  });

  LocationStreamCubit build() => LocationStreamCubit(
    requestAccess: RequestLocationAccess(location),
    watchPosition: WatchDevicePosition(location),
    sendLocation: SendDriverLocation(trips),
    ticker: (_) => heartbeat.stream,
    now: () => now,
  );

  const DriverPosition position = DriverPosition(point: GeoPoint.riyadh);

  blocTest<LocationStreamCubit, LocationStreamState>(
    'sends the first position, throttles movement and re-sends on ticks',
    build: build,
    act: (LocationStreamCubit cubit) async {
      await cubit.start();
      location.feed.add(position);
      await Future<void>.delayed(Duration.zero);
      now = now.add(const Duration(seconds: 1));
      location.feed.add(position);
      await Future<void>.delayed(Duration.zero);
      now = now.add(const Duration(seconds: 4));
      heartbeat.add(null);
      await Future<void>.delayed(Duration.zero);
    },
    verify: (LocationStreamCubit cubit) {
      expect(cubit.state.isStreaming, isTrue);
      expect(trips.sentPositions.length, 2);
      expect(cubit.state.lastSentAt, now);
    },
  );

  blocTest<LocationStreamCubit, LocationStreamState>(
    'a denied permission is reported and nothing is sent',
    build: build,
    setUp: () => location.access = LocationAccess.deniedForever,
    act: (LocationStreamCubit cubit) => cubit.start(),
    expect: () => <LocationStreamState>[
      const LocationStreamState(status: LocationStreamStatus.requesting),
      const LocationStreamState(status: LocationStreamStatus.deniedForever),
    ],
    verify: (LocationStreamCubit cubit) {
      expect(cubit.state.isBlocked, isTrue);
      expect(trips.sentPositions, isEmpty);
    },
  );

  test('stop resets the state', () async {
    final LocationStreamCubit cubit = build();
    await cubit.start();
    await cubit.stop();
    expect(cubit.state, const LocationStreamState());
    await cubit.close();
  });
}
