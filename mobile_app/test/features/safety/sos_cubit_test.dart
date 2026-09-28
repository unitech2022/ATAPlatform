import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/usecases/cancel_sos.dart';
import 'package:ata_app/features/safety/domain/usecases/get_safety_case.dart';
import 'package:ata_app/features/safety/domain/usecases/send_sos_location.dart';
import 'package:ata_app/features/safety/domain/usecases/trigger_sos.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_state.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/usecases/request_location_access.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_device_position.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockTrigger extends Mock implements TriggerSos {}

class _MockSendLocation extends Mock implements SendSosLocation {}

class _MockCancel extends Mock implements CancelSos {}

class _MockGetCase extends Mock implements GetSafetyCase {}

class _MockAccess extends Mock implements RequestLocationAccess {}

class _MockPositions extends Mock implements WatchDevicePosition {}

void main() {
  late _MockTrigger trigger;
  late _MockSendLocation send;
  late _MockCancel cancel;
  late _MockGetCase getCase;
  late _MockAccess access;
  late _MockPositions positions;
  late StreamController<DriverPosition> feed;
  late StreamController<void> ticks;

  const GeoPoint here = GeoPoint(lat: 24.7, lng: 46.6);
  const SosResult result = SosResult(
    caseId: 'k1',
    caseNumber: 'SC-20260928-0007',
    contactsNotified: 2,
  );

  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(const SosRequest(point: here));
    registerFallbackValue(const SosLocationParams(caseId: '', point: here));
    registerFallbackValue(const CancelSosParams(caseId: ''));
  });

  setUp(() {
    trigger = _MockTrigger();
    send = _MockSendLocation();
    cancel = _MockCancel();
    getCase = _MockGetCase();
    access = _MockAccess();
    positions = _MockPositions();
    feed = StreamController<DriverPosition>.broadcast();
    ticks = StreamController<void>.broadcast();
    when(() => access(any())).thenAnswer(
      (_) async => const Right<Failure, LocationAccess>(LocationAccess.granted),
    );
    when(() => positions()).thenAnswer((_) => feed.stream);
    when(
      () => trigger(any()),
    ).thenAnswer((_) async => const Right<Failure, SosResult>(result));
    when(
      () => send(any()),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit));
  });

  tearDown(() async {
    await feed.close();
    await ticks.close();
  });

  SosCubit build() => SosCubit(
    triggerSos: trigger,
    sendLocation: send,
    cancelSos: cancel,
    getCase: getCase,
    requestAccess: access,
    watchPosition: positions,
    ticker: (_) => ticks.stream,
    positionTimeout: const Duration(milliseconds: 10),
    statusEvery: 100,
  );

  blocTest<SosCubit, SosState>(
    'raises the SOS with the trip id and the device position, then streams',
    build: build,
    act: (SosCubit cubit) async {
      final Future<void> raising = cubit.trigger(tripId: 't1');
      await Future<void>.delayed(Duration.zero);
      feed.add(const DriverPosition(point: here, accuracy: 12));
      await raising;
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
    },
    expect: () => const <SosState>[
      SosState(status: SosStatus.sending),
      SosState(status: SosStatus.active, result: result),
      SosState(status: SosStatus.active, result: result, locationsSent: 1),
    ],
    verify: (_) {
      final SosRequest request =
          verify(() => trigger(captureAny())).captured.single as SosRequest;
      expect(request.tripId, 't1');
      expect(request.point, here);
      expect(request.accuracy, 12);
      expect(request.notifyTrustedContacts, isTrue);
      final SosLocationParams params =
          verify(() => send(captureAny())).captured.single as SosLocationParams;
      expect(params.caseId, 'k1');
    },
  );

  blocTest<SosCubit, SosState>(
    'falls back to the given point when no position arrives in time',
    build: build,
    act: (SosCubit cubit) =>
        cubit.trigger(fallback: const GeoPoint(lat: 1, lng: 2)),
    verify: (_) {
      final SosRequest request =
          verify(() => trigger(captureAny())).captured.single as SosRequest;
      expect(request.point, const GeoPoint(lat: 1, lng: 2));
    },
  );

  blocTest<SosCubit, SosState>(
    '"pressed by mistake" cancels the case and stops streaming',
    build: build,
    setUp: () => when(() => cancel(any())).thenAnswer(
      (_) async => const Right<Failure, SafetyCaseSummary>(
        SafetyCaseSummary(id: 'k1', caseNumber: 'SC-1', status: 'open'),
      ),
    ),
    act: (SosCubit cubit) async {
      await cubit.trigger(fallback: here);
      await cubit.cancel();
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
    },
    skip: 2,
    expect: () => const <SosState>[
      SosState(status: SosStatus.cancelling, result: result),
      SosState(status: SosStatus.cancelled, result: result),
    ],
    verify: (_) => verifyNever(() => send(any())),
  );
}
