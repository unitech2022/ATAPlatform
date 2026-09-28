/// Event codes of the notification catalog (`docs/08` §F13.2) and the
/// client-side compatibility with the legacy `snake_case` types.
abstract final class NotificationTypes {
  static const String tripCompleted = 'trip.completed';
  static const String tripNoDrivers = 'trip.no_drivers';
  static const String applicationApproved = 'driver.application.approved';

  static const Map<String, String> _legacy = <String, String>{
    'driver_application_approved': 'driver.application.approved',
    'driver_application_rejected': 'driver.application.rejected',
    'driver_application_under_review': 'driver.application.under_review',
    'driver_suspended': 'driver.suspended',
    'driver_reinstated': 'driver.reinstated',
    'trip_driver_assigned': 'trip.driver_assigned',
    'trip_driver_arrived': 'trip.driver_arrived',
    'trip_completed': 'trip.completed',
    'trip_cancelled': 'trip.cancelled',
    'trip_no_drivers': 'trip.no_drivers',
  };

  /// Maps a legacy type to its catalog code; new codes pass through.
  static String normalize(String type) => _legacy[type] ?? type;

  /// Category of a code (`trips`, `wallet`, `safety`, `promotions`,
  /// `system`), used when the API row has none.
  static String categoryOf(String code) {
    final String prefix = code.split('.').first;
    return switch (prefix) {
      'trip' || 'scheduled' || 'rating' || 'lost_item' => 'trips',
      'offer' => 'offers',
      'payment' ||
      'wallet' ||
      'payout' ||
      'settlement' ||
      'cancellation' => 'wallet',
      'safety' => 'safety',
      'promo' || 'incentive' => 'promotions',
      _ => 'system',
    };
  }

  /// Deep link for rows created before `data.deepLink` existed.
  static String? fallbackLink(String code, Map<String, dynamic>? data) {
    final String? tripId = data?['tripId']?.toString();
    if (code == tripCompleted && tripId != null) {
      return 'ata://rides/$tripId/receipt';
    }
    if (code == tripNoDrivers) return 'ata://home';
    if (code.startsWith('trip.') && tripId != null) return 'ata://trip/$tripId';
    if (code == applicationApproved) return 'ata://driver';
    if (code.startsWith('driver.application.')) return 'ata://driver/pending';
    if (code.startsWith('driver.')) return 'ata://driver';
    if (code.startsWith('payment.') || code.startsWith('wallet.')) {
      return 'ata://wallet';
    }
    return null;
  }
}
