import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/data/models/driver_rewards_models.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/incentive.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/driver_tier_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/cubit/incentives_cubit.dart';
import 'package:ata_app/features/driver_rewards/presentation/pages/driver_tier_page.dart';
import 'package:ata_app/features/driver_rewards/presentation/pages/incentive_detail_page.dart';
import 'package:ata_app/features/driver_rewards/presentation/pages/incentives_page.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/nearest_incentive_card.dart';
import 'package:ata_app/features/driver_rewards/presentation/widgets/tier_card.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:ata_app/features/promotions/presentation/pages/promotions_page.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';

import '../../helpers/rewards_fakes.dart';
import '../../helpers/safety_fakes.dart';
import '../../helpers/test_app.dart';

class _ReducedReliability extends FakeCancellationRepository {
  @override
  Future<Either<Failure, ReliabilitySummary>> getReliability(
    TripActor role,
  ) async => const Right<Failure, ReliabilitySummary>(
    ReliabilitySummary(role: 'driver', incentiveMultiplier: 0.5),
  );
}

class _Promotions implements PromotionsRepository {
  @override
  Future<Either<Failure, List<Promotion>>> getPromotions(
    PromotionStatus status,
  ) async => Right<Failure, List<Promotion>>(<Promotion>[
    if (status == PromotionStatus.available)
      const Promotion(
        code: 'WELCOME',
        name: 'مرحباً بك',
        type: PromotionType.percent,
        value: 20,
        maxDiscount: 15,
        firstTripOnly: true,
      ),
  ]);

  @override
  Future<Either<Failure, PromoValidation>> validate(
    PromoValidationParams params,
  ) => throw UnimplementedError();
}

final Map<String, dynamic> _quest = <String, dynamic>{
  'id': 'i1',
  'name': 'رحلات المساء',
  'type': 'weekly',
  'targetTrips': 10,
  'rewardAmount': 75,
  'window': <String, dynamic>{
    'daysOfWeek': <int>[4, 5],
    'from': '16:00',
    'to': '22:00',
  },
  'zones': <Map<String, dynamic>>[
    <String, dynamic>{'id': 'z1', 'name': 'العليا'},
  ],
  'requiresOptIn': true,
  'optedIn': false,
  'progress': <String, dynamic>{'completedTrips': 7, 'status': 'in_progress'},
};

void main() {
  late FakeDriverRewardsRepository rewards;

  setUp(() async {
    await registerTestDependencies();
    rewards = getIt<DriverRewardsRepository>() as FakeDriverRewardsRepository
      ..tier = DriverRewardsModels.tier(<String, dynamic>{
        'tier': 'silver',
        'nextTier': 'gold',
        'metrics': <String, dynamic>{
          'completedTrips': 96,
          'ratingAvg': 4.82,
          'acceptanceRate': 0.88,
          'cancellationRate': 0.04,
        },
        'nextRequirements': <String, dynamic>{
          'minCompletedTrips': 150,
          'minRatingAvg': 4.85,
          'minAcceptanceRate': 0.9,
          'maxCancellationRate': 0.03,
        },
        'benefits': <String, dynamic>{'commissionDiscountPercent': 5},
      })
      ..incentives = <Incentive>[DriverRewardsModels.incentive(_quest)];
    getIt
      ..unregister<CancellationRepository>()
      ..registerSingleton<CancellationRepository>(_ReducedReliability())
      ..unregister<PromotionsRepository>()
      ..registerSingleton<PromotionsRepository>(_Promotions());
  });
  tearDown(getIt.reset);

  Future<void> pump(WidgetTester tester, Widget page) async {
    await tester.binding.setSurfaceSize(const Size(430, 1800));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(wrapForTest(page));
    await tester.pumpAndSettle();
  }

  testWidgets('tier page: badge, remaining trips, criteria and benefit', (
    WidgetTester tester,
  ) async {
    await pump(tester, const DriverTierPage());
    expect(find.text('فضي'), findsOneWidget);
    expect(find.text('تبقّت 54 رحلة للوصول إلى ذهبي'), findsOneWidget);
    expect(find.text('0 من 4 شروط محققة'), findsOneWidget);
    expect(find.text('خصم العمولة 5%'), findsOneWidget);
    expect(find.text('شروط ذهبي (آخر 28 يوماً)'), findsOneWidget);
    expect(find.text('96 / 150'), findsOneWidget);
    expect(find.text('4% / ≤ 3%'), findsOneWidget);
  });

  testWidgets('incentives: progress, reduced reward and notice', (
    WidgetTester tester,
  ) async {
    await pump(tester, const IncentivesPage());
    expect(
      find.byKey(const ValueKey<String>('reduced-reward-notice')),
      findsOneWidget,
    );
    expect(find.text('رحلات المساء'), findsOneWidget);
    expect(find.text('37.5 ر.س'), findsOneWidget);
    expect(find.text('75 ر.س'), findsOneWidget);
    expect(find.text('الخميس، الجمعة · 16:00–22:00'), findsOneWidget);
    expect(find.text('العليا'), findsOneWidget);
    expect(find.textContaining('7 من 10 رحلات'), findsOneWidget);

    await tester.tap(find.text('القادمة'));
    await tester.pumpAndSettle();
    expect(find.text('لا توجد حوافز هنا حالياً.'), findsOneWidget);
  });

  testWidgets('incentive detail: opt in, then 409 on another quest', (
    WidgetTester tester,
  ) async {
    await pump(tester, const IncentiveDetailPage(incentiveId: 'i1'));
    expect(find.text('اشترك في الحافز'), findsOneWidget);
    await tester.tap(find.text('اشترك في الحافز'));
    await tester.pumpAndSettle();
    expect(find.text('اشترك في الحافز'), findsNothing);
    expect(find.text('أنت مشترك في هذا الحافز'), findsOneWidget);

    rewards.optInFailure = const ServerFailure(
      code: 'incentive_opt_in_closed',
      message: '',
      statusCode: 409,
    );
    await tester.pumpWidget(const SizedBox.shrink());
    await pump(tester, const IncentiveDetailPage(incentiveId: 'i1'));
    await tester.tap(find.text('اشترك في الحافز'));
    await tester.pumpAndSettle();
    expect(find.text('الاشتراك في هذا الحافز غير متاح'), findsOneWidget);
  });

  testWidgets('overview cards: tier summary and nearest quest', (
    WidgetTester tester,
  ) async {
    await pump(
      tester,
      MultiBlocProvider(
        providers: <BlocProvider<dynamic>>[
          BlocProvider<DriverTierCubit>(
            create: (_) => DriverTierCubit(getTier: getIt())..load(),
          ),
          BlocProvider<IncentivesCubit>(
            create: (_) =>
                IncentivesCubit(getIncentives: getIt(), getReliability: getIt())
                  ..load(),
          ),
        ],
        child: const SingleChildScrollView(
          child: Column(children: <Widget>[TierCard(), NearestIncentiveCard()]),
        ),
      ),
    );
    expect(find.byKey(const ValueKey<String>('tier-card')), findsOneWidget);
    expect(find.text('أقرب حافز'), findsOneWidget);
    expect(find.text('رحلات المساء'), findsOneWidget);
    expect(find.text('تقييماتي'), findsOneWidget);
    expect(DriverTier.values, hasLength(4));
  });

  testWidgets('promotions page lists the available codes with actions', (
    WidgetTester tester,
  ) async {
    await pump(tester, const PromotionsPage());
    expect(find.text('WELCOME'), findsOneWidget);
    expect(find.text('خصم 20% حتى 15 ر.س'), findsOneWidget);
    expect(find.text('• للرحلة الأولى فقط'), findsOneWidget);
    expect(find.text('نسخ'), findsOneWidget);
    expect(find.text('استخدم'), findsOneWidget);
    await tester.tap(find.text('المنتهية'));
    await tester.pumpAndSettle();
    expect(find.text('لا توجد عروض هنا حالياً.'), findsOneWidget);
  });
}
