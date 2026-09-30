import 'dart:async';

import 'package:ata_app/app/corporate_cubits.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/domain/entities/trip_corporate.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:ata_app/features/corporate/domain/usecases/check_corporate_eligibility.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_cubit.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_paid_card.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_payment_section.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_types.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:ata_app/features/payments/data/models/receipt_model.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:ata_app/features/payments/presentation/pages/receipt_page.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_receipt_view.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/corporate_fakes.dart';
import '../../helpers/fakes.dart';
import '../../helpers/payments_fakes.dart';
import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';
import '../payments/receipt_test.dart' show receiptJson;

class _MockSession extends MockCubit<SessionState> implements SessionCubit {}

class _PushRepository extends FakeNotificationsRepository {
  final StreamController<PushEvent> pushes =
      StreamController<PushEvent>.broadcast();

  @override
  Stream<PushEvent> watchIncoming() => pushes.stream;
}

void main() {
  setUp(registerTestDependencies);
  tearDown(getIt.reset);

  group('the purpose form', () {
    late CorporatePaymentCubit cubit;

    setUp(() {
      cubit = CorporatePaymentCubit(
        checkEligibility: const CheckCorporateEligibility(),
      );
    });
    tearDown(() => cubit.close());

    Future<void> pump(WidgetTester tester) async {
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(
        wrapForTest(
          BlocProvider<CorporatePaymentCubit>.value(
            value: cubit,
            child: const SingleChildScrollView(
              child: CorporatePaymentSection(),
            ),
          ),
        ),
      );
    }

    testWidgets('is hidden until the company account is chosen', (
      WidgetTester tester,
    ) async {
      cubit.sync(
        const EligibilityDraft(profile: testCorporateProfile),
        selected: false,
      );
      await pump(tester);
      expect(
        find.byKey(const ValueKey<String>('corporate-section')),
        findsNothing,
      );
    });

    testWidgets('requires the purpose and the cost center when the policy '
        'demands them', (WidgetTester tester) async {
      cubit.sync(
        EligibilityDraft(
          profile: CorporateProfile(
            membership: testCorporateProfile.membership,
            policy: const CorporatePolicySummary(
              requirePurpose: true,
              requireCostCenter: true,
            ),
            costCenters: testCorporateProfile.costCenters,
          ),
        ),
        selected: true,
      );
      await pump(tester);
      expect(find.text('غرض الرحلة *'), findsOneWidget);
      expect(find.text('مركز التكلفة *'), findsOneWidget);
      expect(find.text('غرض الرحلة مطلوب'), findsOneWidget);
      expect(find.text('اختر مركز التكلفة'), findsOneWidget);
      expect(cubit.state.ready, isFalse);

      await tester.enterText(
        find.byKey(const ValueKey<String>('corporate-purpose')),
        'اجتماع',
      );
      await tester.tap(find.byKey(const ValueKey<String>('cost-center-cc2')));
      await tester.pump();
      expect(cubit.state.purpose, 'اجتماع');
      expect(cubit.state.costCenterId, 'cc2');
      expect(cubit.state.ready, isTrue);
      expect(find.text('غرض الرحلة مطلوب'), findsNothing);
      expect(find.text('اختر مركز التكلفة'), findsNothing);

      // Tapping the chosen cost center again clears it.
      await tester.tap(find.byKey(const ValueKey<String>('cost-center-cc2')));
      await tester.pump();
      expect(cubit.state.costCenterId, isNull);
      expect(find.text('اختر مركز التكلفة'), findsOneWidget);
    });

    testWidgets('optional fields carry no star and no warning', (
      WidgetTester tester,
    ) async {
      cubit.sync(
        EligibilityDraft(
          profile: CorporateProfile(
            membership: testCorporateProfile.membership,
          ),
        ),
        selected: true,
      );
      await pump(tester);
      expect(find.text('غرض الرحلة'), findsOneWidget);
      expect(find.text('غرض الرحلة مطلوب'), findsNothing);
      // No cost centers to choose from: the field is not shown at all.
      expect(find.text('مركز التكلفة'), findsNothing);
      expect(cubit.state.ready, isTrue);
    });

    testWidgets('lists the violations with the allowed options', (
      WidgetTester tester,
    ) async {
      cubit.sync(
        const EligibilityDraft(
          profile: testCorporateProfile,
          serverCheck: CorporateQuoteCheck(
            allowed: false,
            violations: <PolicyViolation>[
              PolicyViolation(
                rule: PolicyRule.timeWindow,
                allowed: <String>['07:00-22:00'],
              ),
            ],
          ),
        ),
        selected: true,
      );
      await pump(tester);
      expect(find.text('وقت الرحلة خارج الساعات المسموحة'), findsOneWidget);
      expect(find.text('الساعات المسموحة: 07:00 – 22:00'), findsOneWidget);
      expect(
        find.text('غيّر الفئة أو الوقت، أو اختر وسيلة دفع أخرى'),
        findsOneWidget,
      );
    });
  });

  group('receipts', () {
    final Map<String, dynamic> corporateReceipt = <String, dynamic>{
      ...receiptJson,
      'payment': <String, dynamic>{
        'method': 'corporate',
        'status': 'billed',
        'paidAmount': 0,
      },
      'refunds': <dynamic>[],
      'netPaid': null,
      'corporate': <String, dynamic>{
        'companyName': 'شركة المثال',
        'purpose': 'اجتماع عميل',
        'costCenter': 'IT-01',
      },
    };

    testWidgets('the receipt says the company paid, with purpose and cost '
        'center, and never mentions wallet or card charges', (
      WidgetTester tester,
    ) async {
      final FakePaymentsRepository payments = FakePaymentsRepository()
        ..receipt = ReceiptModel.fromJson(corporateReceipt);
      getIt.registerSingleton<PaymentsRepository>(payments);
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapForTest(const ReceiptPage(tripId: 't1')));
      await tester.pumpAndSettle();

      expect(find.text('حساب الشركة'), findsOneWidget);
      expect(find.text('مدفوعة من حساب الشركة'), findsOneWidget);
      expect(find.text('شركة المثال'), findsOneWidget);
      expect(find.text('اجتماع عميل'), findsOneWidget);
      expect(find.text('IT-01'), findsOneWidget);
      expect(find.text('المبلغ على حساب الشركة'), findsOneWidget);
      expect(find.text('المدفوع'), findsNothing);
      expect(find.text('مدى •••• 4201'), findsNothing);
      expect(
        find.text('تعذّر الدفع بالبطاقة، فتم تحويل الرحلة إلى الدفع نقداً'),
        findsNothing,
      );
    });

    testWidgets('a corporate receipt without the object still says so', (
      WidgetTester tester,
    ) async {
      final Map<String, dynamic> bare = <String, dynamic>{...corporateReceipt}
        ..remove('corporate');
      getIt.registerSingleton<PaymentsRepository>(
        FakePaymentsRepository()..receipt = ReceiptModel.fromJson(bare),
      );
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapForTest(const ReceiptPage(tripId: 't1')));
      await tester.pumpAndSettle();
      expect(find.text('مدفوعة من حساب الشركة'), findsOneWidget);
      expect(find.text('اجتماع عميل'), findsNothing);
    });

    testWidgets('the end-of-trip view shows who paid', (
      WidgetTester tester,
    ) async {
      await tester.binding.setSurfaceSize(const Size(430, 2000));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      final PendingRatingCubit pending = PendingRatingCubit(
        getPending: getIt(),
      );
      addTearDown(pending.close);
      const Trip trip = Trip(
        id: 't1',
        tripNumber: 'T-1',
        status: TripStage.completed,
        pickup: testPickup,
        dropoff: testDropoff,
        paymentMethod: 'corporate',
        finalFare: 34,
        corporate: TripCorporate(
          companyName: 'شركة المثال',
          purpose: 'زيارة عميل',
          costCenter: 'SL-02',
        ),
      );
      await tester.pumpWidget(
        wrapForTest(
          BlocProvider<PendingRatingCubit>.value(
            value: pending,
            child: SingleChildScrollView(
              child: TripReceiptView(trip: trip, onDone: () {}),
            ),
          ),
        ),
      );
      await tester.pump();
      expect(find.text('مدفوعة من حساب الشركة'), findsOneWidget);
      expect(find.text('زيارة عميل'), findsOneWidget);
      expect(find.text('SL-02'), findsOneWidget);
      // The summary row names the method.
      expect(find.text('حساب الشركة'), findsOneWidget);
    });

    testWidgets('the paid card without an object shows only the headline', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(wrapForTest(const CorporatePaidCard()));
      expect(find.text('مدفوعة من حساب الشركة'), findsOneWidget);
    });
  });

  group('deep links and notifications', () {
    const ParseDeepLink parse = ParseDeepLink();

    test('ata://corporate/invitations opens /account/corporate for riders', () {
      expect(
        parse(
          const DeepLinkParams(
            link: 'ata://corporate/invitations',
            isDriver: false,
          ),
        )!.route,
        '/account/corporate',
      );
      // A driver has no company account: back to the driver home.
      expect(
        parse(
          const DeepLinkParams(
            link: 'ata://corporate/invitations',
            isDriver: true,
          ),
        )!.route,
        '/driver',
      );
    });

    test('an old corporate.invitation row gets the invitations link', () {
      expect(
        NotificationTypes.fallbackLink(
          NotificationTypes.corporateInvitation,
          null,
        ),
        'ata://corporate/invitations',
      );
    });
  });

  group('CorporateCubits', () {
    final AuthSession rider = testSession.copyWith(
      user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
    );
    late FakeCorporateRepository repo;
    late _PushRepository push;
    late StreamController<SessionState> sessions;
    late CorporateCubits cubits;

    setUp(() {
      repo = getIt<CorporateRepository>() as FakeCorporateRepository
        ..profile = testCorporateProfile
        ..invitations = <CorporateInvitation>[testInvitation];
      push = _PushRepository();
      sessions = StreamController<SessionState>.broadcast();
      final _MockSession session = _MockSession();
      whenListen(
        session,
        sessions.stream,
        initialState: const SessionState.unknown(),
      );
      cubits = CorporateCubits(
        membership: CorporateMembershipCubit(getMembership: getIt()),
        invitations: CorporateInvitationsCubit(
          getInvitations: getIt(),
          accept: getIt(),
          decline: getIt(),
        ),
        watchIncoming: WatchIncomingNotifications(push),
      )..bind(session);
    });

    tearDown(() async {
      await cubits.close();
      await cubits.membership.close();
      await cubits.invitations.close();
      await sessions.close();
      await push.pushes.close();
    });

    test('nothing loads before a rider signs in; sign-out forgets', () async {
      await pumpEventQueue();
      expect(repo.profileCalls, 0);

      sessions.add(SessionState.authenticated(rider));
      await pumpEventQueue();
      expect(cubits.membership.state.isActive, isTrue);
      expect(cubits.invitations.state.invitations, hasLength(1));

      sessions.add(const SessionState.unauthenticated());
      await pumpEventQueue();
      expect(cubits.membership.state.isMember, isFalse);
      expect(cubits.invitations.state.hasInvitations, isFalse);
    });

    test('a rider who still has to accept the terms is not bound', () async {
      sessions.add(const SessionState.authenticated(testSession));
      await pumpEventQueue();
      expect(repo.profileCalls, 0);
    });

    test('a driver has no company account', () async {
      sessions.add(
        SessionState.authenticated(rider.copyWith(activeRole: UserRole.driver)),
      );
      await pumpEventQueue();
      expect(repo.profileCalls, 0);
    });

    test(
      'a corporate push reloads the membership and the invitations',
      () async {
        sessions.add(SessionState.authenticated(rider));
        await pumpEventQueue();
        final int before = repo.profileCalls;
        repo.invitations = <CorporateInvitation>[];

        push.pushes.add(const PushEvent(eventCode: 'trip.completed'));
        await pumpEventQueue();
        expect(repo.profileCalls, before);

        push.pushes.add(const PushEvent(eventCode: 'corporate.invitation'));
        await pumpEventQueue();
        expect(repo.profileCalls, before + 1);
        expect(cubits.invitations.state.hasInvitations, isFalse);
      },
    );

    test('accepting an invitation reloads the membership', () async {
      repo.profile = null;
      repo.profileOnAccept = testCorporateProfile;
      sessions.add(SessionState.authenticated(rider));
      await pumpEventQueue();
      expect(cubits.membership.state.isMember, isFalse);

      await cubits.invitations.accept('inv1');
      await pumpEventQueue();
      expect(repo.accepted, <String>['inv1']);
      expect(cubits.membership.state.isActive, isTrue);
    });
  });
}
