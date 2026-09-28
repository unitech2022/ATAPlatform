import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const ParseDeepLink parse = ParseDeepLink();

  String? rider(String link, {bool activeTrip = false}) => parse(
    DeepLinkParams(link: link, isDriver: false, hasActiveTrip: activeTrip),
  )?.route;

  String? driver(String link) =>
      parse(DeepLinkParams(link: link, isDriver: true))?.route;

  group('passenger links (docs/08 §F13.7)', () {
    const Map<String, String> table = <String, String>{
      'ata://home': '/home',
      'ata://trip/t1': '/rides/t1',
      'ata://trip/t1/chat': '/trip/chat',
      'ata://rides/t1': '/rides/t1',
      'ata://rides/t1/receipt': '/rides/t1/receipt',
      'ata://rate/t1': '/rate/t1',
      'ata://scheduled/t1': '/scheduled/t1',
      'ata://wallet': '/wallet',
      'ata://wallet/transactions': '/wallet/transactions',
      'ata://promotions': '/promotions',
      'ata://reliability': '/account/reliability',
      'ata://safety/check/a1': '/safety/check/a1',
      'ata://safety/cases/c1': '/safety/cases/c1',
      'ata://support/tickets/k1': '/support/tickets/k1',
      'ata://corporate/invitations': '/account/corporate',
      'ata://account': '/account',
      'ata://account/privacy': '/account/privacy',
    };
    for (final MapEntry<String, String> row in table.entries) {
      test('${row.key} → ${row.value}', () {
        expect(rider(row.key), row.value);
      });
    }

    test('an active trip opens /trip', () {
      expect(rider('ata://trip/t1', activeTrip: true), '/trip');
    });
  });

  group('driver links', () {
    const Map<String, String> table = <String, String>{
      'ata://driver': '/driver',
      'ata://driver/pending': '/driver/pending',
      'ata://driver/offer': '/driver/offer',
      'ata://driver/trip': '/driver/trip',
      'ata://driver/documents': '/driver?tab=documents',
      'ata://driver/earnings': '/driver/earnings',
      'ata://driver/payouts': '/driver/payouts',
      'ata://driver/settlements/s1': '/driver/settlements/s1',
      'ata://driver/tier': '/driver/tier',
      'ata://driver/incentives/i1': '/driver/incentives/i1',
      'ata://driver/scheduled': '/driver/scheduled',
      'ata://driver/scheduled/t1': '/driver/scheduled/t1',
      'ata://driver/lost-items/l1': '/driver/lost-items/l1',
      'ata://trip/t1': '/driver/trip',
      'ata://trip/t1/chat': '/driver/trip/chat',
      'ata://reliability': '/driver/reliability',
    };
    for (final MapEntry<String, String> row in table.entries) {
      test('${row.key} → ${row.value}', () {
        expect(driver(row.key), row.value);
      });
    }
  });

  group('fallbacks', () {
    test('unknown links go to the role home', () {
      expect(rider('ata://nowhere'), '/home');
      expect(driver('ata://nowhere/else'), '/driver');
      expect(rider('https://example.com/x'), '/home');
      expect(rider('not a link'), '/home');
    });

    test('role mismatches go to the role home', () {
      expect(rider('ata://driver/payouts'), '/home');
      expect(driver('ata://wallet'), '/driver');
      expect(driver('ata://rides/t1/receipt'), '/driver');
    });

    test('ata://notifications opens the inbox', () {
      expect(
        parse(
          const DeepLinkParams(link: 'ata://notifications', isDriver: false),
        ),
        const DeepLink(route: '/home', opensInbox: true),
      );
    });

    test('ata://payments/return is left to the payment flow', () {
      expect(rider('ata://payments/return?paymentId=p1'), isNull);
    });
  });

  group('notification rows', () {
    NotificationItem item(String type, [Map<String, dynamic>? data]) =>
        NotificationItem(
          id: 'n1',
          type: type,
          title: '',
          body: '',
          createdAt: DateTime(2026),
          data: data,
        );

    test('data.deepLink wins', () {
      expect(
        item('trip.completed', <String, dynamic>{
          'deepLink': 'ata://rides/t9/receipt',
        }).deepLink,
        'ata://rides/t9/receipt',
      );
    });

    test('legacy types are normalized and get a link', () {
      final NotificationItem legacy = item('trip_completed', <String, dynamic>{
        'tripId': 't2',
      });
      expect(legacy.eventCode, 'trip.completed');
      expect(legacy.resolvedCategory, 'trips');
      expect(legacy.deepLink, 'ata://rides/t2/receipt');
      expect(item('driver_application_approved').deepLink, 'ata://driver');
      expect(item('payment.failed').resolvedCategory, 'wallet');
    });
  });
}
