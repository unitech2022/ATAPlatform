import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_rewards/data/models/driver_rewards_models.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/usecases/get_driver_tier.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGetTier extends Mock implements GetDriverTier {}

const Map<String, dynamic> _json = <String, dynamic>{
  'tier': 'silver',
  'nextTier': 'gold',
  'periodDays': 28,
  'metrics': <String, dynamic>{
    'completedTrips': 96,
    'ratingAvg': 4.82,
    'acceptanceRate': 0.88,
    'cancellationRate': 0.04,
  },
  'nextRequirements': <String, dynamic>{
    'minCompletedTrips': 150,
    'minRatingAvg': 4.85,
    'minAcceptanceRate': 0.90,
    'maxCancellationRate': 0.03,
  },
  'benefits': <String, dynamic>{
    'commissionDiscountPercent': 5,
    'text': 'أولوية',
  },
  'recalculatesAt': '2026-10-04T00:00:00Z',
};

void main() {
  test('parses GET /driver/tier and compares every criterion', () {
    final DriverTierInfo info = DriverRewardsModels.tier(_json);
    expect(info.tier, DriverTier.silver);
    expect(info.nextTier, DriverTier.gold);
    expect(info.benefits.commissionDiscountPercent, 5);
    expect(info.recalculatesAt, DateTime.utc(2026, 10, 4));
    expect(info.isTopTier, isFalse);
    expect(info.tripsToNext, 54);
    expect(info.checks.map((TierCheck c) => c.met), <bool>[
      false,
      false,
      false,
      false,
    ]);
    expect(info.checks.first.progress, closeTo(96 / 150, 1e-9));
    expect(info.checks.last.progress, closeTo(0.03 / 0.04, 1e-9));
    expect(info.progress, greaterThan(0.6));
    expect(info.progress, lessThan(1));
  });

  test('a threshold met exactly counts as met; the top tier has no checks', () {
    const DriverTierInfo edge = DriverTierInfo(
      tier: DriverTier.gold,
      nextTier: DriverTier.platinum,
      metrics: TierMetrics(
        completedTrips: 250,
        ratingAvg: 4.9,
        acceptanceRate: 0.9,
        cancellationRate: 0.03,
      ),
      nextRequirements: TierRequirements(
        minCompletedTrips: 250,
        minRatingAvg: 4.9,
        minAcceptanceRate: 0.9,
        maxCancellationRate: 0.03,
      ),
    );
    expect(edge.metCount, 4);
    expect(edge.progress, 1);
    final DriverTierInfo top = DriverRewardsModels.tier(<String, dynamic>{
      'tier': 'platinum',
      'nextTier': null,
      'nextRequirements': null,
    });
    expect(top.isTopTier, isTrue);
    expect(top.checks, isEmpty);
    expect(top.progress, 1);
    expect(DriverTier.parse('unknown'), DriverTier.bronze);
  });

  group('DriverTierCubit', () {
    late _MockGetTier getTier;

    setUpAll(() => registerFallbackValue(const NoParams()));

    setUp(() {
      getTier = _MockGetTier();
      when(() => getTier(any())).thenAnswer(
        (_) async =>
            Right<Failure, DriverTierInfo>(DriverRewardsModels.tier(_json)),
      );
    });

    blocTest<DriverTierCubit, DriverTierState>(
      'loads the tier',
      build: () => DriverTierCubit(getTier: getTier),
      act: (DriverTierCubit c) => c.load(),
      expect: () => <DriverTierState>[
        const DriverTierState(loading: true),
        DriverTierState(info: DriverRewardsModels.tier(_json)),
      ],
    );

    blocTest<DriverTierCubit, DriverTierState>(
      'keeps the last tier when a reload fails',
      build: () => DriverTierCubit(getTier: getTier),
      act: (DriverTierCubit c) async {
        await c.load();
        when(() => getTier(any())).thenAnswer(
          (_) async => const Left<Failure, DriverTierInfo>(
            NetworkFailure(message: 'offline'),
          ),
        );
        await c.load();
      },
      verify: (DriverTierCubit c) {
        expect(c.state.info?.tier, DriverTier.silver);
        expect(c.state.failure, isA<NetworkFailure>());
      },
    );
  });
}
