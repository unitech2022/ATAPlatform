import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/rating/domain/usecases/submit_rating.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockRepository extends Mock implements RatingRepository {}

const SubmittedRating _rating = SubmittedRating(
  id: 'r1',
  tripId: 't1',
  stars: 2,
);

void main() {
  group('SubmitRating', () {
    late _MockRepository repository;

    setUpAll(
      () => registerFallbackValue(
        const RatingDraft(tripId: '', rater: TripActor.passenger, stars: 1),
      ),
    );

    setUp(() {
      repository = _MockRepository();
      when(
        () => repository.submit(any()),
      ).thenAnswer((_) async => const Right<Failure, SubmittedRating>(_rating));
    });

    test('rejects stars outside 1–5 without calling the API', () async {
      final result = await SubmitRating(repository)(
        const RatingDraft(tripId: 't1', rater: TripActor.driver, stars: 0),
      );
      final Failure failure = result.getLeft().toNullable()!;
      expect(failure.code, 'validation_failed');
      expect(failure.details, <String, dynamic>{'stars': 'invalid'});
      verifyNever(() => repository.submit(any()));
    });

    test('rejects a comment over 500 characters', () async {
      final result = await SubmitRating(repository)(
        RatingDraft(
          tripId: 't1',
          rater: TripActor.driver,
          stars: 3,
          comment: 'x' * 501,
        ),
      );
      expect(result.getLeft().toNullable()?.details, <String, dynamic>{
        'comment': 'invalid',
      });
    });

    test(
      'trims the comment, drops a blank one and de-duplicates tags',
      () async {
        await SubmitRating(repository)(
          const RatingDraft(
            tripId: 't1',
            rater: TripActor.driver,
            stars: 5,
            tags: <String>['punctuality', 'punctuality'],
            comment: '   ',
          ),
        );
        final RatingDraft sent =
            verify(() => repository.submit(captureAny())).captured.single
                as RatingDraft;
        expect(sent.comment, isNull);
        expect(sent.tags, <String>['punctuality']);
      },
    );
  });
}
