import 'package:ata_app/features/rating/data/models/rating_models.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rides/data/models/trip_summary_model.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_rewards.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('parses a pending rating', () {
    final PendingRating p = RatingModels.pending(<String, dynamic>{
      'tripId': 't1',
      'tripNumber': 'T-20260929-00001',
      'counterpartName': 'محمد',
      'completedAt': '2026-09-29T10:00:00Z',
      'rateUntil': '2026-10-02T10:00:00Z',
    });
    expect(p.counterpartName, 'محمد');
    expect(p.isOpenAt(DateTime.utc(2026, 10, 2, 9)), isTrue);
    expect(p.isOpenAt(DateTime.utc(2026, 10, 2, 10)), isFalse);
  });

  test('parses the 201 rating and builds the request body', () {
    final SubmittedRating r = RatingModels.submitted(<String, dynamic>{
      'id': 'r1',
      'tripId': 't1',
      'stars': 5,
      'tags': <String>['driving', 'cleanliness'],
      'comment': null,
      'createdAt': '2026-09-29T10:00:00Z',
    });
    expect(r.tags, <String>['driving', 'cleanliness']);
    expect(r.comment, isNull);
    expect(
      RatingModels.draftBody(
        const RatingDraft(
          tripId: 't1',
          rater: TripActor.passenger,
          stars: 4,
          tags: <String>['navigation'],
        ),
      ),
      <String, dynamic>{
        'stars': 4,
        'tags': <String>['navigation'],
      },
    );
  });

  test('parses the ratings summary', () {
    final RatingSummary s = RatingModels.summary(<String, dynamic>{
      'ratingAvg': 4.87,
      'ratingCount': 312,
      'distribution': <String, dynamic>{
        '1': 2,
        '2': 3,
        '3': 10,
        '4': 40,
        '5': 257,
      },
      'topTags': <Map<String, dynamic>>[
        <String, dynamic>{
          'code': 'driving',
          'name': 'القيادة',
          'count': 120,
          'positive': true,
        },
        <String, dynamic>{
          'code': 'navigation',
          'name': 'معرفة الطريق',
          'count': 4,
          'positive': false,
        },
      ],
      'recentComments': <Map<String, dynamic>>[
        <String, dynamic>{'stars': 5, 'comment': 'ممتاز', 'week': '2026-W39'},
      ],
    });
    expect(s.ratingAvg, 4.87);
    expect(s.countFor(5), 257);
    expect(s.shareOf(1), closeTo(2 / 312, 1e-9));
    expect(s.topTags.last.positive, isFalse);
    expect(s.recentComments.single.week, '2026-W39');
  });

  test('Trip carries myRating / canRate / rateUntil and the promotion', () {
    final Trip trip = TripModel.fromJson(const <String, dynamic>{
      'id': 't1',
      'tripNumber': 'T-1',
      'status': 'completed',
      'myRating': null,
      'canRate': true,
      'rateUntil': '2026-10-02T10:00:00Z',
      'promotion': <String, dynamic>{
        'code': 'ATA10',
        'status': 'applied',
        'discountAmount': 5,
      },
    });
    expect(trip.rating.isRated, isFalse);
    expect(trip.rating.canRateAt(DateTime.utc(2026, 10, 1)), isTrue);
    expect(trip.rating.canRateAt(DateTime.utc(2026, 10, 3)), isFalse);
    expect(trip.promotion?.code, 'ATA10');
    expect(trip.promotion?.discountAmount, 5);
    final Trip back = TripModel.fromJson((trip as TripModel).toJson());
    expect(back.rating, trip.rating);
    expect(back.promotion, trip.promotion);
  });

  test('a rated trip cannot be rated again; without flags the 72 h window '
      'from completion applies', () {
    const TripRatingInfo rated = TripRatingInfo(myStars: 4);
    expect(rated.canRateAt(DateTime.utc(2026)), isFalse);
    final DateTime done = DateTime.utc(2026, 9, 29, 10);
    const TripRatingInfo unknown = TripRatingInfo();
    expect(
      unknown.canRateAt(DateTime.utc(2026, 10, 2, 9), completedAt: done),
      isTrue,
    );
    expect(
      unknown.canRateAt(DateTime.utc(2026, 10, 2, 11), completedAt: done),
      isFalse,
    );
    expect(unknown.canRateAt(DateTime.utc(2026, 9, 29)), isFalse);
  });

  test('TripSummary reads the rating fields', () {
    final TripSummary t = TripSummaryModel.fromJson(const <String, dynamic>{
      'id': 't1',
      'status': 'completed',
      'completedAt': '2026-09-29T10:00:00Z',
      'driverName': 'محمد',
      'myRating': <String, dynamic>{
        'stars': 5,
        'tags': <String>['driving'],
      },
    });
    expect(t.driverName, 'محمد');
    expect(t.rating.myStars, 5);
    expect(t.canRateAt(DateTime.utc(2026, 9, 29, 12)), isFalse);
  });
}
