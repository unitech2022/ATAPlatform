import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/usecases/get_pending_ratings.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// App-wide: loads the unrated recent trips on sign-in / app start and after
/// a trip completes, and exposes one [PendingRatingState.prompt] at a time.
/// A dismissed prompt is not shown again this session; ratings sent from
/// any screen are remembered so the "rate" buttons disappear.
class PendingRatingCubit extends Cubit<PendingRatingState> {
  PendingRatingCubit({required this._getPending, DateTime Function()? now})
    : _now = now ?? DateTime.now,
      super(const PendingRatingState());

  final GetPendingRatings _getPending;
  final DateTime Function() _now;

  /// Binds the cubit to [role] and loads (no-op when already bound).
  Future<void> start(TripActor role) async {
    if (state.role == role) return;
    emit(PendingRatingState(role: role));
    await refresh();
  }

  /// Signed out or role not eligible.
  void stop() {
    if (state.role != null) emit(const PendingRatingState());
  }

  /// Reloads the pending list (after a trip completes).
  Future<void> refresh() async {
    final TripActor? role = state.role;
    if (role == null || state.loading) return;
    emit(state.copyWith(loading: true));
    final result = await _getPending(role);
    if (isClosed || state.role != role) return;
    emit(
      result.fold(
        (_) => state.copyWith(loading: false, now: _now()),
        (List<PendingRating> pending) =>
            state.copyWith(loading: false, pending: pending, now: _now()),
      ),
    );
  }

  /// Closes the prompt of [tripId] without rating.
  void dismiss(String tripId) =>
      emit(state.copyWith(dismissed: <String>{...state.dismissed, tripId}));

  /// Records a sent rating (receipt, rides, prompt or driver summary).
  void markRated(String tripId) => emit(
    state.copyWith(
      rated: <String>{...state.rated, tripId},
      pending: state.pending
          .where((PendingRating p) => p.tripId != tripId)
          .toList(growable: false),
    ),
  );
}
