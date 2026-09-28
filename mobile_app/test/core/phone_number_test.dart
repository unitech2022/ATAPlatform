import 'package:ata_app/core/utils/phone_number.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PhoneNumber.normalize', () {
    test('accepts local, national and international forms', () {
      expect(PhoneNumber.normalize('0512345678'), '+966512345678');
      expect(PhoneNumber.normalize('512345678'), '+966512345678');
      expect(PhoneNumber.normalize('+966512345678'), '+966512345678');
      expect(PhoneNumber.normalize('00966 51 234 5678'), '+966512345678');
    });

    test('rejects numbers that are not Saudi mobiles', () {
      expect(PhoneNumber.normalize('412345678'), isNull);
      expect(PhoneNumber.normalize('51234567'), isNull);
      expect(PhoneNumber.normalize('5123456789'), isNull);
      expect(PhoneNumber.normalize(''), isNull);
    });
  });

  group('PhoneNumber.isValidLocal', () {
    test('requires nine digits starting with 5', () {
      expect(PhoneNumber.isValidLocal('512345678'), isTrue);
      expect(PhoneNumber.isValidLocal('51234567'), isFalse);
      expect(PhoneNumber.isValidLocal('612345678'), isFalse);
      expect(PhoneNumber.isValidLocal('51234567a'), isFalse);
    });
  });

  test('grouped formats like the placeholder', () {
    expect(PhoneNumber.grouped('512345678'), '51 234 5678');
    expect(PhoneNumber.grouped('51'), '51');
  });
}
