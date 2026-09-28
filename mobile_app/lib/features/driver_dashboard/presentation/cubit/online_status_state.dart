import 'package:ata_app/core/errors/failures.dart';
import 'package:equatable/equatable.dart';

/// State of the online/offline toggle.
class OnlineStatusState extends Equatable {
  const OnlineStatusState({
    this.isOnline = false,
    this.canGoOnline = true,
    this.updating = false,
    this.failure,
  });

  final bool isOnline;
  final bool canGoOnline;
  final bool updating;
  final Failure? failure;

  OnlineStatusState copyWith({
    bool? isOnline,
    bool? canGoOnline,
    bool? updating,
    Failure? failure,
    bool clearFailure = false,
  }) => OnlineStatusState(
    isOnline: isOnline ?? this.isOnline,
    canGoOnline: canGoOnline ?? this.canGoOnline,
    updating: updating ?? this.updating,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    isOnline,
    canGoOnline,
    updating,
    failure,
  ];
}
