import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/usecases/get_demand_at_location.dart';
import 'package:ata_app/features/pricing/presentation/cubit/demand_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/demand_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/pricing_fakes.dart';

void main() {
  late FakePricingRepository repository;
  late StreamController<void> ticks;
  const GeoPoint elsewhere = GeoPoint(lat: 24.8, lng: 46.7);

  setUp(() {
    repository = FakePricingRepository();
    ticks = StreamController<void>.broadcast();
  });

  tearDown(() => ticks.close());

  DemandCubit build() => DemandCubit(
    getDemand: GetDemandAtLocation(repository),
    ticker: (_) => ticks.stream,
  );

  blocTest<DemandCubit, DemandState>(
    'watch loads the level at the pickup and shows the badge when elevated',
    build: build,
    act: (DemandCubit cubit) => cubit.watch(GeoPoint.riyadh),
    expect: () => <DemandState>[
      const DemandState(location: GeoPoint.riyadh),
      const DemandState(status: DemandStatus.loading, location: GeoPoint.riyadh),
      const DemandState(
        status: DemandStatus.ready,
        location: GeoPoint.riyadh,
        level: testHighDemand,
      ),
    ],
    verify: (DemandCubit cubit) {
      expect(cubit.state.showBadge, isTrue);
      expect(repository.demandCalls, 1);
    },
  );

  blocTest<DemandCubit, DemandState>(
    'the same pickup is not reloaded, a new one is',
    build: build,
    act: (DemandCubit cubit) async {
      await cubit.watch(GeoPoint.riyadh);
      await cubit.watch(GeoPoint.riyadh);
      await cubit.watch(elsewhere);
    },
    verify: (DemandCubit cubit) {
      expect(repository.demandCalls, 2);
      expect(cubit.state.location, elsewhere);
    },
  );

  blocTest<DemandCubit, DemandState>(
    'every tick refreshes while the sheet is open',
    build: build,
    act: (DemandCubit cubit) async {
      await cubit.watch(GeoPoint.riyadh);
      repository.demand = DemandLevel.normal;
      ticks.add(null);
      await Future<void>.delayed(Duration.zero);
    },
    verify: (DemandCubit cubit) {
      expect(repository.demandCalls, 2);
      expect(cubit.state.level, DemandLevel.normal);
      expect(cubit.state.showBadge, isFalse);
    },
  );

  blocTest<DemandCubit, DemandState>(
    'a failure keeps the last level and hides nothing else',
    build: build,
    act: (DemandCubit cubit) async {
      await cubit.watch(GeoPoint.riyadh);
      repository.failure = const NetworkFailure(message: 'offline');
      await cubit.refresh();
    },
    verify: (DemandCubit cubit) {
      expect(cubit.state.status, DemandStatus.failure);
      expect(cubit.state.level, testHighDemand);
      expect(cubit.state.failure, isA<NetworkFailure>());
    },
  );
}
