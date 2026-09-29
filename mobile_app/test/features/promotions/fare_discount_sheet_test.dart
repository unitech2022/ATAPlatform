import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/widgets/fare_breakdown_sheet.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/pricing_fakes.dart';
import '../../helpers/test_app.dart';

const QuoteCategory _discounted = QuoteCategory(
  rideCategoryId: 'c1',
  code: 'economy',
  name: 'اقتصادي',
  etaMinutes: 4,
  total: 37,
  offerMin: 29.5,
  offerMax: 54.5,
  breakdown: FareBreakdown(
    baseFare: 8,
    distanceFare: 18,
    timeFare: 6,
    bookingFee: 2,
    serviceFee: 4,
    discount: 5,
    discounts: <FareDiscount>[
      FareDiscount(
        source: DiscountSource.promotion,
        reference: 'ATA10',
        label: 'خصم ATA10',
        amount: 5,
      ),
    ],
  ),
);

void main() {
  testWidgets('the fare breakdown shows the discount line with its source '
      'and the price before the discount', (WidgetTester tester) async {
    await tester.pumpWidget(
      wrapForTest(FareBreakdownSheet(quote: testQuote, category: _discounted)),
    );
    expect(find.text('خصم ATA10 · عرض ترويجي'), findsOneWidget);
    expect(find.text('- 5 ر.س'), findsOneWidget);
    expect(find.text('قبل الخصم'), findsOneWidget);
    expect(find.text('42 ر.س'), findsOneWidget);
    expect(find.text('37 ر.س'), findsOneWidget);
    expect(find.text('الخصم'), findsNothing);
  });
}
