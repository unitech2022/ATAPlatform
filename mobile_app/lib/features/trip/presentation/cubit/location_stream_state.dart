import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:equatable/equatable.dart';

/// Whether positions are flowing to the API.
enum LocationStreamStatus {
  idle,
  requesting,
  streaming,
  denied,
  deniedForever,
  serviceDisabled,
  failure,
}

/// State of [LocationStreamCubit].
class LocationStreamState extends Equatable {
  const LocationStreamState({
    this.status = LocationStreamStatus.idle,
    this.position,
    this.lastSentAt,
    this.failure,
  });

  final LocationStreamStatus status;
  final DriverPosition? position;
  final DateTime? lastSentAt;
  final Failure? failure;

  bool get isStreaming => status == LocationStreamStatus.streaming;

  /// The OS blocks positioning; the driver must fix it in settings.
  bool get isBlocked =>
      status == LocationStreamStatus.denied ||
      status == LocationStreamStatus.deniedForever ||
      status == LocationStreamStatus.serviceDisabled;

  LocationStreamState copyWith({
    LocationStreamStatus? status,
    DriverPosition? position,
    DateTime? lastSentAt,
    Failure? failure,
    bool clearFailure = false,
  }) => LocationStreamState(
    status: status ?? this.status,
    position: position ?? this.position,
    lastSentAt: lastSentAt ?? this.lastSentAt,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[status, position, lastSentAt, failure];
}
