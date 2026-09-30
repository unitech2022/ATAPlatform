import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:equatable/equatable.dart';

/// State of `AirportQueueCubit` (driver).
class AirportQueueState extends Equatable {
  const AirportQueueState({
    this.queue,
    this.loading = false,
    this.busy = false,
    this.failure,
    this.actionFailure,
  });

  final AirportQueueStatus? queue;
  final bool loading;

  /// Joining or leaving.
  final bool busy;
  final Failure? failure;

  /// `422 not_in_airport_waiting_area` and other join / leave errors.
  final Failure? actionFailure;

  bool get inQueue => queue?.inQueue ?? false;

  /// The overview card is shown when queued or standing at an airport.
  bool get isRelevant => queue?.isRelevant ?? false;

  AirportQueueState copyWith({
    AirportQueueStatus? queue,
    bool? loading,
    bool? busy,
    Failure? failure,
    Failure? actionFailure,
    bool clearFailure = false,
    bool clearActionFailure = false,
  }) => AirportQueueState(
    queue: queue ?? this.queue,
    loading: loading ?? this.loading,
    busy: busy ?? this.busy,
    failure: clearFailure ? null : failure ?? this.failure,
    actionFailure: clearActionFailure
        ? null
        : actionFailure ?? this.actionFailure,
  );

  @override
  List<Object?> get props => <Object?>[
    queue,
    loading,
    busy,
    failure,
    actionFailure,
  ];
}
