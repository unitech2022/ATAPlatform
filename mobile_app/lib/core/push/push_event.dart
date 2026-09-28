import 'package:equatable/equatable.dart';

/// A push notification delivered by the provider, reduced to the fields the
/// app routes on (`data` of `docs/08` §F13.1).
class PushEvent extends Equatable {
  const PushEvent({
    this.eventCode,
    this.deepLink,
    this.notificationId,
    this.actionId,
    this.data = const <String, dynamic>{},
  });

  /// Reads `eventCode`, `deepLink` and `notificationId` from the payload.
  factory PushEvent.fromData(Map<String, dynamic>? data, {String? actionId}) {
    final Map<String, dynamic> payload = data ?? const <String, dynamic>{};
    return PushEvent(
      eventCode: payload[eventCodeKey]?.toString(),
      deepLink: payload[deepLinkKey]?.toString(),
      notificationId: payload[notificationIdKey]?.toString(),
      actionId: actionId,
      data: payload,
    );
  }

  static const String eventCodeKey = 'eventCode';
  static const String deepLinkKey = 'deepLink';
  static const String notificationIdKey = 'notificationId';

  /// Catalog code such as `trip.driver_arrived`.
  final String? eventCode;

  /// `ata://…` link to open.
  final String? deepLink;

  /// Id of the inbox row created with the push.
  final String? notificationId;

  /// Button pressed (for example `ok` / `help` of `safety.check`).
  final String? actionId;
  final Map<String, dynamic> data;

  @override
  List<Object?> get props => <Object?>[
    eventCode,
    deepLink,
    notificationId,
    actionId,
    data,
  ];
}
