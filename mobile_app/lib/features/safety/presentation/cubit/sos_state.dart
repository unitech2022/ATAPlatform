import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle of an SOS raised from the app.
enum SosStatus { idle, sending, active, cancelling, cancelled }

/// State of `SosCubit`.
class SosState extends Equatable {
  const SosState({
    this.status = SosStatus.idle,
    this.result,
    this.locationsSent = 0,
    this.failure,
  });

  final SosStatus status;

  /// Case number, status and emergency number once raised.
  final SosResult? result;

  /// Location updates delivered to `/safety/sos/{caseId}/location`.
  final int locationsSent;
  final Failure? failure;

  bool get isActive => status == SosStatus.active;
  bool get isBusy =>
      status == SosStatus.sending || status == SosStatus.cancelling;

  /// The SOS panel is shown (raised, being raised or just cancelled).
  bool get isVisible => status != SosStatus.idle || failure != null;

  String get emergencyNumber =>
      result?.emergencyNumber ?? SosResult.defaultEmergencyNumber;

  SosState copyWith({
    SosStatus? status,
    SosResult? result,
    int? locationsSent,
    Failure? failure,
    bool clearFailure = false,
  }) => SosState(
    status: status ?? this.status,
    result: result ?? this.result,
    locationsSent: locationsSent ?? this.locationsSent,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[status, result, locationsSent, failure];
}
