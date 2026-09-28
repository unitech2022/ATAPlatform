import 'package:ata_app/core/push/push_event.dart';

/// Push provider abstraction (OneSignal in production, no-op otherwise).
///
/// The user is addressed by External ID = `user_id`: [login] after sign-in,
/// [logout] on sign-out (`docs/03` OneSignal section).
abstract interface class PushService {
  /// `false` for the no-op implementation.
  bool get isEnabled;

  /// Provider subscription id, sent as `pushToken` to `PUT /me/devices`.
  String? get subscriptionId;

  /// Notifications the user tapped (opened the app from).
  Stream<PushEvent> get opened;

  /// Notifications that arrived while the app is in the foreground.
  Stream<PushEvent> get received;

  Future<void> initialize();
  Future<void> login(String userId);
  Future<void> logout();
  Future<void> setTags(Map<String, String> tags);
  Future<void> setLanguage(String languageCode);

  /// Shows the OS permission prompt once; returns whether push is allowed.
  Future<bool> requestPermission();
}
