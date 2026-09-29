import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/rating/data/models/rating_models.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';

/// `/catalog/rating-tags`, `/{role}/trips/{id}/rating` and
/// `/{role}/ratings/pending|summary`.
class RatingRemoteDataSource {
  const RatingRemoteDataSource(this._api);

  final ApiClient _api;

  static const String tagsPath = '/catalog/rating-tags';

  static String rolePrefix(TripActor role) =>
      role == TripActor.driver ? '/driver' : '/passenger';

  Future<List<RatingTag>> tags(RatingTargetRole target) async => _list(
    await _api.get(
      tagsPath,
      query: <String, dynamic>{'target': target.apiValue},
    ),
  ).map(RatingModels.tag).toList(growable: false);

  Future<SubmittedRating> submit(RatingDraft draft) async =>
      RatingModels.submitted(
        await _api.post(
              '${rolePrefix(draft.rater)}/trips/${draft.tripId}/rating',
              body: RatingModels.draftBody(draft),
            )
            as Map<String, dynamic>,
      );

  Future<List<PendingRating>> pending(TripActor role) async => _list(
    await _api.get('${rolePrefix(role)}/ratings/pending'),
  ).map(RatingModels.pending).toList(growable: false);

  Future<RatingSummary> summary(TripActor role) async => RatingModels.summary(
    await _api.get('${rolePrefix(role)}/ratings/summary')
        as Map<String, dynamic>,
  );

  /// Accepts a bare array or a `{ items: [...] }` page.
  static List<Map<String, dynamic>> _list(Object? body) {
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    return items is List<dynamic>
        ? items.whereType<Map<String, dynamic>>().toList(growable: false)
        : const <Map<String, dynamic>>[];
  }
}
