import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_types.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const ParseDeepLink parse = ParseDeepLink();

  String? rider(String link) =>
      parse(DeepLinkParams(link: link, isDriver: false))?.route;

  String? driver(String link) =>
      parse(DeepLinkParams(link: link, isDriver: true))?.route;

  test('F15 links map to the shipped routes (docs/08 §F13.7)', () {
    expect(rider('ata://rate/t1'), AppRoutes.rateTrip('t1'));
    expect(driver('ata://rate/t1'), AppRoutes.driverRateTrip('t1'));
    expect(rider('ata://promotions'), AppRoutes.promotions);
    expect(driver('ata://driver/tier'), AppRoutes.driverTier);
    expect(
      driver('ata://driver/incentives/i1'),
      AppRoutes.driverIncentive('i1'),
    );
    expect(driver('ata://driver/incentives'), AppRoutes.driverIncentives);
    expect(driver('ata://driver/ratings'), AppRoutes.driverRatings);
    // Role mismatch falls back to the role's home.
    expect(driver('ata://promotions'), AppRoutes.driver);
    expect(rider('ata://driver/tier'), AppRoutes.home);
  });

  test('rows without data.deepLink get a derived F15 link', () {
    expect(
      NotificationTypes.fallbackLink('rating.reminder', <String, dynamic>{
        'tripId': 't1',
      }),
      'ata://rate/t1',
    );
    expect(
      NotificationTypes.fallbackLink('promo.new', null),
      'ata://promotions',
    );
    expect(
      NotificationTypes.fallbackLink('incentive.achieved', <String, dynamic>{
        'incentiveId': 'i1',
      }),
      'ata://driver/incentives/i1',
    );
    expect(
      NotificationTypes.fallbackLink('incentive.new', null),
      'ata://driver/incentives',
    );
    expect(
      NotificationTypes.fallbackLink('driver.tier_changed', null),
      'ata://driver/tier',
    );
    expect(NotificationTypes.categoryOf('rating.reminder'), 'trips');
    expect(NotificationTypes.categoryOf('promo.new'), 'promotions');
    expect(NotificationTypes.categoryOf('incentive.new'), 'promotions');
    expect(NotificationTypes.categoryOf('incentive.achieved'), 'wallet');
  });

  test('the promotions page opens the home sheet with the code', () {
    expect(AppRoutes.homeWithPromo('ATA10'), '/home?promo=ATA10');
  });
}
