import 'dart:developer' as developer;

import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';

/// Input of [ParseDeepLink].
class DeepLinkParams {
  const DeepLinkParams({
    required this.link,
    required this.isDriver,
    this.hasActiveTrip = false,
  });

  final String link;
  final bool isDriver;

  /// A passenger trip is in progress (`ata://trip/{id}` then opens `/trip`).
  final bool hasActiveTrip;
}

/// Maps an `ata://` link to a go_router location following the table of
/// `docs/08` §F13.7. Unknown links and role mismatches fall back to the
/// role's home (`/home` or `/driver`); `ata://payments/return` is handled
/// by the payment flow and yields `null`.
class ParseDeepLink {
  const ParseDeepLink();

  static const String scheme = 'ata';
  static const String _home = '/home';
  static const String _driver = '/driver';

  DeepLink? call(DeepLinkParams params) {
    final Uri? uri = Uri.tryParse(params.link.trim());
    if (uri == null || uri.scheme != scheme || uri.host.isEmpty) {
      return _fallback(params);
    }
    final List<String> s = <String>[
      uri.host,
      ...uri.pathSegments.where((String e) => e.isNotEmpty),
    ];
    if (s.first == 'payments') return null;
    if (s.first == 'notifications' && s.length == 1) {
      return DeepLink(
        route: params.isDriver ? _driver : _home,
        opensInbox: true,
      );
    }
    final String? route = _route(s, params);
    if (route == null) return _fallback(params);
    final bool driverRoute = route == _driver || route.startsWith('$_driver/');
    final bool driverQuery = route.startsWith('$_driver?');
    if (params.isDriver != (driverRoute || driverQuery)) {
      return _fallback(params);
    }
    return DeepLink(route: route);
  }

  String? _route(List<String> s, DeepLinkParams p) {
    final String head = s.first;
    final int n = s.length;
    final String? id = n > 1 ? s[1] : null;
    switch (head) {
      case 'home' when n == 1:
        return _home;
      case 'trip' when n == 2:
        if (p.isDriver) return '$_driver/trip';
        return p.hasActiveTrip ? '/trip' : '/rides/$id';
      case 'trip' when n == 3 && s[2] == 'chat':
        return p.isDriver ? '$_driver/trip/chat' : '/trip/chat';
      case 'rides' when n == 2:
        return '/rides/$id';
      case 'rides' when n == 3 && s[2] == 'receipt':
        return '/rides/$id/receipt';
      case 'rate' when n == 2:
        return p.isDriver ? '$_driver/rate/$id' : '/rate/$id';
      case 'scheduled' when n == 1:
        return p.isDriver ? '$_driver/scheduled' : '/scheduled';
      case 'scheduled' when n == 2:
        // `scheduled.reminder` / `reservation_released` reach both roles.
        return p.isDriver ? '$_driver/scheduled/$id' : '/scheduled/$id';
      case 'wallet' when n == 1:
        return '/wallet';
      case 'wallet' when n == 2 && id == 'transactions':
        return '/wallet/transactions';
      case 'promotions' when n == 1:
        return '/promotions';
      case 'reliability' when n == 1:
        return p.isDriver ? '$_driver/reliability' : '/account/reliability';
      case 'safety' when n == 3 && (id == 'check' || id == 'cases'):
        return '/safety/$id/${s[2]}';
      case 'support' when n == 3 && id == 'tickets':
        return '/support/tickets/${s[2]}';
      case 'corporate' when n == 2 && id == 'invitations':
        return '/account/corporate';
      case 'account' when n == 1:
        return '/account';
      case 'account' when n == 2 && id == 'privacy':
        return '/account/privacy';
      case 'driver':
        return _driverRoute(s);
    }
    return null;
  }

  static const Set<String> _driverSingles = <String>{
    'pending',
    'offer',
    'trip',
    'earnings',
    'payouts',
    'tier',
    'incentives',
    'ratings',
    'scheduled',
    'airport-queue',
  };
  static const Set<String> _driverWithId = <String>{
    'settlements',
    'incentives',
    'scheduled',
    'lost-items',
  };

  String? _driverRoute(List<String> s) {
    if (s.length == 1) return _driver;
    final String section = s[1];
    if (s.length == 2 && section == 'documents') {
      return '$_driver?tab=documents';
    }
    if (s.length == 2 && _driverSingles.contains(section)) {
      return '$_driver/$section';
    }
    if (s.length == 3 && _driverWithId.contains(section)) {
      return '$_driver/$section/${s[2]}';
    }
    return null;
  }

  DeepLink _fallback(DeepLinkParams params) {
    developer.log('Unknown deep link: ${params.link}', name: 'deeplink');
    return DeepLink(route: params.isDriver ? _driver : _home);
  }
}
