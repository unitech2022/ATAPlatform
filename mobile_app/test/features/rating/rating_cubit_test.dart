import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_tags.dart';
import 'package:ata_app/features/rating/domain/usecases/submit_rating.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockTags extends Mock implements GetRatingTags {}

class _MockSubmit extends Mock implements SubmitRating {}

const RatingSubject _subject = RatingSubject(
  tripId: 't1',
  rater: TripActor.passenger,
  counterpartName: 'محمد',
);

const RatingDraft _draft = RatingDraft(
  tripId: 't1',
  rater: TripActor.passenger,
  stars: 5,
);

const SubmittedRating _rating = SubmittedRating(
  id: 'r1',
  tripId: 't1',
  stars: 2,
  tags: <String>['cleanliness'],
);

void main() {
  late _MockTags tags;
  late _MockSubmit submit;

  setUpAll(() {
    registerFallbackValue(RatingTargetRole.driver);
    registerFallbackValue(_draft);
  });

  setUp(() {
    tags = _MockTags();
    submit = _MockSubmit();
    when(() => tags(any())).thenAnswer(
      (_) async => const Right<Failure, List<RatingTag>>(<RatingTag>[
        RatingTag(code: 'driving', name: 'القيادة'),
      ]),
    );
    when(
      () => submit(any()),
    ).thenAnswer((_) async => const Right<Failure, SubmittedRating>(_rating));
  });

  RatingCubit build() =>
      RatingCubit(subject: _subject, getTags: tags, submitRating: submit);

  group('RatingCubit', () {
    blocTest<RatingCubit, RatingState>(
      'loads the driver tags for a passenger rater',
      build: build,
      act: (RatingCubit c) => c.loadTags(),
      verify: (RatingCubit c) {
        verify(() => tags(RatingTargetRole.driver)).called(1);
        expect(c.state.tags.single.code, 'driving');
        expect(c.state.loadingTags, isFalse);
      },
    );

    blocTest<RatingCubit, RatingState>(
      'falls back to the seeded tags when the catalog fails',
      setUp: () => when(() => tags(any())).thenAnswer(
        (_) async => const Left<Failure, List<RatingTag>>(
          NetworkFailure(message: 'offline'),
        ),
      ),
      build: build,
      act: (RatingCubit c) => c.loadTags(),
      verify: (RatingCubit c) => expect(
        c.state.tags.map((RatingTag t) => t.code),
        RatingTag.driverCodes,
      ),
    );

    blocTest<RatingCubit, RatingState>(
      'submit without stars flags the stars and sends nothing',
      build: build,
      act: (RatingCubit c) => c.submit(),
      expect: () => <RatingState>[
        const RatingState(subject: _subject, starsMissing: true),
      ],
      verify: (_) => verifyNever(() => submit(any())),
    );

    blocTest<RatingCubit, RatingState>(
      'crossing the liked / went-wrong boundary clears the tags',
      build: build,
      act: (RatingCubit c) => c
        ..setStars(5)
        ..toggleTag('driving')
        ..toggleTag('navigation')
        ..toggleTag('navigation')
        ..setStars(4)
        ..setStars(2),
      verify: (RatingCubit c) {
        expect(c.state.stars, 2);
        expect(c.state.isNegative, isTrue);
        expect(c.state.selectedTags, isEmpty);
      },
    );

    blocTest<RatingCubit, RatingState>(
      'keeps the tags while staying on the same side',
      build: build,
      act: (RatingCubit c) => c
        ..setStars(5)
        ..toggleTag('driving')
        ..setStars(4),
      verify: (RatingCubit c) =>
          expect(c.state.selectedTags, <String>['driving']),
    );

    blocTest<RatingCubit, RatingState>(
      'submits stars, tags and comment once',
      build: build,
      act: (RatingCubit c) async {
        c
          ..setStars(2)
          ..toggleTag('cleanliness')
          ..commentChanged('  السيارة غير نظيفة ');
        await c.submit();
        await c.submit();
      },
      verify: (RatingCubit c) {
        final RatingDraft sent =
            verify(() => submit(captureAny())).captured.single as RatingDraft;
        expect(sent.stars, 2);
        expect(sent.tags, <String>['cleanliness']);
        expect(sent.comment, '  السيارة غير نظيفة ');
        expect(c.state.status, RatingStatus.done);
        expect(c.state.result, _rating);
      },
    );

    blocTest<RatingCubit, RatingState>(
      '409 rating_exists finishes the form with the message',
      setUp: () => when(() => submit(any())).thenAnswer(
        (_) async => const Left<Failure, SubmittedRating>(
          ServerFailure(code: 'rating_exists', message: '', statusCode: 409),
        ),
      ),
      build: build,
      act: (RatingCubit c) async {
        c.setStars(4);
        await c.submit();
      },
      verify: (RatingCubit c) {
        expect(c.state.status, RatingStatus.done);
        expect(c.state.isFinished, isTrue);
        expect(c.state.failure?.code, 'rating_exists');
      },
    );

    blocTest<RatingCubit, RatingState>(
      '422 rating_window_closed closes the form',
      setUp: () => when(() => submit(any())).thenAnswer(
        (_) async => const Left<Failure, SubmittedRating>(
          ServerFailure(code: 'rating_window_closed', message: ''),
        ),
      ),
      build: build,
      act: (RatingCubit c) async {
        c.setStars(4);
        await c.submit();
      },
      verify: (RatingCubit c) {
        expect(c.state.status, RatingStatus.closed);
        expect(c.state.canSubmit, isFalse);
      },
    );

    blocTest<RatingCubit, RatingState>(
      'other failures keep the form editable',
      setUp: () => when(() => submit(any())).thenAnswer(
        (_) async => const Left<Failure, SubmittedRating>(
          NetworkFailure(message: 'offline'),
        ),
      ),
      build: build,
      act: (RatingCubit c) async {
        c.setStars(4);
        await c.submit();
      },
      verify: (RatingCubit c) {
        expect(c.state.status, RatingStatus.editing);
        expect(c.state.canSubmit, isTrue);
        expect(c.state.failure, isA<NetworkFailure>());
      },
    );

    test('a comment over 500 characters blocks submitting', () {
      final RatingCubit cubit = build()
        ..setStars(5)
        ..commentChanged('x' * 501);
      expect(cubit.state.commentTooLong, isTrue);
      expect(cubit.state.canSubmit, isFalse);
      cubit.close();
    });
  });
}
