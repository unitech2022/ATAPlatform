import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /passenger|driver/trips/{id}/rating`.
class RatingDraft extends Equatable {
  const RatingDraft({
    required this.tripId,
    required this.rater,
    required this.stars,
    this.tags = const <String>[],
    this.comment,
  });

  static const int minStars = 1;
  static const int maxStars = 5;

  /// `comment VARCHAR(500)`.
  static const int maxCommentLength = 500;

  /// Up to this many stars the tags mean "what went wrong".
  static const int negativeMaxStars = 3;

  final String tripId;
  final TripActor rater;
  final int stars;
  final List<String> tags;
  final String? comment;

  bool get hasValidStars => stars >= minStars && stars <= maxStars;
  bool get hasValidComment => (comment?.length ?? 0) <= maxCommentLength;

  @override
  List<Object?> get props => <Object?>[tripId, rater, stars, tags, comment];
}

/// `201` answer of the rating endpoints.
class SubmittedRating extends Equatable {
  const SubmittedRating({
    required this.id,
    required this.tripId,
    required this.stars,
    this.tags = const <String>[],
    this.comment,
    this.createdAt,
  });

  final String id;
  final String tripId;
  final int stars;
  final List<String> tags;
  final String? comment;
  final DateTime? createdAt;

  @override
  List<Object?> get props => <Object?>[
    id,
    tripId,
    stars,
    tags,
    comment,
    createdAt,
  ];
}
