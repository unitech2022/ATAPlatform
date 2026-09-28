/// Decides which pushes the app renders itself while in the foreground
/// (`docs/08` §F13.9): driver offers, trip updates and safety checks update
/// their screens instead of showing a system banner.
abstract final class PushForegroundPolicy {
  static const String _offerReceived = 'offer.received';
  static const String _safetyCheck = 'safety.check';
  static const String _tripPrefix = 'trip.';

  /// `true` when the system banner should be suppressed.
  static bool handledInApp(String? eventCode) {
    if (eventCode == null) return false;
    return eventCode == _offerReceived ||
        eventCode == _safetyCheck ||
        eventCode.startsWith(_tripPrefix);
  }
}
