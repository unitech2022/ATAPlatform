import 'package:equatable/equatable.dart';

/// A pending "are you OK?" check (`GET /safety/alerts/pending`, hub
/// `SafetyCheck`, push `safety.check`).
class SafetyAlert extends Equatable {
  const SafetyAlert({
    required this.id,
    this.tripId,
    this.type = '',
    this.status = pendingRider,
    this.detectedAt,
    this.respondBy,
  });

  static const String pendingRider = 'pending_rider';

  final String id;
  final String? tripId;

  /// `unexpected_stop`, `route_deviation`, `trip_overrun`.
  final String type;
  final String status;
  final DateTime? detectedAt;
  final DateTime? respondBy;

  bool get isPending => status == pendingRider;

  @override
  List<Object?> get props => <Object?>[
    id,
    tripId,
    type,
    status,
    detectedAt,
    respondBy,
  ];
}

/// `response` of `POST /safety/alerts/{id}/respond`.
enum SafetyCheckResponse {
  ok('ok'),
  needHelp('need_help');

  const SafetyCheckResponse(this.apiValue);

  final String apiValue;

  /// Notification buttons are `ok` / `help`.
  static SafetyCheckResponse? fromAction(String? actionId) =>
      switch (actionId) {
        'ok' => ok,
        'help' || 'need_help' => needHelp,
        _ => null,
      };
}
