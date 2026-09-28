import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/push/push_service.dart';

/// Used when `ONESIGNAL_APP_ID` is empty (development, tests): every call
/// succeeds and nothing is ever delivered.
class NoopPushService implements PushService {
  const NoopPushService();

  @override
  bool get isEnabled => false;

  @override
  String? get subscriptionId => null;

  @override
  Stream<PushEvent> get opened => const Stream<PushEvent>.empty();

  @override
  Stream<PushEvent> get received => const Stream<PushEvent>.empty();

  @override
  Future<void> initialize() async {}

  @override
  Future<void> login(String userId) async {}

  @override
  Future<void> logout() async {}

  @override
  Future<void> setTags(Map<String, String> tags) async {}

  @override
  Future<void> setLanguage(String languageCode) async {}

  @override
  Future<bool> requestPermission() async => false;
}
