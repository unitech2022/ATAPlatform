/// Event codes of the notification catalog (`docs/08` §F13.2) and the
/// client-side compatibility with the legacy `snake_case` types.
abstract final class NotificationTypes {
  static const String tripCompleted = 'trip.completed';
  static const String tripNoDrivers = 'trip.no_drivers';

  /// F16: the favourite captain did not answer, the search moved on.
  static const String tripFavoriteFallback = 'trip.favorite_fallback';
  static const String applicationApproved = 'driver.application.approved';
  static const String ratingReminder = 'rating.reminder';
  static const String promoNew = 'promo.new';
  static const String incentivePrefix = 'incentive.';
  static const String incentiveAchieved = 'incentive.achieved';
  static const String tierChanged = 'driver.tier_changed';

  /// F17 scheduled rides (`scheduled.booked`, `.reminder`, `.driver_reserved`,
  /// `.confirm_request`, `.reservation_released`, `.favorite_request`,
  /// `.rematched`).
  static const String scheduledPrefix = 'scheduled.';
  static const String scheduledConfirmRequest = 'scheduled.confirm_request';
  static const String scheduledFavoriteRequest = 'scheduled.favorite_request';

  /// F18 support (`support.reply`, `support.status`) and the lost item
  /// updates that open their ticket.
  static const String supportPrefix = 'support.';
  static const String lostItemUpdate = 'lost_item.update';

  /// F19 corporate accounts (`corporate.invitation`, `.guest_trip`,
  /// `.invoice_issued`); the invitation opens `/account/corporate`.
  static const String corporateInvitation = 'corporate.invitation';

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
  /// `support`, `system`), used when the API row has none.
  static String categoryOf(String code) {
    if (code == incentiveAchieved) return 'wallet';
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
      'support' => 'support',
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
    if (code == ratingReminder && tripId != null) return 'ata://rate/$tripId';
    if (code == promoNew) return 'ata://promotions';
    if (code == corporateInvitation) return 'ata://corporate/invitations';
    final String? ticketId = data?['ticketId']?.toString();
    if ((code.startsWith(supportPrefix) || code == lostItemUpdate) &&
        ticketId != null) {
      return 'ata://support/tickets/$ticketId';
    }
    final String? incentiveId = data?['incentiveId']?.toString();
    if (code.startsWith(incentivePrefix)) {
      return incentiveId == null
          ? 'ata://driver/incentives'
          : 'ata://driver/incentives/$incentiveId';
    }
    if (code == tierChanged) return 'ata://driver/tier';
    if (code.startsWith(scheduledPrefix)) return _scheduledLink(code, tripId);
    if (code == tripFavoriteFallback && tripId != null) {
      return 'ata://trip/$tripId';
    }
    if (code.startsWith('trip.') && tripId != null) return 'ata://trip/$tripId';
    if (code == applicationApproved) return 'ata://driver';
    if (code.startsWith('driver.application.')) return 'ata://driver/pending';
    if (code.startsWith('driver.')) return 'ata://driver';
    if (code.startsWith('payment.') || code.startsWith('wallet.')) {
      return 'ata://wallet';
    }
    return null;
  }

  /// Driver prompts open the reservation; everything else the booking.
  static String? _scheduledLink(String code, String? tripId) {
    if (tripId == null) {
      return code == 'scheduled.reservation_released'
          ? 'ata://driver/scheduled'
          : null;
    }
    return code == scheduledConfirmRequest || code == scheduledFavoriteRequest
        ? 'ata://driver/scheduled/$tripId'
        : 'ata://scheduled/$tripId';
  }
}
