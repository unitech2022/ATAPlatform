import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_tags.dart';
import 'package:ata_app/features/rating/domain/usecases/submit_rating.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_sheet.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/test_app.dart';

class _MockTags extends Mock implements GetRatingTags {}

class _MockSubmit extends Mock implements SubmitRating {}

void main() {
  late _MockTags tags;
  late _MockSubmit submit;

  setUpAll(() {
    registerFallbackValue(RatingTargetRole.driver);
    registerFallbackValue(
      const RatingDraft(tripId: '', rater: TripActor.passenger, stars: 1),
    );
  });

  setUp(() {
    tags = _MockTags();
    submit = _MockSubmit();
    when(() => tags(any())).thenAnswer(
      (_) async => const Right<Failure, List<RatingTag>>(<RatingTag>[]),
    );
    when(() => submit(any())).thenAnswer(
      (_) async => const Right<Failure, SubmittedRating>(
        SubmittedRating(id: 'r1', tripId: 't1', stars: 2),
      ),
    );
  });

  Future<RatingCubit> pumpSheet(WidgetTester tester, RatingSubject s) async {
    await tester.binding.setSurfaceSize(const Size(430, 1200));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final RatingCubit cubit = RatingCubit(
      subject: s,
      getTags: tags,
      submitRating: submit,
    )..loadTags();
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

  testWidgets('rider flow: stars → negative tags → comment → thanks', (
    WidgetTester tester,
  ) async {
    await pumpSheet(
      tester,
      const RatingSubject(
        tripId: 't1',
        rater: TripActor.passenger,
        counterpartName: 'محمد',
      ),
    );

    expect(find.text('كيف كانت رحلتك مع محمد؟'), findsOneWidget);
    expect(find.text('ما الذي أعجبك؟'), findsNothing);

    // Submitting without stars asks for them and sends nothing.
    await tester.tap(find.text('إرسال التقييم'));
    await tester.pump();
    expect(find.text('اختر عدد النجوم'), findsOneWidget);
    verifyNever(() => submit(any()));

    await tester.tap(find.byKey(const ValueKey<String>('star-5')));
    await tester.pump();
    expect(find.text('ممتازة'), findsOneWidget);
    expect(find.text('ما الذي أعجبك؟'), findsOneWidget);
    // The empty catalog falls back to the five seeded driver tags.
    for (final String label in <String>[
      'القيادة',
      'النظافة',
      'التعامل',
      'معرفة الطريق',
      'حالة المركبة',
    ]) {
      expect(find.text(label), findsOneWidget);
    }

    await tester.tap(find.byKey(const ValueKey<String>('star-2')));
    await tester.pump();
    expect(find.text('ما الذي لم يعجبك؟'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey<String>('tag-cleanliness')));
    await tester.enterText(
      find.byKey(const ValueKey<String>('rating-comment')),
      'السيارة غير نظيفة',
    );
    await tester.tap(find.text('إرسال التقييم'));
    await tester.pumpAndSettle();

    final RatingDraft sent =
        verify(() => submit(captureAny())).captured.single as RatingDraft;
    expect(sent.stars, 2);
    expect(sent.tags, <String>['cleanliness']);
    expect(sent.comment, 'السيارة غير نظيفة');
    expect(find.text('شكراً لتقييمك'), findsOneWidget);
  });

  testWidgets('driver rates the passenger with the passenger tags', (
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
    expect(find.text('كيف كان الراكب سارة؟'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey<String>('star-4')));
    await tester.pump();
    expect(find.text('الالتزام بالوقت'), findsOneWidget);
    expect(find.text('التعامل'), findsOneWidget);
    expect(find.text('النظافة'), findsOneWidget);
    expect(find.text('القيادة'), findsNothing);
    verify(() => tags(RatingTargetRole.passenger)).called(1);
  });

  testWidgets('rating_exists shows the localized message and a done button', (
    WidgetTester tester,
  ) async {
    when(() => submit(any())).thenAnswer(
      (_) async => const Left<Failure, SubmittedRating>(
        ServerFailure(code: 'rating_exists', message: '', statusCode: 409),
      ),
    );
    await pumpSheet(
      tester,
      const RatingSubject(tripId: 't1', rater: TripActor.passenger),
    );
    expect(find.text('كيف كانت رحلتك مع الكابتن؟'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey<String>('star-3')));
    await tester.pump();
    await tester.tap(find.text('إرسال التقييم'));
    await tester.pumpAndSettle();
    expect(find.text('تم تقييم هذه الرحلة مسبقاً'), findsOneWidget);
    expect(find.text('تم'), findsOneWidget);
    expect(find.text('إرسال التقييم'), findsNothing);
  });
}
