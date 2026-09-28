import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/driver_earnings_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/driver_payouts_page.dart';
import 'package:ata_app/features/driver_wallet/presentation/pages/payout_request_page.dart';
import 'package:ata_app/features/payments/data/models/receipt_model.dart';
import 'package:ata_app/features/payments/data/tokenizers/sandbox_card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/repositories/card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:ata_app/features/payments/presentation/pages/add_card_page.dart';
import 'package:ata_app/features/payments/presentation/pages/payment_methods_page.dart';
import 'package:ata_app/features/payments/presentation/pages/receipt_page.dart';
import 'package:ata_app/features/wallet/presentation/pages/top_up_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/driver_wallet_fakes.dart';
import '../../helpers/payments_fakes.dart';
import '../../helpers/test_app.dart';
import 'receipt_test.dart' show receiptJson;

void main() {
  late FakePaymentsRepository payments;

  setUp(() async {
    await registerTestDependencies();
    payments = FakePaymentsRepository();
    getIt
      ..registerSingleton<PaymentsRepository>(payments)
      ..registerSingleton<CardTokenizer>(const SandboxCardTokenizer())
      ..registerSingleton<DriverWalletRepository>(FakeDriverWalletRepository());
  });

  tearDown(getIt.reset);

  Future<void> pump(WidgetTester tester, Widget page) async {
    await tester.binding.setSurfaceSize(const Size(430, 1400));
    await tester.pumpWidget(wrapForTest(page));
    await tester.pumpAndSettle();
  }

  testWidgets('receipt page shows lines, discount source and fallback', (
    WidgetTester tester,
  ) async {
    payments.receipt = ReceiptModel.fromJson(receiptJson);
    await pump(tester, const ReceiptPage(tripId: 't1'));
    expect(find.text('خصم ATA10'), findsOneWidget);
    expect(find.text('عرض ترويجي'), findsOneWidget);
    expect(find.text('مدى •••• 4201'), findsOneWidget);
    expect(
      find.text('تعذّر الدفع بالبطاقة، فتم تحويل الرحلة إلى الدفع نقداً'),
      findsOneWidget,
    );
  });

  testWidgets('receipt page explains a missing receipt', (tester) async {
    await pump(tester, const ReceiptPage(tripId: 't2'));
    expect(find.text('لا يتوفر إيصال لهذه الرحلة'), findsOneWidget);
  });

  testWidgets('payment methods list the saved cards', (tester) async {
    await pump(tester, const PaymentMethodsPage());
    expect(find.text('مدى •••• 4201'), findsOneWidget);
    expect(find.text('فيزا •••• 1111'), findsOneWidget);
    expect(find.text('الافتراضية'), findsOneWidget);
  });

  testWidgets('add card validates before tokenising', (tester) async {
    await pump(tester, const AddCardPage());
    await tester.tap(find.text('حفظ البطاقة'));
    await tester.pumpAndSettle();
    expect(find.text('رقم البطاقة غير صحيح'), findsOneWidget);
    expect(payments.tokens, isEmpty);
  });

  testWidgets('top-up offers saved cards and the sandbox', (tester) async {
    await pump(tester, const TopUpPage());
    expect(find.text('مدى •••• 4201'), findsOneWidget);
    expect(find.text('إضافة بطاقة'), findsOneWidget);
  });

  testWidgets('driver payouts show the cash debt and history', (tester) async {
    await pump(tester, const DriverPayoutsPage());
    expect(find.text('مستحقات النقد'), findsOneWidget);
    expect(find.text('PO-20260928-00012'), findsOneWidget);
    expect(find.text('قيد المراجعة'), findsOneWidget);
  });

  testWidgets('payout request shows the IBAN and validates', (tester) async {
    await pump(tester, const PayoutRequestPage());
    expect(find.text('SA03 **** **** 1234'), findsOneWidget);
    await tester.enterText(find.byType(TextFormField), '50');
    await tester.pumpAndSettle();
    expect(
      find.text('المبلغ أقل من الحد الأدنى للسحب (100 ر.س)'),
      findsOneWidget,
    );
  });

  testWidgets('earnings statement switches periods', (tester) async {
    await pump(tester, const DriverEarningsPage());
    expect(find.text('الصافي'), findsOneWidget);
    await tester.tap(find.text('الشهر'));
    await tester.pumpAndSettle();
    expect(find.text('عمولة المنصة'), findsOneWidget);
  });
}
