import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/offered_price_row.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/pricing_fakes.dart';
import '../../helpers/rewards_fakes.dart';
import '../../helpers/test_app.dart';

/// Signed-in rider who accepted the terms (the router opens /home).
final AuthSession _rider = testSession.copyWith(
  user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
);

void main() {
  setUp(() async {
    await registerTestDependencies(auth: FakeAuthRepository(restored: _rider));
    (getIt<RatingRepository>() as FakeRatingRepository).pending =
        <PendingRating>[
          PendingRating(
            tripId: 't9',
            counterpartName: 'محمد',
            rateUntil: DateTime.now().add(const Duration(hours: 10)),
          ),
        ];
  });
  tearDown(getIt.reset);

  Future<void> openHome(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(900, 2600));
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

  testWidgets('pending-rating prompt on app open is dismissible', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    expect(find.text('قيّم رحلتك الأخيرة'), findsOneWidget);
    expect(find.text('كيف كانت رحلتك مع محمد؟'), findsOneWidget);

    await tester.tap(find.byTooltip('لاحقاً'));
    await tester.pumpAndSettle();
    expect(find.text('قيّم رحلتك الأخيرة'), findsNothing);
    await leave(tester);
  });

  testWidgets('promo code: enter, validate, re-quote with promoCode, remove', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    final FakePricingRepository pricing =
        getIt<PricingRepository>() as FakePricingRepository;
    expect(find.text('كود خصم'), findsOneWidget);
    expect(find.text('أضف كود الخصم إن كان لديك'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey<String>('promo-row')));
    await tester.pumpAndSettle();
    expect(find.text('كود الخصم'), findsOneWidget);
    await tester.enterText(find.byType(TextFormField), 'ata10');
    await tester.pump();
    await tester.tap(find.text('تطبيق'));
    await tester.pumpAndSettle();

    // The sheet closed, the row shows the applied code and the quote was
    // requested again with the code.
    expect(find.text('تطبيق'), findsNothing);
    expect(find.textContaining('ATA10 مطبّق'), findsOneWidget);
    await tester.pump(const Duration(seconds: 1));
    expect(pricing.requests.last.promoCode, 'ATA10');

    await tester.tap(find.text('إزالة'));
    await tester.pumpAndSettle();
    expect(find.text('أضف كود الخصم إن كان لديك'), findsOneWidget);
    await tester.pump(const Duration(seconds: 1));
    expect(pricing.requests.last.promoCode, isNull);
    await leave(tester);
  });

  testWidgets('offering a price hides the promo code', (
    WidgetTester tester,
  ) async {
    await openHome(tester);
    await tester.tap(
      find.descendant(
        of: find.byType(OfferedPriceRow),
        matching: find.byType(AtaToggle),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('لا ينطبق الخصم مع اقتراح السعر'), findsOneWidget);
    await leave(tester);
  });
}
