import 'package:equatable/equatable.dart';

/// A non-internal note visible to the reporter.
class SafetyCaseNote extends Equatable {
  const SafetyCaseNote({required this.body, this.createdAt});

  final String body;
  final DateTime? createdAt;

  @override
  List<Object?> get props => <Object?>[body, createdAt];
}

/// `SafetyCaseSummary` (`/safety/cases`, SOS cancel, reports).
class SafetyCaseSummary extends Equatable {
  const SafetyCaseSummary({
    required this.id,
    required this.caseNumber,
    this.type = 'sos',
    this.status = 'open',
    this.priority = 'medium',
    this.tripId,
    this.tripNumber,
    this.openedAt,
    this.resolvedAt,
    this.publicNotes = const <SafetyCaseNote>[],
  });

  final String id;
  final String caseNumber;

  /// `sos`, `unexpected_stop`, `route_deviation`, `trip_overrun`,
  /// `safety_report`.
  final String type;

  /// `open`, `in_progress`, `escalated`, `resolved`.
  final String status;
  final String priority;
  final String? tripId;
  final String? tripNumber;
  final DateTime? openedAt;
  final DateTime? resolvedAt;
  final List<SafetyCaseNote> publicNotes;

  bool get isResolved => status == 'resolved';

  @override
  List<Object?> get props => <Object?>[
    id,
    caseNumber,
    type,
    status,
    priority,
    tripId,
    tripNumber,
    openedAt,
    resolvedAt,
    publicNotes,
  ];
}

/// `category` of `POST /safety/reports`.
enum SafetyReportCategory {
  unsafeDriving('unsafe_driving'),
  harassment('harassment'),
  vehicleMismatch('vehicle_mismatch'),
  driverMismatch('driver_mismatch'),
  passengerMisconduct('passenger_misconduct'),
  other('other');

  const SafetyReportCategory(this.apiValue);

  final String apiValue;
}

/// Body of `POST /safety/reports`.
class SafetyReportDraft extends Equatable {
  const SafetyReportDraft({
    required this.tripId,
    required this.category,
    required this.description,
  });

  final String tripId;
  final SafetyReportCategory category;
  final String description;

  @override
  List<Object?> get props => <Object?>[tripId, category, description];
}
