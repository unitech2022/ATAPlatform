import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of the F15 rating payloads (`docs/10` §F15.3).
abstract final class RatingModels {
  static RatingTag tag(Map<String, dynamic> json) => RatingTag(
    code: JsonReaders.string(json, 'code'),
    name: JsonReaders.string(json, 'name'),
  );

  static PendingRating pending(Map<String, dynamic> json) => PendingRating(
    tripId: JsonReaders.string(json, 'tripId'),
    tripNumber: JsonReaders.string(json, 'tripNumber'),
    counterpartName: JsonReaders.string(json, 'counterpartName'),
    completedAt: JsonReaders.date(json, 'completedAt'),
    rateUntil: JsonReaders.date(json, 'rateUntil'),
  );

  static SubmittedRating submitted(Map<String, dynamic> json) =>
      SubmittedRating(
        id: JsonReaders.string(json, 'id'),
        tripId: JsonReaders.string(json, 'tripId'),
        stars: JsonReaders.integer(json, 'stars'),
        tags: stringList(json['tags']),
        comment: JsonReaders.optionalString(json, 'comment'),
        createdAt: JsonReaders.date(json, 'createdAt'),
      );

  static Map<String, dynamic> draftBody(RatingDraft draft) => <String, dynamic>{
    'stars': draft.stars,
    'tags': draft.tags,
    'comment': ?draft.comment,
  };

  static RatingSummary summary(Map<String, dynamic> json) {
    final Map<String, dynamic> distribution =
        JsonReaders.object(json, 'distribution') ?? const <String, dynamic>{};
    return RatingSummary(
      ratingAvg: JsonReaders.optionalNumber(json, 'ratingAvg') ?? 5,
      ratingCount: JsonReaders.integer(json, 'ratingCount'),
      distribution: <int, int>{
        for (final MapEntry<String, dynamic> e in distribution.entries)
          if (int.tryParse(e.key) != null && e.value is num)
            int.parse(e.key): (e.value as num).toInt(),
      },
      topTags: JsonReaders.objects(json, 'topTags')
          .map(
            (Map<String, dynamic> t) => RatingTagCount(
              code: JsonReaders.string(t, 'code'),
              name: JsonReaders.string(t, 'name'),
              count: JsonReaders.integer(t, 'count'),
              positive: t['positive'] != false,
            ),
          )
          .toList(growable: false),
      recentComments: JsonReaders.objects(json, 'recentComments')
          .map(
            (Map<String, dynamic> c) => RatingComment(
              stars: JsonReaders.integer(c, 'stars'),
              comment: JsonReaders.string(c, 'comment'),
              week: JsonReaders.string(c, 'week'),
            ),
          )
          .toList(growable: false),
    );
  }

  static List<String> stringList(Object? value) => value is List<dynamic>
      ? value.map((Object? e) => e.toString()).toList(growable: false)
      : const <String>[];
}
