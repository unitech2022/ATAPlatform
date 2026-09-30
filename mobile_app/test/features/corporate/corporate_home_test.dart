import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/payment_row.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/corporate_fakes.dart';
import '../../helpers/fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/test_app.dart';
import '../../helpers/trip_fakes.dart';

final AuthSession _rider = testSession.copyWith(
  user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
);

FareQuote _quoteWith(CorporateQuoteCheck check) => FareQuote(
  quoteId: testQuote.quoteId,
  expiresAt: DateTime.now().add(const Duration(minutes: 5)),
  distanceMeters: testQuote.distanceMeters,
  durationSeconds: testQuote.durationSeconds,
  pickupZone: testQuote.pickupZone,
  demand: testQuote.demand,
  categories: testQuote.categories,
  corporate: check,
);

void main() {
  late FakeCorporateRepository corporate;
  late FakePricingRepository pricing;
  late FakeTripRepository trips;

  setUp(() async {
    await registerTestDependencies(auth: FakeAuthRepository(restored: _rider));
    corporate = getIt<CorporateRepository>() as FakeCorporateRepository;
    pricing = getIt<PricingRepository>() as FakePricingRepository;
    trips = getIt<TripRepository>() as FakeTripRepository;
  });
  tearDown(getIt.reset);

  Future<void> openHome(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 3400));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(buildTestApp());
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(find.byType(HomePage), findsOneWidget);
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 2));
  }

  Future<void> openPaymentSheet(WidgetTester tester) async {
    await tester.tap(find.byType(PaymentRow));
    await tester.pumpAndSettle();
  }

  Future<void> chooseCorporate(WidgetTester tester) async {
    await openPaymentSheet(tester);
    await tester.tap(find.byKey(const ValueKey<String>('payment-corporate')));
    await tester.pumpAndSettle();
  }

  AtaButton requestButton(WidgetTester tester) => tester
      .widgetList<AtaButton>(find.byType(AtaButton))
      .firstWhere((AtaButton b) => b.label.startsWith('اطلب'));

  Future<void> tapRequest(WidgetTester tester) async {
    final Finder button = find.byWidget(requestButton(tester));
    await tester.ensureVisible(button);
    await tester.tap(button);
  }

  Finder purposeField() =>
      find.byKey(const ValueKey<String>('corporate-purpose'));

  testWidgets('a rider without a company does not see the company account', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    await openPaymentSheet(tester);
    expect(find.text('نقداً'), findsWidgets);
    expect(find.text('حساب الشركة'), findsNothing);
    expect(
      find.byKey(const ValueKey<String>('payment-corporate')),
      findsNothing,
    );
    await leave(tester);
  });

  testWidgets('a pending or disabled membership is not offered either', (
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
    );
    await openHome(tester);
    await openPaymentSheet(tester);
    expect(
      find.byKey(const ValueKey<String>('payment-corporate')),
      findsNothing,
    );
    await leave(tester);
  });

  testWidgets('an active employee sees the company, the budget left and the '
      'per-trip limit', (WidgetTester tester) async {
    corporate.profile = testCorporateProfile;
    await openHome(tester);
    await openPaymentSheet(tester);
    expect(find.text('حساب الشركة'), findsOneWidget);
    expect(find.text('شركة المثال'), findsOneWidget);
    expect(find.text('المتبقي هذا الشهر: 1,079.5 ر.س'), findsOneWidget);
    expect(find.text('الحد الأقصى للرحلة: 150 ر.س'), findsOneWidget);
    await leave(tester);
  });

  testWidgets('choosing it asks for the purpose and cost center, explains '
      'why promo and favourites are off and sends them with the request', (
    WidgetTester tester,
  ) async {
    corporate.profile = testCorporateProfile;
    await openHome(tester);
    expect(
      find.byKey(const ValueKey<String>('corporate-section')),
      findsNothing,
    );

    await chooseCorporate(tester);
    expect(
      find.byKey(const ValueKey<String>('corporate-section')),
      findsOneWidget,
    );
    expect(find.text('لا تُطبّق أكواد الخصم مع حساب الشركة'), findsOneWidget);
    expect(
      find.text('لا يُطبّق خصم السائق المفضل مع حساب الشركة'),
      findsOneWidget,
    );
    expect(find.text('غرض الرحلة *'), findsOneWidget);
    expect(find.text('غرض الرحلة مطلوب'), findsOneWidget);
    expect(find.text('IT-01 · تقنية المعلومات'), findsOneWidget);
    // The remaining budget and the limit are repeated in the form.
    expect(
      find.text('المتبقي من ميزانيتك الشهرية: 1,079.5 ر.س'),
      findsOneWidget,
    );
    // The quote is re-priced with the company policy evaluation.
    await tester.pump(const Duration(seconds: 1));
    expect(pricing.requests.last.paymentMethod, 'corporate');

    // Not requestable until the purpose is filled in.
    expect(requestButton(tester).onPressed, isNull);
    await tester.enterText(purposeField(), 'اجتماع عميل');
    await tester.pump();
    expect(find.text('غرض الرحلة مطلوب'), findsNothing);
    expect(requestButton(tester).onPressed, isNotNull);

    await tester.tap(find.byKey(const ValueKey<String>('cost-center-cc1')));
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    expect(pricing.requests.last.costCenterId, 'cc1');

    await tapRequest(tester);
    await tester.pump();
    expect(trips.requests, hasLength(1));
    expect(trips.requests.single.paymentMethod, 'corporate');
    expect(trips.requests.single.tripPurpose, 'اجتماع عميل');
    expect(trips.requests.single.costCenterId, 'cc1');
    expect(trips.requests.single.promoCode, isNull);
    await leave(tester);
  });

  testWidgets('a category the policy does not allow is disabled with the '
      'reason and the allowed categories', (WidgetTester tester) async {
    corporate.profile = CorporateProfile(
      membership: testCorporateProfile.membership,
      policy: const CorporatePolicySummary(
        allowedRideCategoryCodes: <String>['family'],
      ),
    );
    await openHome(tester);
    await openPaymentSheet(tester);
    expect(
      find.text(
        'غير متاح: هذه الفئة غير مسموحة في سياسة شركتك '
        '(الفئات المسموحة: عائلي)',
      ),
      findsOneWidget,
    );
    await tester.tap(find.byKey(const ValueKey<String>('payment-corporate')));
    await tester.pumpAndSettle();
    // Still open, nothing selected.
    expect(
      find.byKey(const ValueKey<String>('payment-corporate')),
      findsOneWidget,
    );
    expect(
      find.byKey(const ValueKey<String>('corporate-section')),
      findsNothing,
    );
    await leave(tester);
  });

  testWidgets('violations from the quote are listed with the allowed options '
      'and block the request', (WidgetTester tester) async {
    corporate.profile = testCorporateProfile;
    pricing.nextQuote = _quoteWith(
      const CorporateQuoteCheck(
        allowed: false,
        violations: <PolicyViolation>[
          PolicyViolation(
            rule: PolicyRule.zone,
            allowed: <String>['شمال الرياض'],
          ),
        ],
        remainingBudget: 79.5,
      ),
    );
    await openHome(tester);
    await chooseCorporate(tester);
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    await tester.enterText(purposeField(), 'زيارة');
    await tester.pump();

    expect(
      find.byKey(const ValueKey<String>('corporate-violations')),
      findsOneWidget,
    );
    expect(find.text('هذه الرحلة لا تتوافق مع سياسة شركتك'), findsOneWidget);
    expect(find.text('منطقة الالتقاط أو الوجهة غير مسموحة'), findsOneWidget);
    expect(find.text('المناطق المسموحة: شمال الرياض'), findsOneWidget);
    expect(find.text('المتبقي من ميزانيتك الشهرية: 79.5 ر.س'), findsOneWidget);
    expect(requestButton(tester).onPressed, isNull);
    await leave(tester);
  });

  testWidgets('a policy refusal of the request shows the broken rules', (
    WidgetTester tester,
  ) async {
    corporate.profile = testCorporateProfile;
    trips.requestFailure = const ServerFailure(
      code: 'corporate_policy_violation',
      message: '',
      statusCode: 422,
      details: <String, dynamic>{
        'violations': <dynamic>[
          <String, dynamic>{'rule': 'max_fare', 'limit': 150},
        ],
      },
    );
    await openHome(tester);
    await chooseCorporate(tester);
    await tester.enterText(purposeField(), 'زيارة');
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    await tapRequest(tester);
    await tester.pumpAndSettle();
    expect(
      find.text('الأجرة تتجاوز الحد الأقصى للرحلة (150 ر.س)'),
      findsOneWidget,
    );
    await leave(tester);
  });

  testWidgets('losing the membership goes back to cash and explains', (
    WidgetTester tester,
  ) async {
    corporate.profile = testCorporateProfile;
    trips.requestFailure = const ServerFailure(
      code: 'corporate_account_inactive',
      message: '',
      statusCode: 403,
    );
    await openHome(tester);
    await chooseCorporate(tester);
    await tester.enterText(purposeField(), 'زيارة');
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    // The company suspended the account meanwhile.
    corporate.profile = null;
    await tapRequest(tester);
    await tester.pumpAndSettle();
    expect(find.text('حساب الشركة غير نشط'), findsOneWidget);
    expect(
      find.byKey(const ValueKey<String>('corporate-section')),
      findsNothing,
    );
    await openPaymentSheet(tester);
    expect(
      find.byKey(const ValueKey<String>('payment-corporate')),
      findsNothing,
    );
    await leave(tester);
  });
}
