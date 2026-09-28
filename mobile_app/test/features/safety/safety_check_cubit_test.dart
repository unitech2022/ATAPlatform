import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/get_pending_safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/respond_to_safety_alert.dart';
import 'package:ata_app/features/safety/domain/usecases/watch_safety_checks.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockWatch extends Mock implements WatchSafetyChecks {}

class _MockPending extends Mock implements GetPendingSafetyAlert {}

class _MockRespond extends Mock implements RespondToSafetyAlert {}

void main() {
  late _MockWatch watch;
  late _MockPending pending;
  late _MockRespond respond;
  late StreamController<SafetyAlert> checks;
  late StreamController<void> ticks;
  late DateTime now;

  final DateTime t0 = DateTime.utc(2026, 9, 28, 12);
  late SafetyAlert alert;

  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(
      const RespondToAlertParams(alertId: '', response: SafetyCheckResponse.ok),
    );
  });

  setUp(() {
    watch = _MockWatch();
    pending = _MockPending();
    respond = _MockRespond();
    checks = StreamController<SafetyAlert>.broadcast();
    ticks = StreamController<void>.broadcast();
    now = t0;
    alert = SafetyAlert(
      id: 'a1',
      tripId: 't1',
      type: 'unexpected_stop',
      respondBy: t0.add(const Duration(seconds: 120)),
    );
    when(() => watch()).thenAnswer((_) => checks.stream);
    when(
      () => pending(any()),
    ).thenAnswer((_) async => const Right<Failure, SafetyAlert?>(null));
    when(() => respond(any())).thenAnswer(
      (_) async => const Right<Failure, SafetyAlert>(
        SafetyAlert(id: 'a1', status: 'resolved_ok'),
      ),
    );
  });

  tearDown(() async {
    await checks.close();
    await ticks.close();
  });

  SafetyCheckCubit build() => SafetyCheckCubit(
    watchChecks: watch,
    getPending: pending,
    respond: respond,
    ticker: (_) => ticks.stream,
    now: () => now,
  );

  blocTest<SafetyCheckCubit, SafetyCheckState>(
    'a hub SafetyCheck prompts with a countdown; "I\'m OK" answers',
    build: build,
    act: (SafetyCheckCubit cubit) async {
      await cubit.start();
      checks.add(alert);
      await Future<void>.delayed(Duration.zero);
      now = t0.add(const Duration(seconds: 30));
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
      await cubit.respond(SafetyCheckResponse.ok);
    },
    expect: () => <dynamic>[
      SafetyCheckState(
        status: SafetyCheckStatus.prompting,
        alert: alert,
        secondsLeft: 120,
      ),
      SafetyCheckState(
        status: SafetyCheckStatus.prompting,
        alert: alert,
        secondsLeft: 90,
      ),
      SafetyCheckState(
        status: SafetyCheckStatus.responding,
        alert: alert,
        secondsLeft: 90,
        response: SafetyCheckResponse.ok,
      ),
      isA<SafetyCheckState>()
          .having(
            (SafetyCheckState s) => s.status,
            'status',
            SafetyCheckStatus.answered,
          )
          .having(
            (SafetyCheckState s) => s.alert?.status,
            'alert',
            'resolved_ok',
          ),
    ],
  );

  blocTest<SafetyCheckCubit, SafetyCheckState>(
    'the push "help" button answers need_help right away',
    build: build,
    setUp: () => when(
      () => pending(any()),
    ).thenAnswer((_) async => Right<Failure, SafetyAlert?>(alert)),
    act: (SafetyCheckCubit cubit) => cubit.open('a1', actionId: 'help'),
    verify: (SafetyCheckCubit cubit) {
      final RespondToAlertParams p =
          verify(() => respond(captureAny())).captured.single
              as RespondToAlertParams;
      expect(p.alertId, 'a1');
      expect(p.response, SafetyCheckResponse.needHelp);
      expect(cubit.state.status, SafetyCheckStatus.answered);
      expect(cubit.state.response, SafetyCheckResponse.needHelp);
    },
  );

  blocTest<SafetyCheckCubit, SafetyCheckState>(
    'restores a pending check on start and expires at respondBy',
    build: build,
    setUp: () => when(
      () => pending(any()),
    ).thenAnswer((_) async => Right<Failure, SafetyAlert?>(alert)),
    act: (SafetyCheckCubit cubit) async {
      await cubit.start();
      now = t0.add(const Duration(seconds: 121));
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
    },
    verify: (SafetyCheckCubit cubit) {
      expect(cubit.state.status, SafetyCheckStatus.expired);
      expect(cubit.state.canRespond, isFalse);
    },
  );

  blocTest<SafetyCheckCubit, SafetyCheckState>(
    'a 409 (no longer pending) closes the prompt as expired',
    build: build,
    seed: () =>
        SafetyCheckState(status: SafetyCheckStatus.prompting, alert: alert),
    setUp: () => when(() => respond(any())).thenAnswer(
      (_) async => const Left<Failure, SafetyAlert>(
        ServerFailure(code: 'conflict', message: '', statusCode: 409),
      ),
    ),
    act: (SafetyCheckCubit cubit) =>
        cubit.respond(SafetyCheckResponse.needHelp),
    verify: (SafetyCheckCubit cubit) =>
        expect(cubit.state.status, SafetyCheckStatus.expired),
  );
}
