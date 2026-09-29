import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/usecases/get_pending_ratings.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockPending extends Mock implements GetPendingRatings {}

final DateTime _now = DateTime.utc(2026, 9, 29, 12);

final PendingRating _recent = PendingRating(
  tripId: 't1',
  tripNumber: 'T-1',
  counterpartName: 'محمد',
  completedAt: _now.subtract(const Duration(hours: 1)),
  rateUntil: _now.add(const Duration(hours: 71)),
);

final PendingRating _closed = PendingRating(
  tripId: 't0',
  rateUntil: _now.subtract(const Duration(minutes: 1)),
);

final PendingRating _older = PendingRating(
  tripId: 't2',
  counterpartName: 'خالد',
  rateUntil: _now.add(const Duration(hours: 5)),
);

void main() {
  late _MockPending pending;

  setUpAll(() => registerFallbackValue(TripActor.passenger));

  setUp(() {
    pending = _MockPending();
    when(() => pending(any())).thenAnswer(
      (_) async => Right<Failure, List<PendingRating>>(<PendingRating>[
        _closed,
        _recent,
        _older,
      ]),
    );
  });

  PendingRatingCubit build() =>
      PendingRatingCubit(getPending: pending, now: () => _now);

  blocTest<PendingRatingCubit, PendingRatingState>(
    'start loads the pending ratings of the role and prompts the first '
    'trip still inside the window',
    build: build,
    act: (PendingRatingCubit c) => c.start(TripActor.passenger),
    verify: (PendingRatingCubit c) {
      verify(() => pending(TripActor.passenger)).called(1);
      expect(c.state.prompt, _recent);
    },
  );

  blocTest<PendingRatingCubit, PendingRatingState>(
    'start is idempotent for the same role',
    build: build,
    act: (PendingRatingCubit c) async {
      await c.start(TripActor.driver);
      await c.start(TripActor.driver);
    },
    verify: (_) => verify(() => pending(TripActor.driver)).called(1),
  );

  blocTest<PendingRatingCubit, PendingRatingState>(
    'a dismissed prompt is not shown again; the next one is',
    build: build,
    act: (PendingRatingCubit c) async {
      await c.start(TripActor.passenger);
      c.dismiss('t1');
      await c.refresh();
    },
    verify: (PendingRatingCubit c) => expect(c.state.prompt, _older),
  );

  blocTest<PendingRatingCubit, PendingRatingState>(
    'markRated removes the trip and remembers it',
    build: build,
    act: (PendingRatingCubit c) async {
      await c.start(TripActor.passenger);
      c
        ..markRated('t1')
        ..markRated('t2');
    },
    verify: (PendingRatingCubit c) {
      expect(c.state.prompt, isNull);
      expect(c.state.isRated('t1'), isTrue);
      expect(c.state.pending.map((PendingRating p) => p.tripId), <String>[
        't0',
      ]);
    },
  );

  blocTest<PendingRatingCubit, PendingRatingState>(
    'a failed load keeps no prompt; stop resets',
    setUp: () => when(() => pending(any())).thenAnswer(
      (_) async => const Left<Failure, List<PendingRating>>(
        NetworkFailure(message: 'offline'),
      ),
    ),
    build: build,
    act: (PendingRatingCubit c) async {
      await c.start(TripActor.passenger);
      expect(c.state.prompt, isNull);
      expect(c.state.loading, isFalse);
      c.stop();
    },
    verify: (PendingRatingCubit c) =>
        expect(c.state, const PendingRatingState()),
  );

  test('refresh does nothing before start', () async {
    final PendingRatingCubit cubit = build();
    await cubit.refresh();
    verifyNever(() => pending(any()));
    await cubit.close();
  });
}
