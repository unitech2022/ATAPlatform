import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle of the "are you OK?" prompt.
enum SafetyCheckStatus { idle, prompting, responding, answered, expired }

/// State of `SafetyCheckCubit`.
class SafetyCheckState extends Equatable {
  const SafetyCheckState({
    this.status = SafetyCheckStatus.idle,
    this.alert,
    this.secondsLeft,
    this.response,
    this.failure,
  });

  final SafetyCheckStatus status;
  final SafetyAlert? alert;

  /// Seconds until `respondBy` (`null` when unknown).
  final int? secondsLeft;
  final SafetyCheckResponse? response;
  final Failure? failure;

  /// The prompt (banner / full screen) is visible.
  bool get isShown => status != SafetyCheckStatus.idle && alert != null;
  bool get canRespond => status == SafetyCheckStatus.prompting && alert != null;

  SafetyCheckState copyWith({
    SafetyCheckStatus? status,
    SafetyAlert? alert,
    int? secondsLeft,
    SafetyCheckResponse? response,
    Failure? failure,
    bool clearFailure = false,
  }) => SafetyCheckState(
    status: status ?? this.status,
    alert: alert ?? this.alert,
    secondsLeft: secondsLeft ?? this.secondsLeft,
    response: response ?? this.response,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    alert,
    secondsLeft,
    response,
    failure,
  ];
}
