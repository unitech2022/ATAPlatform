import 'dart:async';

import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/push/push_foreground_policy.dart';
import 'package:ata_app/core/push/push_service.dart';
import 'package:onesignal_flutter/onesignal_flutter.dart';

/// [PushService] backed by `onesignal_flutter` (one OneSignal app for
/// riders and drivers, Android and iOS).
class OneSignalPushService implements PushService {
  OneSignalPushService({required this.appId});

  final String appId;

  final StreamController<PushEvent> _opened =
      StreamController<PushEvent>.broadcast();
  final StreamController<PushEvent> _received =
      StreamController<PushEvent>.broadcast();
  bool _initialized = false;

  @override
  bool get isEnabled => true;

  @override
  String? get subscriptionId => OneSignal.User.pushSubscription.id;

  @override
  Stream<PushEvent> get opened => _opened.stream;

  @override
  Stream<PushEvent> get received => _received.stream;

  @override
  Future<void> initialize() async {
    if (_initialized) return;
    _initialized = true;
    await OneSignal.initialize(appId);
    OneSignal.Notifications.addClickListener(_onClick);
    OneSignal.Notifications.addForegroundWillDisplayListener(_onForeground);
  }

  void _onClick(OSNotificationClickEvent event) => _opened.add(
    PushEvent.fromData(
      event.notification.additionalData,
      actionId: event.result.actionId,
    ),
  );

  void _onForeground(OSNotificationWillDisplayEvent event) {
    final PushEvent push = PushEvent.fromData(
      event.notification.additionalData,
    );
    if (PushForegroundPolicy.handledInApp(push.eventCode)) {
      event.preventDefault();
    }
    _received.add(push);
  }

  @override
  Future<void> login(String userId) => OneSignal.login(userId);

  @override
  Future<void> logout() => OneSignal.logout();

  @override
  Future<void> setTags(Map<String, String> tags) =>
      OneSignal.User.addTags(tags);

  @override
  Future<void> setLanguage(String languageCode) =>
      OneSignal.User.setLanguage(languageCode);

  @override
  Future<bool> requestPermission() =>
      OneSignal.Notifications.requestPermission(true);
}
