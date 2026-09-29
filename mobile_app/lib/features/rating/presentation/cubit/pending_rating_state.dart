import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:equatable/equatable.dart';

/// State of [PendingRatingCubit].
class PendingRatingState extends Equatable {
  const PendingRatingState({
    this.role,
    this.pending = const <PendingRating>[],
    this.loading = false,
    this.dismissed = const <String>{},
    this.rated = const <String>{},
    this.now,
  });

  /// `null` while signed out / not started.
  final TripActor? role;
  final List<PendingRating> pending;
  final bool loading;

  /// Trips whose prompt was closed this session (shown once).
  final Set<String> dismissed;

  /// Trips rated from any screen this session.
  final Set<String> rated;

  /// Clock reading of the last load (window check).
  final DateTime? now;

  /// The trip to prompt for, if any.
  PendingRating? get prompt {
    for (final PendingRating p in pending) {
      if (dismissed.contains(p.tripId) || rated.contains(p.tripId)) continue;
      if (now != null && !p.isOpenAt(now!)) continue;
      return p;
    }
    return null;
  }

  bool isRated(String tripId) => rated.contains(tripId);

  PendingRatingState copyWith({
    TripActor? role,
    List<PendingRating>? pending,
    bool? loading,
    Set<String>? dismissed,
    Set<String>? rated,
    DateTime? now,
  }) => PendingRatingState(
    role: role ?? this.role,
    pending: pending ?? this.pending,
    loading: loading ?? this.loading,
    dismissed: dismissed ?? this.dismissed,
    rated: rated ?? this.rated,
    now: now ?? this.now,
  );

  @override
  List<Object?> get props => <Object?>[
    role,
    pending,
    loading,
    dismissed,
    rated,
    now,
  ];
}
