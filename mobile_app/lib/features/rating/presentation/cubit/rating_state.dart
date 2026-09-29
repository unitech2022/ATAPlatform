import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle of the rating form.
enum RatingStatus {
  editing,
  submitting,

  /// Sent (or `409 rating_exists`): nothing left to rate.
  done,

  /// `422 rating_window_closed`: the 72 h window is over.
  closed,
}

/// Result of the optional "add to favourites" of the rating form (F16).
enum RatingFavoriteOutcome { none, added, failed }

/// State of [RatingCubit].
class RatingState extends Equatable {
  const RatingState({
    required this.subject,
    this.tags = const <RatingTag>[],
    this.loadingTags = false,
    this.stars = 0,
    this.selectedTags = const <String>[],
    this.comment = '',
    this.status = RatingStatus.editing,
    this.starsMissing = false,
    this.failure,
    this.result,
    this.addToFavorites = false,
    this.favoriteOutcome = RatingFavoriteOutcome.none,
    this.favoriteFailure,
  });

  final RatingSubject subject;
  final List<RatingTag> tags;
  final bool loadingTags;

  /// 0 until the user taps a star.
  final int stars;
  final List<String> selectedTags;
  final String comment;
  final RatingStatus status;

  /// Submit was pressed without choosing stars.
  final bool starsMissing;
  final Failure? failure;
  final SubmittedRating? result;

  /// "أضف إلى المفضلة" is on (riders only): the driver is added after the
  /// rating is sent.
  final bool addToFavorites;
  final RatingFavoriteOutcome favoriteOutcome;
  final Failure? favoriteFailure;

  bool get hasStars => stars >= RatingDraft.minStars;

  /// With ≤ 3 stars the tags mean "what went wrong".
  bool get isNegative => hasStars && stars <= RatingDraft.negativeMaxStars;
  bool get commentTooLong => comment.length > RatingDraft.maxCommentLength;
  bool get isSubmitting => status == RatingStatus.submitting;
  bool get isFinished =>
      status == RatingStatus.done || status == RatingStatus.closed;
  bool get canSubmit =>
      status == RatingStatus.editing && hasStars && !commentTooLong;

  RatingState copyWith({
    List<RatingTag>? tags,
    bool? loadingTags,
    int? stars,
    List<String>? selectedTags,
    String? comment,
    RatingStatus? status,
    bool? starsMissing,
    Failure? failure,
    SubmittedRating? result,
    bool? addToFavorites,
    RatingFavoriteOutcome? favoriteOutcome,
    Failure? favoriteFailure,
    bool clearFailure = false,
  }) => RatingState(
    subject: subject,
    tags: tags ?? this.tags,
    loadingTags: loadingTags ?? this.loadingTags,
    stars: stars ?? this.stars,
    selectedTags: selectedTags ?? this.selectedTags,
    comment: comment ?? this.comment,
    status: status ?? this.status,
    starsMissing: starsMissing ?? this.starsMissing,
    failure: clearFailure ? null : failure ?? this.failure,
    result: result ?? this.result,
    addToFavorites: addToFavorites ?? this.addToFavorites,
    favoriteOutcome: favoriteOutcome ?? this.favoriteOutcome,
    favoriteFailure: favoriteFailure ?? this.favoriteFailure,
  );

  @override
  List<Object?> get props => <Object?>[
    subject,
    tags,
    loadingTags,
    stars,
    selectedTags,
    comment,
    status,
    starsMissing,
    failure,
    result,
    addToFavorites,
    favoriteOutcome,
    favoriteFailure,
  ];
}
