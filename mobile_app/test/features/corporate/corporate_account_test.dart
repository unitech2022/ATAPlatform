import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/account/presentation/pages/account_page.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:ata_app/features/corporate/presentation/pages/corporate_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

import '../../helpers/corporate_fakes.dart';
import '../../helpers/fakes.dart';
import '../../helpers/test_app.dart';

final AuthSession _rider = testSession.copyWith(
  user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
);

void main() {
  late FakeCorporateRepository corporate;

  setUp(() async {
    await registerTestDependencies(auth: FakeAuthRepository(restored: _rider));
    corporate = getIt<CorporateRepository>() as FakeCorporateRepository;
  });
  tearDown(getIt.reset);

  Future<void> openAccount(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 3000));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(buildTestApp());
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    GoRouter.of(tester.element(find.byType(Scaffold).first)).go('/account');
    await tester.pumpAndSettle();
    expect(find.byType(AccountPage), findsOneWidget);
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  testWidgets('no company and no invitation: the account page has no card', (
    WidgetTester tester,
  ) async {
    await openAccount(tester);
    expect(find.byKey(const ValueKey<String>('company-card')), findsNothing);
    expect(
      find.byKey(const ValueKey<String>('invitation-prompt')),
      findsNothing,
    );
    await leave(tester);
  });

  testWidgets('"شركتي" shows the company, the role and the budget used', (
    WidgetTester tester,
  ) async {
    corporate.profile = testCorporateProfile;
    await openAccount(tester);
    expect(find.byKey(const ValueKey<String>('company-card')), findsOneWidget);
    expect(find.text('شركتي'), findsOneWidget);
    expect(find.text('شركة المثال'), findsOneWidget);
    expect(find.text('موظف'), findsOneWidget);
    expect(find.text('استُخدم 420.5 ر.س من 1,500 ر.س'), findsOneWidget);
    expect(find.text('المتبقي 1,079.5 ر.س'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey<String>('company-card')));
    await tester.pumpAndSettle();
    expect(find.byType(CorporatePage), findsOneWidget);
    // Membership, budget and per-trip limit.
    expect(find.text('فعّال'), findsOneWidget);
    expect(find.text('الرقم الوظيفي: E-17 · القسم: المبيعات'), findsOneWidget);
    expect(find.text('الحد الأقصى للرحلة 150 ر.س'), findsOneWidget);
    // The policy.
    expect(find.text('السياسة الافتراضية'), findsOneWidget);
    expect(find.text('الفئات: كل الفئات'), findsOneWidget);
    expect(find.text('غرض الرحلة مطلوب'), findsOneWidget);
    expect(find.text('الحجز المجدول: مسموح'), findsOneWidget);
    await leave(tester);
  });

  testWidgets('a disabled membership says so and hides budget and policy', (
    WidgetTester tester,
  ) async {
    corporate.profile = CorporateProfile(
      membership: const CorporateMembership(
        corporateUserId: 'cu1',
        accountId: 'ca1',
        companyName: 'شركة المثال',
        status: CorporateMemberStatus.disabled,
      ),
      policy: testCorporateProfile.policy,
      budget: testCorporateProfile.budget,
    );
    await openAccount(tester);
    await tester.tap(find.byKey(const ValueKey<String>('company-card')));
    await tester.pumpAndSettle();
    expect(find.text('معطّل'), findsOneWidget);
    expect(
      find.text('تم تعطيل حسابك في الشركة، تواصل مع مسؤول الشركة'),
      findsOneWidget,
    );
    expect(find.text('الميزانية الشهرية'), findsNothing);
    expect(
      find.byKey(const ValueKey<String>('corporate-policy')),
      findsNothing,
    );
    await leave(tester);
  });

  testWidgets('a pending invitation is prompted and accepted in the app', (
    WidgetTester tester,
  ) async {
    corporate.invitations = <CorporateInvitation>[testInvitation];
    corporate.profileOnAccept = testCorporateProfile;
    await openAccount(tester);
    expect(
      find.byKey(const ValueKey<String>('invitation-prompt')),
      findsOneWidget,
    );
    expect(find.text('لديك دعوة للانضمام إلى شركة'), findsOneWidget);

    await tester.tap(find.text('عرض الدعوة'));
    await tester.pumpAndSettle();
    expect(find.byType(CorporatePage), findsOneWidget);
    expect(find.text('دعتك شركة المثال للانضمام'), findsOneWidget);
    expect(find.text('الدور: موظف'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('corporate-none')),
      findsOneWidget,
    );

    await tester.tap(find.byKey(const ValueKey<String>('accept-inv1')));
    await tester.pumpAndSettle();
    expect(corporate.accepted, <String>['inv1']);
    expect(find.text('تم الانضمام إلى شركة المثال'), findsOneWidget);
    // The membership was reloaded: details replace the empty state.
    expect(
      find.byKey(const ValueKey<String>('corporate-details')),
      findsOneWidget,
    );
    expect(find.byKey(const ValueKey<String>('invitation-inv1')), findsNothing);
    await leave(tester);
  });

  testWidgets('declining removes the invitation', (WidgetTester tester) async {
    corporate.invitations = <CorporateInvitation>[testInvitation];
    await openAccount(tester);
    await tester.tap(find.text('عرض الدعوة'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey<String>('decline-inv1')));
    await tester.pumpAndSettle();
    expect(corporate.declined, <String>['inv1']);
    expect(find.text('تم رفض الدعوة'), findsOneWidget);
    expect(find.byKey(const ValueKey<String>('invitation-inv1')), findsNothing);
    await leave(tester);
  });

  testWidgets('an expired invitation shows its error and disappears', (
    WidgetTester tester,
  ) async {
    corporate.invitations = <CorporateInvitation>[testInvitation];
    corporate.actionFailure = const ServerFailure(
      code: 'invitation_expired',
      message: '',
      statusCode: 410,
    );
    await openAccount(tester);
    await tester.tap(find.text('عرض الدعوة'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey<String>('accept-inv1')));
    await tester.pumpAndSettle();
    expect(
      find.text('انتهت صلاحية الدعوة، اطلب من شركتك إعادة إرسالها'),
      findsOneWidget,
    );
    expect(find.byKey(const ValueKey<String>('invitation-inv1')), findsNothing);
    await leave(tester);
  });

  testWidgets('member elsewhere is explained and the invitation stays', (
    WidgetTester tester,
  ) async {
    corporate.invitations = <CorporateInvitation>[testInvitation];
    corporate.actionFailure = const ServerFailure(
      code: 'corporate_member_elsewhere',
      message: '',
      statusCode: 409,
    );
    await openAccount(tester);
    await tester.tap(find.text('عرض الدعوة'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey<String>('accept-inv1')));
    await tester.pumpAndSettle();
    expect(find.text('الرقم مرتبط بحساب شركة آخر'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('invitation-inv1')),
      findsOneWidget,
    );
    await leave(tester);
  });

  testWidgets('a failed load offers a retry', (WidgetTester tester) async {
    corporate.failure = const NetworkFailure(message: 'x');
    await openAccount(tester);
    GoRouter.of(
      tester.element(find.byType(Scaffold).first),
    ).go('/account/corporate');
    await tester.pumpAndSettle();
    expect(find.byType(CorporatePage), findsOneWidget);
    expect(find.text('إعادة المحاولة'), findsOneWidget);
    corporate
      ..failure = null
      ..profile = testCorporateProfile;
    await tester.tap(find.text('إعادة المحاولة'));
    await tester.pumpAndSettle();
    expect(
      find.byKey(const ValueKey<String>('corporate-details')),
      findsOneWidget,
    );
    await leave(tester);
  });
}
