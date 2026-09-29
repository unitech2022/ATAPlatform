import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/add_favorite_driver.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_tags.dart';
import 'package:ata_app/features/rating/domain/usecases/submit_rating.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_sheet.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/favorites_fakes.dart';
import '../../helpers/test_app.dart';

class _MockTags extends Mock implements GetRatingTags {}

class _MockSubmit extends Mock implements SubmitRating {}

class _MockAdd extends Mock implements AddFavoriteDriver {}

const RatingSubject _rider = RatingSubject(
  tripId: 't1',
  rater: TripActor.passenger,
  counterpartName: 'محمد',
);

void main() {
  late _MockTags tags;
  late _MockSubmit submit;
  late _MockAdd add;

  setUpAll(() {
    registerFallbackValue(RatingTargetRole.driver);
    registerFallbackValue(
      const RatingDraft(tripId: '', rater: TripActor.passenger, stars: 1),
    );
    registerFallbackValue(const AddFavoriteParams(tripId: 't'));
  });

  setUp(() {
    tags = _MockTags();
    submit = _MockSubmit();
    add = _MockAdd();
    when(() => tags(any())).thenAnswer(
      (_) async => const Right<Failure, List<RatingTag>>(<RatingTag>[]),
    );
    when(() => submit(any())).thenAnswer(
      (_) async => const Right<Failure, SubmittedRating>(
        SubmittedRating(id: 'r1', tripId: 't1', stars: 5),
      ),
    );
    when(() => add(any())).thenAnswer(
      (_) async => const Right<Failure, FavoriteDriver>(testFavoriteDriver),
    );
  });

  RatingCubit build({
    RatingSubject subject = _rider,
    bool withFavorites = true,
  }) => RatingCubit(
    subject: subject,
    getTags: tags,
    submitRating: submit,
    addFavorite: withFavorites ? add : null,
  );

  group('RatingCubit favourites option', () {
    test('adds the driver by tripId after the rating was sent', () async {
      final RatingCubit cubit = build();
      expect(cubit.canAddFavorite, isTrue);
      cubit
        ..setStars(5)
        ..toggleAddToFavorites();
      expect(cubit.state.addToFavorites, isTrue);
      await cubit.submit();

      verify(() => submit(any())).called(1);
      final AddFavoriteParams sent =
          verify(() => add(captureAny())).captured.single as AddFavoriteParams;
      expect(sent.tripId, 't1');
      expect(cubit.state.status, RatingStatus.done);
      expect(cubit.state.favoriteOutcome, RatingFavoriteOutcome.added);
      await cubit.close();
    });

    test('is off by default: the rating alone is sent', () async {
      final RatingCubit cubit = build();
      cubit.setStars(4);
      await cubit.submit();
      verifyNever(() => add(any()));
      expect(cubit.state.favoriteOutcome, RatingFavoriteOutcome.none);
      await cubit.close();
    });

    test('a favourites failure keeps the rating done and reports it', () async {
      when(() => add(any())).thenAnswer(
        (_) async => const Left<Failure, FavoriteDriver>(
          ServerFailure(code: 'favorites_limit', message: '', statusCode: 422),
        ),
      );
      final RatingCubit cubit = build();
      cubit
        ..setStars(5)
        ..toggleAddToFavorites();
      await cubit.submit();
      expect(cubit.state.status, RatingStatus.done);
      expect(cubit.state.favoriteOutcome, RatingFavoriteOutcome.failed);
      expect(cubit.state.favoriteFailure?.code, 'favorites_limit');
      await cubit.close();
    });

    test('favorite_exists still counts as added', () async {
      when(() => add(any())).thenAnswer(
        (_) async => const Left<Failure, FavoriteDriver>(
          ServerFailure(code: 'favorite_exists', message: '', statusCode: 409),
        ),
      );
      final RatingCubit cubit = build();
      cubit
        ..setStars(5)
        ..toggleAddToFavorites();
      await cubit.submit();
      expect(cubit.state.favoriteOutcome, RatingFavoriteOutcome.added);
      await cubit.close();
    });

    test('a failed rating does not add the driver', () async {
      when(() => submit(any())).thenAnswer(
        (_) async => const Left<Failure, SubmittedRating>(
          NetworkFailure(message: 'offline'),
        ),
      );
      final RatingCubit cubit = build();
      cubit
        ..setStars(5)
        ..toggleAddToFavorites();
      await cubit.submit();
      verifyNever(() => add(any()));
      expect(cubit.state.status, RatingStatus.editing);
      await cubit.close();
    });

    test('drivers rating passengers never get the option', () async {
      final RatingCubit cubit = build(
        subject: const RatingSubject(tripId: 't1', rater: TripActor.driver),
      );
      expect(cubit.canAddFavorite, isFalse);
      cubit.toggleAddToFavorites();
      expect(cubit.state.addToFavorites, isFalse);
      expect(build(withFavorites: false).canAddFavorite, isFalse);
      await cubit.close();
    });
  });

  group('rating sheet', () {
    Future<RatingCubit> pumpSheet(
      WidgetTester tester,
      RatingSubject subject,
    ) async {
      await tester.binding.setSurfaceSize(const Size(430, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      final RatingCubit cubit = build(subject: subject)..loadTags();
      addTearDown(cubit.close);
      await tester.pumpWidget(
        wrapForTest(
          BlocProvider<RatingCubit>.value(
            value: cubit,
            child: const RatingSheet(),
          ),
        ),
      );
      await tester.pumpAndSettle();
      return cubit;
    }

    testWidgets('the rider sees "أضف إلى المفضلة", turns it on and gets the '
        'confirmation', (WidgetTester tester) async {
      await pumpSheet(tester, _rider);
      expect(find.text('أضف إلى المفضلة'), findsOneWidget);
      expect(find.text('اطلب محمد مباشرة في رحلاتك القادمة'), findsOneWidget);

      await tester.tap(find.byKey(const ValueKey<String>('star-5')));
      await tester.pump();
      await tester.tap(
        find.descendant(
          of: find.byKey(const ValueKey<String>('rating-favorite-option')),
          matching: find.byType(GestureDetector),
        ),
      );
      await tester.pump();
      await tester.tap(find.text('إرسال التقييم'));
      await tester.pumpAndSettle();

      verify(() => add(any())).called(1);
      expect(find.text('شكراً لتقييمك'), findsOneWidget);
      expect(find.text('تمت إضافة الكابتن إلى مفضلتك'), findsOneWidget);
    });

    testWidgets('a favourites failure is explained on the thank-you screen', (
      WidgetTester tester,
    ) async {
      when(() => add(any())).thenAnswer(
        (_) async => const Left<Failure, FavoriteDriver>(
          ServerFailure(
            code: 'favorite_not_eligible',
            message: '',
            statusCode: 422,
          ),
        ),
      );
      await pumpSheet(tester, _rider);
      await tester.tap(find.byKey(const ValueKey<String>('star-5')));
      await tester.pump();
      await tester.tap(
        find.descendant(
          of: find.byKey(const ValueKey<String>('rating-favorite-option')),
          matching: find.byType(GestureDetector),
        ),
      );
      await tester.pump();
      await tester.tap(find.text('إرسال التقييم'));
      await tester.pumpAndSettle();
      expect(find.text('شكراً لتقييمك'), findsOneWidget);
      expect(
        find.textContaining('يمكنك إضافة الكابتن بعد إكمال رحلة معه'),
        findsOneWidget,
      );
    });

    testWidgets('the driver rating a passenger has no favourites option', (
      WidgetTester tester,
    ) async {
      await pumpSheet(
        tester,
        const RatingSubject(
          tripId: 't1',
          rater: TripActor.driver,
          counterpartName: 'سارة',
        ),
      );
      expect(
        find.byKey(const ValueKey<String>('rating-favorite-option')),
        findsNothing,
      );
    });
  });
}
