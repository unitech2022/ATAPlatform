import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_tags.dart';
import 'package:ata_app/features/rating/domain/usecases/submit_rating.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Rating form of one trip: stars, tags (their meaning follows the stars),
/// comment and a single submission (`docs/10` §F15.2).
class RatingCubit extends Cubit<RatingState> {
  RatingCubit({
    required RatingSubject subject,
    required this._getTags,
    required this._submitRating,
  }) : super(RatingState(subject: subject));

  final GetRatingTags _getTags;
  final SubmitRating _submitRating;

  static const String ratingExists = 'rating_exists';
  static const String windowClosed = 'rating_window_closed';

  /// Loads the tag catalog; falls back to the seeded codes on failure.
  Future<void> loadTags() async {
    emit(state.copyWith(loadingTags: true));
    final result = await _getTags(state.subject.target);
    if (isClosed) return;
    final List<RatingTag> tags = result.fold(
      (_) => RatingTag.defaultsFor(state.subject.target),
      (List<RatingTag> tags) =>
          tags.isEmpty ? RatingTag.defaultsFor(state.subject.target) : tags,
    );
    emit(state.copyWith(loadingTags: false, tags: tags));
  }

  /// Picks 1–5 stars. Crossing the "liked / went wrong" boundary clears the
  /// chosen tags because their meaning flips.
  void setStars(int stars) {
    if (state.status != RatingStatus.editing) return;
    final int value = stars.clamp(RatingDraft.minStars, RatingDraft.maxStars);
    final bool wasNegative = state.isNegative;
    final bool negative = value <= RatingDraft.negativeMaxStars;
    emit(
      state.copyWith(
        stars: value,
        starsMissing: false,
        selectedTags: state.hasStars && wasNegative != negative
            ? const <String>[]
            : null,
        clearFailure: true,
      ),
    );
  }

  void toggleTag(String code) {
    if (state.status != RatingStatus.editing) return;
    final List<String> tags = List<String>.of(state.selectedTags);
    if (!tags.remove(code)) tags.add(code);
    emit(state.copyWith(selectedTags: tags));
  }

  void commentChanged(String comment) =>
      emit(state.copyWith(comment: comment, clearFailure: true));

  Future<void> submit() async {
    if (state.status != RatingStatus.editing) return;
    if (!state.hasStars) {
      emit(state.copyWith(starsMissing: true));
      return;
    }
    if (state.commentTooLong) return;
    emit(state.copyWith(status: RatingStatus.submitting, clearFailure: true));
    final result = await _submitRating(
      RatingDraft(
        tripId: state.subject.tripId,
        rater: state.subject.rater,
        stars: state.stars,
        tags: state.selectedTags,
        comment: state.comment,
      ),
    );
    if (isClosed) return;
    result.fold(_onFailure, (SubmittedRating rating) {
      emit(state.copyWith(status: RatingStatus.done, result: rating));
    });
  }

  void _onFailure(Failure failure) {
    final RatingStatus status = switch (failure.code) {
      ratingExists => RatingStatus.done,
      windowClosed => RatingStatus.closed,
      _ => RatingStatus.editing,
    };
    emit(state.copyWith(status: status, failure: failure));
  }
}
