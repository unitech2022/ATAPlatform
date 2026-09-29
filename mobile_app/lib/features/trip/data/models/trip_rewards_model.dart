import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/trip_rewards.dart';

/// JSON mapping of the F15 fields of `Trip`.
abstract final class TripRewardsModel {
  static TripRatingInfo rating(Map<String, dynamic> json) {
    final Map<String, dynamic>? mine = JsonReaders.object(json, 'myRating');
    final Object? tags = mine?['tags'];
    final Object? canRate = json['canRate'];
    return TripRatingInfo(
      myStars: mine == null ? null : JsonReaders.optionalInteger(mine, 'stars'),
      myTags: tags is List<dynamic>
          ? tags.map((Object? t) => t.toString()).toList(growable: false)
          : const <String>[],
      canRate: canRate is bool ? canRate : null,
      rateUntil: JsonReaders.date(json, 'rateUntil'),
    );
  }

  static TripPromotion? promotion(Map<String, dynamic> json) {
    final Map<String, dynamic>? p = JsonReaders.object(json, 'promotion');
    if (p == null) return null;
    return TripPromotion(
      code: JsonReaders.string(p, 'code'),
      status: JsonReaders.optionalString(p, 'status') ?? 'reserved',
      discountAmount: JsonReaders.optionalNumber(p, 'discountAmount'),
    );
  }

  static Map<String, dynamic> toJson(
    TripRatingInfo rating,
    TripPromotion? promotion,
  ) => <String, dynamic>{
    'myRating': rating.myStars == null
        ? null
        : <String, dynamic>{'stars': rating.myStars, 'tags': rating.myTags},
    'canRate': rating.canRate,
    'rateUntil': rating.rateUntil?.toIso8601String(),
    'promotion': promotion == null
        ? null
        : <String, dynamic>{
            'code': promotion.code,
            'status': promotion.status,
            'discountAmount': promotion.discountAmount,
          },
  };
}
