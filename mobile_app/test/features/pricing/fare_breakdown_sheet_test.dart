import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/widgets/fare_breakdown_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/pricing_fakes.dart';
import '../../helpers/test_app.dart';

void main() {
  testWidgets('shows every applicable line, the demand chip and the total', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      wrapForTest(
        FareBreakdownSheet(quote: testQuote, category: testQuoteCategory),
      ),
    );

    expect(find.text('تفاصيل السعر'), findsOneWidget);
    expect(find.text('كيف حُسب سعر اقتصادي'), findsOneWidget);
    expect(find.text('التعرفة الأساسية'), findsOneWidget);
    expect(find.text('8 ر.س'), findsOneWidget);
    expect(find.text('المسافة (12 كم)'), findsOneWidget);
    expect(find.text('الوقت (20 دقيقة)'), findsOneWidget);
    expect(find.text('مضاعِف الطلب'), findsOneWidget);
    expect(find.text('×1.5'), findsOneWidget);
    expect(find.text('الطلب مرتفع الآن ×1.5'), findsOneWidget);
    expect(find.text('رسوم الحجز'), findsOneWidget);
    expect(find.text('رسوم الخدمة'), findsOneWidget);
    expect(find.text('الإجمالي'), findsOneWidget);
    expect(find.text('42 ر.س'), findsOneWidget);
    // Not applicable for this quote, and never shown to passengers.
    expect(find.text('مضاعِف الوقت'), findsNothing);
    expect(find.text('الخصم'), findsNothing);
    expect(find.text('تم تطبيق الحد الأدنى للتعرفة'), findsNothing);
    expect(find.text('31.5 ر.س'), findsNothing);
  });

  testWidgets('min fare, time multiplier and discount rows appear when set', (
    WidgetTester tester,
  ) async {
    const QuoteCategory category = QuoteCategory(
      rideCategoryId: 'c1',
      code: 'economy',
      name: 'اقتصادي',
      etaMinutes: 4,
      total: 25,
      offerMin: 17.5,
      offerMax: 32.5,
      breakdown: FareBreakdown(
        baseFare: 8,
        distanceFare: 5,
        timeFare: 2,
        minFareApplied: true,
        timeMultiplier: 1.2,
        bookingFee: 2,
        serviceFee: 1.5,
        discount: 5,
      ),
    );
    await tester.pumpWidget(
      wrapForTest(
        FareBreakdownSheet(quote: testQuote, category: category),
        locale: const Locale('en'),
      ),
    );

    expect(find.text('Minimum fare applied'), findsOneWidget);
    expect(find.text('Time multiplier'), findsOneWidget);
    expect(find.text('×1.2'), findsOneWidget);
    expect(find.text('Discount'), findsOneWidget);
    expect(find.text('- 5 SAR'), findsOneWidget);
    expect(find.text('Total'), findsOneWidget);
    expect(find.text('25 SAR'), findsOneWidget);
  });
}
