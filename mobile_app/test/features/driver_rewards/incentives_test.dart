import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/data/models/driver_rewards_models.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_incentives.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/opt_in_incentive.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentive_detail_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentive_detail_state.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_state.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/get_reliability.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockList extends Mock implements GetIncentives {}

class _MockGet extends Mock implements GetIncentive {}

class _MockOptIn extends Mock implements OptInIncentive {}

class _MockReliability extends Mock implements GetReliability {}

const Map<String, dynamic> _json = <String, dynamic>{
  'id': 'i1',
  'name': '10 رحلات مساء الخميس والجمعة',
  'description': 'أكمل 10 رحلات',
  'type': 'weekly',
  'targetTrips': 10,
  'rewardAmount': 75,
  'periodStart': '2026-09-27T00:00:00Z',
  'periodEnd': '2026-10-04T00:00:00Z',
  'window': <String, dynamic>{
    'daysOfWeek': <int>[4, 5],
    'from': '16:00',
    'to': '23:59',
  },
  'zones': <Map<String, dynamic>>[
    <String, dynamic>{'id': 'z1', 'name': 'شمال الرياض'},
  ],
  'rideCategoryCodes': null,
  'requiresOptIn': true,
  'optedIn': false,
  'progress': <String, dynamic>{
    'completedTrips': 7,
    'status': 'in_progress',
    'rewardAmount': null,
    'paidAt': null,
  },
};

Incentive _quest(String id, int done, {String status = 'in_progress'}) =>
    DriverRewardsModels.incentive(<String, dynamic>{
      ..._json,
      'id': id,
      'progress': <String, dynamic>{'completedTrips': done, 'status': status},
    });

ReliabilitySummary _reliability(double multiplier) => ReliabilitySummary(
  role: 'driver',
  level: RestrictionLevel.incentivesReduced,
  incentiveMultiplier: multiplier,
);

void main() {
  late _MockList list;
  late _MockReliability reliability;

  setUpAll(() {
    registerFallbackValue(IncentiveTab.active);
    registerFallbackValue(TripActor.driver);
  });

  setUp(() {
    list = _MockList();
    reliability = _MockReliability();
    when(() => list(IncentiveTab.active)).thenAnswer(
      (_) async => Right<Failure, List<Incentive>>(<Incentive>[
        _quest('a', 3),
        _quest('b', 8),
        _quest('c', 10, status: 'achieved'),
      ]),
    );
    when(() => list(IncentiveTab.upcoming)).thenAnswer(
      (_) async => const Right<Failure, List<Incentive>>(<Incentive>[]),
    );
    when(() => reliability(any())).thenAnswer(
      (_) async => Right<Failure, ReliabilitySummary>(_reliability(0.5)),
    );
  });

  test('parses a quest with window, zones, opt-in and progress', () {
    final Incentive i = DriverRewardsModels.incentive(_json);
    expect(i.window?.daysOfWeek, <int>[4, 5]);
    expect(i.window?.from, '16:00');
    expect(i.zones?.single.name, 'شمال الرياض');
    expect(i.rideCategoryCodes, isNull);
    expect(i.needsOptIn, isTrue);
    expect(i.completedTrips, 7);
    expect(i.progressValue, 0.7);
    expect(i.tripsLeft, 3);
    expect(i.rewardWith(0.5), 37.5);
    expect(i.joined().needsOptIn, isFalse);
    final Incentive bare = DriverRewardsModels.incentive(<String, dynamic>{
      'id': 'x',
      'name': 'n',
      'window': null,
      'zones': null,
      'progress': null,
    });
    expect(bare.window, isNull);
    expect(bare.zones, isNull);
    expect(bare.progress, isNull);
    expect(bare.progressValue, 0);
  });

  group('IncentivesCubit', () {
    IncentivesCubit build() =>
        IncentivesCubit(getIncentives: list, getReliability: reliability);

    blocTest<IncentivesCubit, IncentivesState>(
      'loads the active quests and the reduced multiplier',
      build: build,
      act: (IncentivesCubit c) => c.load(),
      verify: (IncentivesCubit c) {
        expect(c.state.current.map((Incentive i) => i.id), <String>[
          'a',
          'b',
          'c',
        ]);
        expect(c.state.multiplier, 0.5);
        expect(c.state.isReduced, isTrue);
        // The achieved quest is skipped: "b" (8/10) is the closest.
        expect(c.state.nearest?.id, 'b');
      },
    );

    blocTest<IncentivesCubit, IncentivesState>(
      'switching tabs loads the tab once and shows the empty state',
      build: build,
      act: (IncentivesCubit c) async {
        await c.load();
        await c.selectTab(IncentiveTab.upcoming);
        await c.selectTab(IncentiveTab.active);
        await c.selectTab(IncentiveTab.upcoming);
      },
      verify: (IncentivesCubit c) {
        expect(c.state.isEmpty, isTrue);
        verify(() => list(IncentiveTab.upcoming)).called(1);
        verify(() => list(IncentiveTab.active)).called(1);
      },
    );

    blocTest<IncentivesCubit, IncentivesState>(
      'a reliability failure keeps the full rewards',
      setUp: () => when(() => reliability(any())).thenAnswer(
        (_) async => const Left<Failure, ReliabilitySummary>(
          NetworkFailure(message: 'offline'),
        ),
      ),
      build: build,
      act: (IncentivesCubit c) => c.load(),
      verify: (IncentivesCubit c) {
        expect(c.state.multiplier, 1);
        expect(c.state.isReduced, isFalse);
      },
    );
  });

  group('IncentiveDetailCubit', () {
    late _MockGet get;
    late _MockOptIn optIn;

    setUp(() {
      get = _MockGet();
      optIn = _MockOptIn();
      when(() => get('i1')).thenAnswer(
        (_) async =>
            Right<Failure, Incentive>(DriverRewardsModels.incentive(_json)),
      );
      when(
        () => optIn('i1'),
      ).thenAnswer((_) async => const Right<Failure, Unit>(unit));
    });

    IncentiveDetailCubit build() => IncentiveDetailCubit(
      incentiveId: 'i1',
      getIncentive: get,
      optIn: optIn,
      getReliability: reliability,
    );

    blocTest<IncentiveDetailCubit, IncentiveDetailState>(
      'loads the quest with the multiplier, then joins it',
      build: build,
      act: (IncentiveDetailCubit c) async {
        await c.load();
        await c.optIn();
        await c.optIn();
      },
      verify: (IncentiveDetailCubit c) {
        expect(c.state.multiplier, 0.5);
        expect(c.state.incentive?.optedIn, isTrue);
        expect(c.state.joining, isFalse);
        verify(() => optIn('i1')).called(1);
      },
    );

    blocTest<IncentiveDetailCubit, IncentiveDetailState>(
      '409 incentive_opt_in_closed is kept as the action failure',
      setUp: () => when(() => optIn('i1')).thenAnswer(
        (_) async => const Left<Failure, Unit>(
          ServerFailure(
            code: 'incentive_opt_in_closed',
            message: '',
            statusCode: 409,
          ),
        ),
      ),
      build: build,
      act: (IncentiveDetailCubit c) async {
        await c.load();
        await c.optIn();
      },
      verify: (IncentiveDetailCubit c) {
        expect(c.state.actionFailure?.code, 'incentive_opt_in_closed');
        expect(c.state.incentive?.needsOptIn, isTrue);
      },
    );
  });
}
