import 'dart:async';

import 'package:ata_app/features/safety/presentation/cubit/sos_hold_cubit.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late StreamController<void> ticks;

  setUp(() => ticks = StreamController<void>.broadcast());
  tearDown(() => ticks.close());

  SosHoldCubit build() => SosHoldCubit(
    holdDuration: const Duration(milliseconds: 200),
    tick: const Duration(milliseconds: 50),
    ticker: (_) => ticks.stream,
  );

  Future<void> tick([int n = 1]) async {
    for (int i = 0; i < n; i++) {
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
    }
  }

  blocTest<SosHoldCubit, SosHoldState>(
    'holding for the full duration fills the ring and confirms',
    build: build,
    act: (SosHoldCubit cubit) async {
      cubit.press();
      await tick(4);
    },
    expect: () => const <SosHoldState>[
      SosHoldState(holding: true),
      SosHoldState(progress: 0.25, holding: true),
      SosHoldState(progress: 0.5, holding: true),
      SosHoldState(progress: 0.75, holding: true),
      SosHoldState(progress: 1, confirmed: true),
    ],
  );

  blocTest<SosHoldCubit, SosHoldState>(
    'releasing early resets the progress and never confirms',
    build: build,
    act: (SosHoldCubit cubit) async {
      cubit.press();
      await tick(2);
      cubit.release();
      await tick(3);
    },
    expect: () => const <SosHoldState>[
      SosHoldState(holding: true),
      SosHoldState(progress: 0.25, holding: true),
      SosHoldState(progress: 0.5, holding: true),
      SosHoldState(),
    ],
  );

  blocTest<SosHoldCubit, SosHoldState>(
    'a confirmed hold ignores release; reset allows a new hold',
    build: build,
    act: (SosHoldCubit cubit) async {
      cubit.press();
      await tick(4);
      cubit
        ..release()
        ..press()
        ..reset()
        ..press();
    },
    skip: 5,
    expect: () => const <SosHoldState>[
      SosHoldState(),
      SosHoldState(holding: true),
    ],
  );
}
