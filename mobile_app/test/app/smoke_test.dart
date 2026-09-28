import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/widgets/keypad.dart';
import 'package:ata_app/design/widgets/otp_boxes.dart';
import 'package:ata_app/features/auth/presentation/pages/language_page.dart';
import 'package:ata_app/features/auth/presentation/pages/phone_page.dart';
import 'package:ata_app/features/auth/presentation/pages/role_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../helpers/test_app.dart';

void main() {
  setUp(registerTestDependencies);
  tearDown(getIt.reset);

  testWidgets('language -> role -> phone -> otp', (WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(430, 932));
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    expect(find.byType(LanguagePage), findsOneWidget);
    expect(find.text('اختر لغتك · Choose your language'), findsOneWidget);

    await tester.tap(find.text('متابعة بالعربية'));
    await tester.pumpAndSettle();
    expect(find.byType(RolePage), findsOneWidget);
    expect(find.text('كيف تريد استخدام التطبيق؟'), findsOneWidget);

    await tester.tap(find.text('التسجيل كعميل'));
    await tester.pumpAndSettle();
    expect(find.byType(PhonePage), findsOneWidget);
    expect(find.text('أدخل رقم جوالك'), findsOneWidget);
    expect(find.byType(Keypad), findsOneWidget);

    for (final String digit in '512345678'.split('')) {
      await tester.tap(
        find.descendant(of: find.byType(Keypad), matching: find.text(digit)),
      );
    }
    await tester.pumpAndSettle();
    await tester.tap(find.text('إرسال رمز التحقق'));
    await tester.pumpAndSettle();

    expect(find.byType(OtpBoxes), findsOneWidget);
    expect(find.text('تحقق من رقمك'), findsOneWidget);
    expect(find.text('رمز التطوير: 1234'), findsOneWidget);
  });

  testWidgets('English keeps the same flow in LTR', (
    WidgetTester tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(430, 932));
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    await tester.ensureVisible(find.text('Continue in English'));
    await tester.tap(find.text('Continue in English'));
    await tester.pumpAndSettle();
    expect(find.text('How would you like to use ATA?'), findsOneWidget);
  });
}
