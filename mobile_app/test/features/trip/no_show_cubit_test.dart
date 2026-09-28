import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/usecases/mark_passenger_no_show.dart';
import 'package:ata_app/features/trip/presentation/cubit/no_show_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/no_show_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/trip_fakes.dart';

class _MockNoShow extends Mock implements MarkPassengerNoShow {}

void main() {
  late _MockNoShow noShow;
  late StreamController<void> ticks;
  late DateTime now;
  final DateTime arrivedAt = DateTime.utc(2026, 9, 28, 12);

  setUpAll(() => registerFallbackValue(const NoShowParams(tripId: '')));

  setUp(() {
    noShow = _MockNoShow();
    ticks = StreamController<void>.broadcast();
    now = arrivedAt.add(const Duration(minutes: 4, seconds: 58));
  });

  tearDown(() => ticks.close());

  NoShowCubit build() => NoShowCubit(
    markNoShow: noShow,
    ticker: (_) => ticks.stream,
    now: () => now,
  );

  Future<void> advance(Duration by) async {
    now = now.add(by);
    ticks.add(null);
    await Future<void>.delayed(Duration.zero);
  }

  final Trip waiting = tripAt(TripStage.waiting, arrivedAt: arrivedAt);

  blocTest<NoShowCubit, NoShowState>(
    'counts down from arrivedAt and enables the action after the wait',
    build: build,
    act: (NoShowCubit cubit) async {
      cubit.start(waiting);
      expect(cubit.state.canMarkNoShow, isFalse);
      await advance(const Duration(seconds: 1));
      await advance(const Duration(seconds: 1));
    },
    expect: () => const <NoShowState>[
      NoShowState(started: true, secondsRemaining: 2),
      NoShowState(started: true, secondsRemaining: 1),
      NoShowState(started: true),
    ],
    verify: (NoShowCubit cubit) => expect(cubit.state.canMarkNoShow, isTrue),
  );

  blocTest<NoShowCubit, NoShowState>(
    'no_show_too_early resynchronises the countdown with the server',
    build: build,
    setUp: () {
      now = arrivedAt.add(const Duration(minutes: 6));
      when(() => noShow(any())).thenAnswer(
        (_) async => const Left<Failure, Trip>(
          ServerFailure(
            code: 'no_show_too_early',
            message: '',
            details: <String, dynamic>{'secondsRemaining': 45},
            statusCode: 422,
          ),
        ),
      );
    },
    act: (NoShowCubit cubit) async {
      cubit.start(waiting);
      await cubit.markNoShow();
    },
    verify: (NoShowCubit cubit) {
      expect(cubit.state.secondsRemaining, 45);
      expect(cubit.state.canMarkNoShow, isFalse);
      expect(cubit.state.failure?.code, 'no_show_too_early');
    },
  );

  blocTest<NoShowCubit, NoShowState>(
    'a successful no-show exposes the cancelled trip',
    build: build,
    setUp: () {
      now = arrivedAt.add(const Duration(minutes: 5));
      when(() => noShow(any())).thenAnswer(
        (_) async => Right<Failure, Trip>(tripAt(TripStage.cancelled)),
      );
    },
    act: (NoShowCubit cubit) async {
      cubit.start(waiting);
      await cubit.markNoShow();
    },
    verify: (NoShowCubit cubit) {
      expect(cubit.state.result?.status, TripStage.cancelled);
      final NoShowParams p =
          verify(() => noShow(captureAny())).captured.single as NoShowParams;
      expect(p.tripId, 't1');
    },
  );
}
