import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/usecases/accept_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/reject_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_offers.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/trip_fakes.dart';

class _MockAcceptOffer extends Mock implements AcceptOffer {}

class _MockRejectOffer extends Mock implements RejectOffer {}

/// Emits the whole countdown synchronously.
Stream<int> instantCountdown(int seconds) => Stream<int>.fromIterable(
  List<int>.generate(seconds, (int i) => seconds - i - 1),
);

/// Never ticks, so the offer stays pending.
Stream<int> frozenCountdown(int seconds) => const Stream<int>.empty();

void main() {
  late FakeTripRepository repository;
  late _MockAcceptOffer acceptOffer;
  late _MockRejectOffer rejectOffer;
  final DateTime now = DateTime.utc(2026, 9, 28, 12);
  final Offer offer = testOffer(
    expiresAt: now.add(const Duration(seconds: 12)),
  );

  setUpAll(() {
    registerFallbackValue(const RejectOfferParams(offerId: ''));
  });

  setUp(() {
    repository = FakeTripRepository();
    acceptOffer = _MockAcceptOffer();
    rejectOffer = _MockRejectOffer();
  });

  DriverOfferCubit build({
    Stream<int> Function(int) countdown = frozenCountdown,
  }) => DriverOfferCubit(
    watchOffers: WatchOffers(repository),
    acceptOffer: acceptOffer,
    rejectOffer: rejectOffer,
    countdown: countdown,
    now: () => now,
  );

  Future<void> push(Offer? value) async {
    repository.offers.add(value);
    await Future<void>.delayed(Duration.zero);
  }

  blocTest<DriverOfferCubit, DriverOfferState>(
    'an offer becomes pending with the seconds until it expires',
    build: build,
    act: (DriverOfferCubit cubit) async {
      cubit.start();
      await push(offer);
    },
    expect: () => <DriverOfferState>[
      const DriverOfferState(status: DriverOfferStatus.listening),
      DriverOfferState(
        status: DriverOfferStatus.pending,
        offer: offer,
        secondsLeft: 12,
      ),
    ],
  );

  blocTest<DriverOfferCubit, DriverOfferState>(
    'the countdown reaching zero expires the offer',
    build: () => build(countdown: instantCountdown),
    act: (DriverOfferCubit cubit) async {
      cubit.start();
      await push(offer);
      await Future<void>.delayed(Duration.zero);
    },
    verify: (DriverOfferCubit cubit) {
      expect(cubit.state.status, DriverOfferStatus.expired);
      expect(cubit.state.hasOffer, isFalse);
      expect(cubit.state.secondsLeft, 0);
    },
  );

  blocTest<DriverOfferCubit, DriverOfferState>(
    'OfferExpired from the feed expires a pending offer',
    build: build,
    act: (DriverOfferCubit cubit) async {
      cubit.start();
      await push(offer);
      await push(null);
    },
    verify: (DriverOfferCubit cubit) {
      expect(cubit.state.status, DriverOfferStatus.expired);
      expect(cubit.state.hasOffer, isFalse);
    },
  );

  blocTest<DriverOfferCubit, DriverOfferState>(
    'accept returns the trip and clears the offer',
    build: build,
    setUp: () => when(() => acceptOffer('o1')).thenAnswer(
      (_) async => Right<Failure, Trip>(tripAt(TripStage.driverAssigned)),
    ),
    act: (DriverOfferCubit cubit) async {
      cubit.start();
      await push(offer);
      await cubit.accept();
      expect(cubit.state.status, DriverOfferStatus.accepted);
      expect(cubit.state.trip?.status, TripStage.driverAssigned);
      expect(cubit.state.hasOffer, isFalse);
      cubit.acknowledge();
    },
    verify: (DriverOfferCubit cubit) {
      expect(cubit.state.status, DriverOfferStatus.listening);
      expect(cubit.state.trip, isNull);
    },
  );

  blocTest<DriverOfferCubit, DriverOfferState>(
    'accepting an expired offer (409) marks it expired',
    build: build,
    setUp: () => when(() => acceptOffer('o1')).thenAnswer(
      (_) async => const Left<Failure, Trip>(
        ServerFailure(code: 'offer_expired', message: '', statusCode: 409),
      ),
    ),
    act: (DriverOfferCubit cubit) async {
      cubit.start();
      await push(offer);
      await cubit.accept();
    },
    verify: (DriverOfferCubit cubit) {
      expect(cubit.state.status, DriverOfferStatus.expired);
      expect(cubit.state.failure?.code, 'offer_expired');
    },
  );

  blocTest<DriverOfferCubit, DriverOfferState>(
    'reject goes back to listening',
    build: build,
    setUp: () => when(
      () => rejectOffer(any()),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit)),
    act: (DriverOfferCubit cubit) async {
      cubit.start();
      await push(offer);
      await cubit.reject();
    },
    verify: (DriverOfferCubit cubit) {
      expect(cubit.state.status, DriverOfferStatus.listening);
      expect(cubit.state.hasOffer, isFalse);
      verify(() => rejectOffer(any())).called(1);
    },
  );

  test('stop resets to idle', () async {
    final DriverOfferCubit cubit = build()..start();
    await cubit.stop();
    expect(cubit.state, const DriverOfferState());
    await cubit.close();
  });
}
