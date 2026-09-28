import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:equatable/equatable.dart';

/// State of `NoShowCubit`.
class NoShowState extends Equatable {
  const NoShowState({
    this.secondsRemaining = 0,
    this.started = false,
    this.busy = false,
    this.result,
    this.failure,
  });

  /// Seconds until "passenger didn't show" is allowed.
  final int secondsRemaining;
  final bool started;
  final bool busy;

  /// The cancelled trip returned by the API.
  final Trip? result;
  final Failure? failure;

  bool get canMarkNoShow =>
      started && secondsRemaining <= 0 && !busy && result == null;

  NoShowState copyWith({
    int? secondsRemaining,
    bool? started,
    bool? busy,
    Trip? result,
    Failure? failure,
    bool clearFailure = false,
  }) => NoShowState(
    secondsRemaining: secondsRemaining ?? this.secondsRemaining,
    started: started ?? this.started,
    busy: busy ?? this.busy,
    result: result ?? this.result,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    secondsRemaining,
    started,
    busy,
    result,
    failure,
  ];
}
