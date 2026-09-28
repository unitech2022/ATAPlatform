import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/estimate_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/request_trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/trip_fakes.dart';

class _MockEstimateTrip extends Mock implements EstimateTrip {}

class _MockRequestTrip extends Mock implements RequestTrip {}

class _MockCancelTrip extends Mock implements CancelTrip {}

void main() {
  late _MockEstimateTrip estimateTrip;
  late _MockRequestTrip requestTrip;
  late _MockCancelTrip cancelTrip;

  setUpAll(() {
    registerFallbackValue(testTripRequest);
    registerFallbackValue(
      const CancelTripParams(
        tripId: '',
        actor: TripActor.passenger,
        reason: CancelReason.other,
      ),
    );
  });

  setUp(() {
    estimateTrip = _MockEstimateTrip();
    requestTrip = _MockRequestTrip();
    cancelTrip = _MockCancelTrip();
    when(
      () => estimateTrip(any()),
    ).thenAnswer((_) async => const Right<Failure, TripEstimate>(testEstimate));
    when(
      () => requestTrip(any()),
    ).thenAnswer((_) async => const Right<Failure, Trip>(testTrip));
  });

  TripRequestCubit build() => TripRequestCubit(
    estimateTrip: estimateTrip,
    requestTrip: requestTrip,
    cancelTrip: cancelTrip,
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'estimates then requests with fixed pricing',
    build: build,
    act: (TripRequestCubit cubit) => cubit.request(testTripRequest),
    expect: () => <TripRequestState>[
      const TripRequestState(status: TripRequestStatus.requesting),
      const TripRequestState(
        status: TripRequestStatus.searching,
        estimate: testEstimate,
        trip: testTrip,
      ),
    ],
    verify: (_) {
      final TripRequest sent =
          verify(() => requestTrip(captureAny())).captured.single
              as TripRequest;
      expect(sent.pricingMode, PricingMode.fixed);
      expect(sent.offeredPrice, isNull);
      verify(() => estimateTrip(any())).called(1);
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'an offered price switches the pricing mode to offer',
    build: build,
    act: (TripRequestCubit cubit) =>
        cubit.request(testTripRequest.copyWith(offeredPrice: 30)),
    verify: (_) {
      final TripRequest sent =
          verify(() => requestTrip(captureAny())).captured.single
              as TripRequest;
      expect(sent.pricingMode, PricingMode.offer);
      expect(sent.offeredPrice, 30);
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'a quoteId skips the legacy estimate and is sent with the offered price',
    build: build,
    act: (TripRequestCubit cubit) => cubit.request(
      testTripRequest.copyWith(quoteId: 'q1', offeredPrice: 35),
    ),
    expect: () => <TripRequestState>[
      const TripRequestState(status: TripRequestStatus.requesting),
      const TripRequestState(
        status: TripRequestStatus.searching,
        trip: testTrip,
      ),
    ],
    verify: (_) {
      final TripRequest sent =
          verify(() => requestTrip(captureAny())).captured.single
              as TripRequest;
      expect(sent.quoteId, 'q1');
      expect(sent.offeredPrice, 35);
      expect(sent.pricingMode, PricingMode.offer);
      verifyNever(() => estimateTrip(any()));
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'offer_out_of_range exposes the accepted bounds',
    build: build,
    setUp: () => when(() => requestTrip(any())).thenAnswer(
      (_) async => const Left<Failure, Trip>(
        ServerFailure(
          code: 'offer_out_of_range',
          message: '',
          details: <String, dynamic>{'offerMin': 29.5, 'offerMax': 54.5},
          statusCode: 422,
        ),
      ),
    ),
    act: (TripRequestCubit cubit) => cubit.request(
      testTripRequest.copyWith(quoteId: 'q1', offeredPrice: 10),
    ),
    verify: (TripRequestCubit cubit) {
      expect(cubit.state.status, TripRequestStatus.failure);
      expect(cubit.state.offerBounds, const OfferBounds(min: 29.5, max: 54.5));
      expect(cubit.state.isQuoteExpired, isFalse);
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'quote_expired is flagged so the quote can be refreshed',
    build: build,
    setUp: () => when(() => requestTrip(any())).thenAnswer(
      (_) async => const Left<Failure, Trip>(
        ServerFailure(code: 'quote_expired', message: '', statusCode: 422),
      ),
    ),
    act: (TripRequestCubit cubit) =>
        cubit.request(testTripRequest.copyWith(quoteId: 'stale')),
    verify: (TripRequestCubit cubit) {
      expect(cubit.state.status, TripRequestStatus.failure);
      expect(cubit.state.isQuoteExpired, isTrue);
      expect(cubit.state.offerBounds, isNull);
      expect(cubit.state.trip, isNull);
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'a failed estimate stops before requesting',
    build: build,
    setUp: () => when(() => estimateTrip(any())).thenAnswer(
      (_) async =>
          const Left<Failure, TripEstimate>(NetworkFailure(message: 'offline')),
    ),
    act: (TripRequestCubit cubit) => cubit.request(testTripRequest),
    verify: (TripRequestCubit cubit) {
      expect(cubit.state.status, TripRequestStatus.failure);
      expect(cubit.state.failure, isA<NetworkFailure>());
      verifyNever(() => requestTrip(any()));
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'trip_active_exists is surfaced as a failure',
    build: build,
    setUp: () => when(() => requestTrip(any())).thenAnswer(
      (_) async => const Left<Failure, Trip>(
        ServerFailure(code: 'trip_active_exists', message: '', statusCode: 409),
      ),
    ),
    act: (TripRequestCubit cubit) => cubit.request(testTripRequest),
    verify: (TripRequestCubit cubit) {
      expect(cubit.state.status, TripRequestStatus.failure);
      expect(cubit.state.failure?.code, 'trip_active_exists');
      expect(cubit.state.trip, isNull);
    },
  );

  blocTest<TripRequestCubit, TripRequestState>(
    'cancel calls the passenger cancel endpoint and resets',
    build: build,
    setUp: () => when(() => cancelTrip(any())).thenAnswer(
      (_) async => Right<Failure, Trip>(tripAt(TripStage.cancelled)),
    ),
    act: (TripRequestCubit cubit) async {
      await cubit.request(testTripRequest);
      await cubit.cancel(CancelReason.changedMind);
    },
    verify: (TripRequestCubit cubit) {
      expect(cubit.state, const TripRequestState());
      final CancelTripParams params =
          verify(() => cancelTrip(captureAny())).captured.single
              as CancelTripParams;
      expect(params.tripId, 't1');
      expect(params.actor, TripActor.passenger);
      expect(params.reason, CancelReason.changedMind);
    },
  );
}
