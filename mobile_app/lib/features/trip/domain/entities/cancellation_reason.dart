import 'package:equatable/equatable.dart';

/// One row of `GET /catalog/cancellation-reasons?actor=&stage=` (F14).
/// Replaces the former static `CancelReason` enum.
class CancellationReason extends Equatable {
  const CancellationReason({
    required this.code,
    required this.name,
    this.requiresNote = false,
    this.isExcusable = false,
    this.isEmergency = false,
    this.stages,
  });

  final String code;

  /// Localized by the API (`Accept-Language`).
  final String name;
  final bool requiresNote;

  /// Reviewed by operations before any fee or points apply.
  final bool isExcusable;

  /// Treated as excusable and opens a safety case.
  final bool isEmergency;

  /// Stages where the reason applies; `null` = all.
  final List<String>? stages;

  /// No fee / points until operations review the excuse.
  bool get needsReview => isExcusable || isEmergency;

  @override
  List<Object?> get props => <Object?>[
    code,
    name,
    requiresNote,
    isExcusable,
    isEmergency,
    stages,
  ];
}
