import 'package:ata_app/design/widgets/keypad.dart';
import 'package:ata_app/design/widgets/otp_boxes.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../helpers/test_app.dart';

void main() {
  testWidgets('keypad reports digits and delete taps', (
    WidgetTester tester,
  ) async {
    final List<String> digits = <String>[];
    int deletes = 0;
    await tester.pumpWidget(
      wrapForTest(
        Keypad(
          onDigit: digits.add,
          onDelete: () => deletes++,
          deleteLabel: 'حذف',
        ),
      ),
    );

    await tester.tap(find.text('5'));
    await tester.tap(find.text('0'));
    await tester.tap(find.text('حذف'));
    await tester.pump();

    expect(digits, <String>['5', '0']);
    expect(deletes, 1);
    expect(find.text('9'), findsOneWidget);
  });

  testWidgets('disabled keypad ignores taps', (WidgetTester tester) async {
    final List<String> digits = <String>[];
    await tester.pumpWidget(
      wrapForTest(
        Keypad(
          onDigit: digits.add,
          onDelete: () {},
          deleteLabel: 'حذف',
          enabled: false,
        ),
      ),
    );
    await tester.tap(find.text('1'));
    expect(digits, isEmpty);
  });

  testWidgets('otp boxes render four cells and fill typed digits', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(wrapForTest(const OtpBoxes(code: '12')));
    expect(find.text('1'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    final Iterable<AnimatedContainer> boxes = tester
        .widgetList<AnimatedContainer>(find.byType(AnimatedContainer));
    expect(boxes.length, 4);
  });
}
